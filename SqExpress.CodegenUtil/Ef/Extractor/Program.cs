// Embedded by SqExpress.CodeGenUtil and used as Program.cs for the temporary EF metadata extractor project.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

try
{
    var options = Options.Parse(args);
    var targetAssembly = Assembly.Load(new AssemblyName(options.TargetAssembly));
    var context = CreateDbContext(targetAssembly, options.DbContext);
    try
    {
        var model = GetModel(context);
        var providerName = GetProviderName(context) ?? "";
        var metadata = new EfRelationalMetadata(model);
        var tables = ReadTables(metadata, model);
        var document = new EfMetadataDocument { ProviderName = providerName, Tables = tables };
        Console.Out.Write(JsonSerializer.Serialize(document));
        return 0;
    }
    finally
    {
        if (context is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
catch (ExtractorException e)
{
    Console.Error.WriteLine(e.Message);
    return 1;
}

static List<EfTableMetadata> ReadTables(EfRelationalMetadata metadata, object model)
{
    var tables = new Dictionary<string, EfTableMetadata>(StringComparer.OrdinalIgnoreCase);
    foreach (var entityType in metadata.GetEntityTypes(model))
    {
        var tableName = metadata.GetTableName(entityType);
        if (string.IsNullOrWhiteSpace(tableName))
        {
            continue;
        }

        var schema = metadata.GetSchema(entityType) ?? "dbo";
        var key = schema + "." + tableName;
        if (!tables.TryGetValue(key, out var table))
        {
            table = new EfTableMetadata { Schema = schema, Name = tableName! };
            tables.Add(key, table);
        }

        foreach (var property in metadata.GetProperties(entityType))
        {
            var columnName = metadata.GetColumnName(property, table.Schema, table.Name);
            if (string.IsNullOrWhiteSpace(columnName) || table.Columns.Any(c => string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            table.Columns.Add(new EfColumnMetadata
            {
                Name = columnName!,
                StoreType = metadata.GetColumnType(property) ?? "",
                ClrType = metadata.GetClrType(property).FullName ?? "",
                Nullable = metadata.IsNullable(property),
                MaxLength = metadata.GetMaxLength(property),
                Precision = metadata.GetPrecision(property),
                Scale = metadata.GetScale(property),
                Unicode = metadata.IsUnicode(property),
                Identity = metadata.IsIdentity(property),
                DefaultValueKind = metadata.GetDefaultValueKind(property, out var defaultValue),
                DefaultValue = defaultValue
            });
        }

        ApplyPrimaryKey(metadata, entityType, table);
        ApplyForeignKeys(metadata, entityType, table);
        ApplyIndexes(metadata, entityType, table);
    }

    return tables.Values.ToList();
}

static void ApplyPrimaryKey(EfRelationalMetadata metadata, object entityType, EfTableMetadata table)
{
    var primaryKey = metadata.FindPrimaryKey(entityType);
    if (primaryKey == null)
    {
        return;
    }

    var index = 0;
    foreach (var property in metadata.GetKeyProperties(primaryKey))
    {
        var columnName = metadata.GetColumnName(property, table.Schema, table.Name);
        var column = table.Columns.FirstOrDefault(c => string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase));
        if (column != null)
        {
            column.PrimaryKeyIndex = index++;
        }
    }
}

static void ApplyForeignKeys(EfRelationalMetadata metadata, object entityType, EfTableMetadata table)
{
    foreach (var foreignKey in metadata.GetForeignKeys(entityType))
    {
        var principalEntityType = metadata.GetPrincipalEntityType(foreignKey);
        var principalTableName = metadata.GetTableName(principalEntityType);
        if (string.IsNullOrWhiteSpace(principalTableName))
        {
            continue;
        }

        var principalSchema = metadata.GetSchema(principalEntityType) ?? "dbo";
        var dependentProperties = metadata.GetForeignKeyProperties(foreignKey).ToArray();
        var principalProperties = metadata.GetKeyProperties(metadata.GetPrincipalKey(foreignKey)).ToArray();
        for (var i = 0; i < dependentProperties.Length && i < principalProperties.Length; i++)
        {
            var dependentColumn = metadata.GetColumnName(dependentProperties[i], table.Schema, table.Name);
            var principalColumn = metadata.GetColumnName(principalProperties[i], principalSchema, principalTableName!);
            var column = table.Columns.FirstOrDefault(c => string.Equals(c.Name, dependentColumn, StringComparison.OrdinalIgnoreCase));
            if (column != null && principalColumn != null)
            {
                column.ForeignKeys.Add(new EfColumnRefMetadata { Schema = principalSchema, Table = principalTableName!, Column = principalColumn });
            }
        }
    }
}

static void ApplyIndexes(EfRelationalMetadata metadata, object entityType, EfTableMetadata table)
{
    foreach (var index in metadata.GetIndexes(entityType))
    {
        var descending = metadata.GetIndexDescending(index).ToArray();
        var columns = metadata.GetIndexProperties(index)
            .Select((p, i) => new EfIndexColumnMetadata
            {
                Name = metadata.GetColumnName(p, table.Schema, table.Name) ?? "",
                Descending = i < descending.Length && descending[i]
            })
            .Where(c => !string.IsNullOrWhiteSpace(c.Name) && table.Columns.Any(col => string.Equals(col.Name, c.Name, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (columns.Count == 0)
        {
            continue;
        }

        table.Indexes.Add(new EfIndexMetadata
        {
            Name = metadata.GetIndexName(index, table.Schema, table.Name) ?? "IX_" + table.Name + "_" + string.Join("_", columns.Select(c => c.Name)),
            Unique = metadata.IsUnique(index),
            Clustered = metadata.IsClustered(index),
            Columns = columns
        });
    }
}

static object CreateDbContext(Assembly assembly, string? dbContextTypeName)
{
    var assemblyTypes = assembly.GetTypes();
    var dbContextTypes = assemblyTypes.Where(t => !t.IsAbstract && IsDbContextType(t)).ToList();
    var selectedContextType = SelectDbContextType(dbContextTypes, dbContextTypeName);
    var factoryTypes = assemblyTypes.Where(t => !t.IsAbstract && t.GetInterfaces().Any(IsDesignTimeFactoryInterface)).ToList();
    var matchingFactoryTypes = selectedContextType == null
        ? factoryTypes
        : factoryTypes.Where(t => t.GetInterfaces().Any(i => IsDesignTimeFactoryInterface(i) && i.GetGenericArguments()[0] == selectedContextType)).ToList();

    if (matchingFactoryTypes.Count == 1)
    {
        return CreateFromDesignTimeFactory(matchingFactoryTypes[0]);
    }

    if (matchingFactoryTypes.Count > 1)
    {
        throw new ExtractorException("Found multiple EF design-time DbContext factories. Specify --db-context.");
    }

    selectedContextType ??= dbContextTypes.Count switch
    {
        1 => dbContextTypes[0],
        0 => throw new ExtractorException("Could not find any EF DbContext in the target project."),
        _ => throw new ExtractorException("Found multiple EF DbContexts. Specify --db-context.")
    };

    return Activator.CreateInstance(selectedContextType)
           ?? throw new ExtractorException("Could not create EF DbContext \"" + selectedContextType.FullName + "\". Add IDesignTimeDbContextFactory or a parameterless constructor.");
}

static Type? SelectDbContextType(IReadOnlyList<Type> dbContextTypes, string? dbContextTypeName)
{
    if (string.IsNullOrWhiteSpace(dbContextTypeName))
    {
        return null;
    }

    var matches = dbContextTypes.Where(t => t.FullName == dbContextTypeName || t.Name == dbContextTypeName).ToList();
    return matches.Count switch
    {
        1 => matches[0],
        0 => throw new ExtractorException("Could not find EF DbContext \"" + dbContextTypeName + "\"."),
        _ => throw new ExtractorException("Found multiple EF DbContexts named \"" + dbContextTypeName + "\". Use a fully-qualified type name.")
    };
}

static bool IsDesignTimeFactoryInterface(Type type)
    => type.IsGenericType && type.GetGenericTypeDefinition().FullName == "Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory`1";

static object CreateFromDesignTimeFactory(Type factoryType)
{
    try
    {
        var factory = Activator.CreateInstance(factoryType)
                      ?? throw new ExtractorException("Could not create EF design-time factory \"" + factoryType.FullName + "\".");
        var factoryInterface = factoryType.GetInterfaces().Single(IsDesignTimeFactoryInterface);
        var method = factoryInterface.GetMethod("CreateDbContext")
                     ?? throw new ExtractorException("Could not find CreateDbContext on \"" + factoryType.FullName + "\".");
        return method.Invoke(factory, new object[] { Array.Empty<string>() })
               ?? throw new ExtractorException("Factory \"" + factoryType.FullName + "\" did not return a DbContext.");
    }
    catch (TargetInvocationException e) when (e.InnerException != null)
    {
        throw new ExtractorException(
            "EF design-time factory \"" + factoryType.FullName + "\" failed: " + e.InnerException.Message);
    }
}

static bool IsDbContextType(Type type)
{
    for (var current = type; current != null; current = current.BaseType)
    {
        if (current.FullName == "Microsoft.EntityFrameworkCore.DbContext")
        {
            return true;
        }
    }

    return false;
}

static object GetDatabaseFacade(object dbContext)
    => dbContext.GetType().GetProperty("Database", BindingFlags.Public | BindingFlags.Instance)?.GetValue(dbContext)
       ?? throw new ExtractorException("Could not read Database from EF DbContext.");

static string? GetProviderName(object dbContext)
    => GetDatabaseFacade(dbContext).GetType().GetProperty("ProviderName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(GetDatabaseFacade(dbContext)) as string;

static object GetModel(object dbContext)
{
    try
    {
        var designTimeModel = TryGetDesignTimeModel(dbContext);
        if (designTimeModel != null)
        {
            return designTimeModel;
        }

        return dbContext.GetType().GetProperty("Model", BindingFlags.Public | BindingFlags.Instance)?.GetValue(dbContext)
               ?? throw new ExtractorException("Could not read Model from EF DbContext.");
    }
    catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException)
    {
        throw new ExtractorException(e.InnerException.Message);
    }
}

static object? TryGetDesignTimeModel(object dbContext)
{
    var efAssembly = dbContext.GetType().BaseType?.Assembly;
    var infrastructureType = efAssembly?.GetType("Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure`1")?.MakeGenericType(typeof(IServiceProvider));
    if (infrastructureType == null || !infrastructureType.IsInstanceOfType(dbContext))
    {
        return null;
    }

    var serviceProvider = infrastructureType.GetProperty("Instance")?.GetValue(dbContext) as IServiceProvider;
    var designTimeModelType = efAssembly?.GetType("Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel");
    if (serviceProvider == null || designTimeModelType == null)
    {
        return null;
    }

    var designTimeModel = serviceProvider.GetService(designTimeModelType);
    return designTimeModel?.GetType().GetProperty("Model")?.GetValue(designTimeModel);
}

internal sealed class Options
{
    public string TargetAssembly { get; private set; } = "";
    public string? DbContext { get; private set; }

    public static Options Parse(string[] args)
    {
        var result = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--target-assembly":
                    result.TargetAssembly = args[++i];
                    break;
                case "--db-context":
                    result.DbContext = args[++i];
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(result.TargetAssembly))
        {
            throw new ExtractorException("Usage: --target-assembly <name> [--db-context <type>]");
        }

        return result;
    }
}

internal sealed class ExtractorException : Exception
{
    public ExtractorException(string message) : base(message)
    {
    }
}

internal sealed class EfMetadataDocument
{
    public string ProviderName { get; set; } = "";
    public List<EfTableMetadata> Tables { get; set; } = new();
}

internal sealed class EfTableMetadata
{
    public string Schema { get; set; } = "";
    public string Name { get; set; } = "";
    public List<EfColumnMetadata> Columns { get; set; } = new();
    public List<EfIndexMetadata> Indexes { get; set; } = new();
}

internal sealed class EfColumnMetadata
{
    public string Name { get; set; } = "";
    public string StoreType { get; set; } = "";
    public string ClrType { get; set; } = "";
    public bool Nullable { get; set; }
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool? Unicode { get; set; }
    public bool Identity { get; set; }
    public int? PrimaryKeyIndex { get; set; }
    public string? DefaultValueKind { get; set; }
    public string? DefaultValue { get; set; }
    public List<EfColumnRefMetadata> ForeignKeys { get; set; } = new();
}

internal sealed class EfColumnRefMetadata
{
    public string Schema { get; set; } = "";
    public string Table { get; set; } = "";
    public string Column { get; set; } = "";
}

internal sealed class EfIndexMetadata
{
    public string Name { get; set; } = "";
    public bool Unique { get; set; }
    public bool Clustered { get; set; }
    public List<EfIndexColumnMetadata> Columns { get; set; } = new();
}

internal sealed class EfIndexColumnMetadata
{
    public string Name { get; set; } = "";
    public bool Descending { get; set; }
}

// The extractor intentionally uses reflection over public EF Core abstractions and extension methods.
// This keeps SqExpress.CodeGenUtil from referencing one EF Core version directly, while avoiding
// private/internal EF APIs.
internal sealed class EfRelationalMetadata
{
    private readonly Type _relationalEntityExtensions;
    private readonly Type _relationalPropertyExtensions;
    private readonly Type _relationalIndexExtensions;
    private readonly object _storeObjectIdentifier;

    public EfRelationalMetadata(object model)
    {
        var efAssembly = model.GetType().Assembly;
        var relationalAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Microsoft.EntityFrameworkCore.Relational")
                                 ?? efAssembly.GetReferencedAssemblies().Where(a => a.Name == "Microsoft.EntityFrameworkCore.Relational").Select(Assembly.Load).FirstOrDefault()
                                 ?? throw new ExtractorException("Could not load Microsoft.EntityFrameworkCore.Relational from the EF project.");
        _relationalEntityExtensions = relationalAssembly.GetType("Microsoft.EntityFrameworkCore.RelationalEntityTypeExtensions")
                                      ?? throw new ExtractorException("Could not find EF relational entity extensions.");
        _relationalPropertyExtensions = relationalAssembly.GetType("Microsoft.EntityFrameworkCore.RelationalPropertyExtensions")
                                        ?? throw new ExtractorException("Could not find EF relational property extensions.");
        _relationalIndexExtensions = relationalAssembly.GetType("Microsoft.EntityFrameworkCore.RelationalIndexExtensions")
                                     ?? throw new ExtractorException("Could not find EF relational index extensions.");
        _storeObjectIdentifier = relationalAssembly.GetType("Microsoft.EntityFrameworkCore.Metadata.StoreObjectIdentifier")
                                ?? throw new ExtractorException("Could not find EF StoreObjectIdentifier.");
    }

    public IEnumerable<object> GetEntityTypes(object model) => InvokeEnumerable(model, "GetEntityTypes");
    public IEnumerable<object> GetProperties(object entityType) => InvokeEnumerable(entityType, "GetProperties");
    public IEnumerable<object> GetForeignKeys(object entityType) => InvokeEnumerable(entityType, "GetForeignKeys");
    public IEnumerable<object> GetIndexes(object entityType) => InvokeEnumerable(entityType, "GetIndexes");
    public object? FindPrimaryKey(object entityType) => GetPublicMethod(entityType.GetType(), "FindPrimaryKey", Type.EmptyTypes)?.Invoke(entityType, Array.Empty<object>());
    public IEnumerable<object> GetKeyProperties(object key) => GetEnumerableProperty(key, "Properties");
    public IEnumerable<object> GetForeignKeyProperties(object foreignKey) => GetEnumerableProperty(foreignKey, "Properties");
    public object GetPrincipalEntityType(object foreignKey) => GetPublicProperty(foreignKey.GetType(), "PrincipalEntityType")?.GetValue(foreignKey) ?? throw new ExtractorException("Could not read EF foreign key principal entity type.");
    public object GetPrincipalKey(object foreignKey) => GetPublicProperty(foreignKey.GetType(), "PrincipalKey")?.GetValue(foreignKey) ?? throw new ExtractorException("Could not read EF foreign key principal key.");
    public IEnumerable<object> GetIndexProperties(object index) => GetEnumerableProperty(index, "Properties");
    public bool IsUnique(object index) => (bool?)GetPublicProperty(index.GetType(), "IsUnique")?.GetValue(index) ?? false;
    public bool IsClustered(object index) => GetAnnotationValue(index, "SqlServer:Clustered") as bool? ?? false;
    public bool[] GetIndexDescending(object index)
    {
        var value = GetPublicProperty(index.GetType(), "IsDescending")?.GetValue(index);
        return value is IEnumerable enumerable ? enumerable.Cast<object>().Select(v => v is true).ToArray() : Array.Empty<bool>();
    }
    public string? GetIndexName(object index, string schema, string table) => InvokeRelationalString(_relationalIndexExtensions, "GetDatabaseName", index, CreateStoreObject(schema, table)) ?? InvokeRelationalString(_relationalIndexExtensions, "GetDatabaseName", index);
    public string? GetTableName(object entityType) => InvokeRelationalString(_relationalEntityExtensions, "GetTableName", entityType);
    public string? GetSchema(object entityType) => InvokeRelationalString(_relationalEntityExtensions, "GetSchema", entityType);
    public string? GetColumnName(object property, string schema, string table) => InvokeRelationalString(_relationalPropertyExtensions, "GetColumnName", property, CreateStoreObject(schema, table)) ?? InvokeRelationalString(_relationalPropertyExtensions, "GetColumnName", property) ?? GetPublicProperty(property.GetType(), "Name")?.GetValue(property) as string;
    public string? GetColumnType(object property) => InvokeRelationalString(_relationalPropertyExtensions, "GetColumnType", property);
    public Type GetClrType(object property) => GetPublicProperty(property.GetType(), "ClrType")?.GetValue(property) as Type ?? throw new ExtractorException("Could not read EF property CLR type.");
    public bool IsNullable(object property) => (bool?)GetPublicProperty(property.GetType(), "IsNullable")?.GetValue(property) ?? false;
    public int? GetMaxLength(object property) => InvokeNullableInt(property, "GetMaxLength");
    public int? GetPrecision(object property) => InvokeNullableInt(property, "GetPrecision");
    public int? GetScale(object property) => InvokeNullableInt(property, "GetScale");
    public bool? IsUnicode(object property) => InvokeNullableBool(property, "IsUnicode");
    public bool IsIdentity(object property)
    {
        var value = GetAnnotationValue(property, "SqlServer:ValueGenerationStrategy")?.ToString();
        return value?.IndexOf("IdentityColumn", StringComparison.OrdinalIgnoreCase) >= 0;
    }
    public string? GetDefaultValueKind(object property, out string? defaultValue)
    {
        defaultValue = null;
        var sql = HasAnnotation(property, "Relational:DefaultValueSql") ? InvokeRelationalObject(_relationalPropertyExtensions, "GetDefaultValueSql", property) as string : null;
        if (!string.IsNullOrWhiteSpace(sql))
        {
            var normalizedSql = sql.Trim();
            if (string.Equals(normalizedSql, "getutcdate()", StringComparison.OrdinalIgnoreCase) || string.Equals(normalizedSql, "sysutcdatetime()", StringComparison.OrdinalIgnoreCase))
            {
                return "GetUtcDate";
            }
            defaultValue = sql;
            return "Raw";
        }
        if (!HasAnnotation(property, "Relational:DefaultValue"))
        {
            return null;
        }
        var value = InvokeRelationalObject(_relationalPropertyExtensions, "GetDefaultValue", property);
        if (value == null)
        {
            return null;
        }
        if (value == DBNull.Value)
        {
            return "Null";
        }
        if (value is bool boolValue)
        {
            defaultValue = boolValue ? "1" : "0";
            return "Bool";
        }
        if (value is string text)
        {
            defaultValue = text;
            return "String";
        }
        defaultValue = Convert.ToString(value, CultureInfo.InvariantCulture);
        return "Raw";
    }
    private object CreateStoreObject(string schema, string table) => ((Type)_storeObjectIdentifier).GetMethod("Table", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(string) }, null)?.Invoke(null, new object[] { table, schema }) ?? throw new ExtractorException("Could not create EF StoreObjectIdentifier.");
    private static IEnumerable<object> InvokeEnumerable(object target, string name) => ((IEnumerable?)GetPublicMethod(target.GetType(), name, Type.EmptyTypes)?.Invoke(target, Array.Empty<object>()))?.Cast<object>() ?? Enumerable.Empty<object>();
    private static IEnumerable<object> GetEnumerableProperty(object target, string name) => ((IEnumerable?)GetPublicProperty(target.GetType(), name)?.GetValue(target))?.Cast<object>() ?? Enumerable.Empty<object>();
    private static int? InvokeNullableInt(object target, string name) => GetPublicMethod(target.GetType(), name, Type.EmptyTypes)?.Invoke(target, Array.Empty<object>()) as int?;
    private static bool? InvokeNullableBool(object target, string name) => GetPublicMethod(target.GetType(), name, Type.EmptyTypes)?.Invoke(target, Array.Empty<object>()) as bool?;
    private string? InvokeRelationalString(Type extensionType, string methodName, params object[] args) => InvokeRelationalObject(extensionType, methodName, args) as string;
    private object? InvokeRelationalObject(Type extensionType, string methodName, params object[] args)
    {
        foreach (var method in extensionType.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == methodName))
        {
            if (method.GetParameters().Length != args.Length)
            {
                continue;
            }
            try { return method.Invoke(null, args); }
            catch (ArgumentException) { }
            catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException or NotSupportedException) { }
        }
        return null;
    }
    private static object? GetAnnotationValue(object target, string name)
    {
        var annotation = GetPublicMethod(target.GetType(), "FindAnnotation", new[] { typeof(string) })?.Invoke(target, new object[] { name });
        return annotation == null ? null : GetPublicProperty(annotation.GetType(), "Value")?.GetValue(annotation);
    }
    private static bool HasAnnotation(object target, string name) => GetPublicMethod(target.GetType(), "FindAnnotation", new[] { typeof(string) })?.Invoke(target, new object[] { name }) != null;
    private static MethodInfo? GetPublicMethod(Type type, string name, Type[] parameterTypes) => type.GetMethod(name, parameterTypes) ?? type.GetInterfaces().Select(i => i.GetMethod(name, parameterTypes)).FirstOrDefault(m => m != null);
    private static PropertyInfo? GetPublicProperty(Type type, string name) => type.GetProperty(name) ?? type.GetInterfaces().Select(i => i.GetProperty(name)).FirstOrDefault(p => p != null);
}
