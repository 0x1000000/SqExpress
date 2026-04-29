using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.CodeGenUtil
{
    internal static class EfModelTableReader
    {
        public static IReadOnlyList<TableModel> SelectTables(
            object model,
            string providerName,
            string tableClassPrefix,
            bool skipUnknownColumnTypes)
        {
            if (providerName.IndexOf("SqlServer", StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new SqExpressCodeGenException(
                    $"Unsupported EF provider \"{providerName}\". Only Microsoft SQL Server is supported by EF mode currently.");
            }

            var metadata = new EfRelationalMetadata(model);
            var tables = new Dictionary<TableRef, MutableTableModel>();

            foreach (var entityType in metadata.GetEntityTypes(model))
            {
                var tableName = metadata.GetTableName(entityType);
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    continue;
                }

                var schemaName = metadata.GetSchema(entityType) ?? "dbo";
                var tableRef = new TableRef(schemaName, tableName!);
                if (!tables.TryGetValue(tableRef, out var table))
                {
                    table = new MutableTableModel(tableRef, tableClassPrefix + StringHelper.DeSnake(tableRef.Name));
                    tables.Add(tableRef, table);
                }

                foreach (var property in metadata.GetProperties(entityType))
                {
                    var columnName = metadata.GetColumnName(property, tableRef);
                    if (string.IsNullOrWhiteSpace(columnName) || table.Columns.ContainsKey(columnName!))
                    {
                        continue;
                    }

                    var columnType = TryMapColumnType(metadata, property, out var mappedType);
                    if (!columnType)
                    {
                        if (skipUnknownColumnTypes)
                        {
                            continue;
                        }

                        throw new SqExpressCodeGenException(
                            $"Unsupported EF column type \"{metadata.GetColumnType(property)}\" for {tableRef.Schema}.{tableRef.Name}.{columnName}.");
                    }

                    var columnRef = new ColumnRef(tableRef.Schema, tableRef.Name, columnName!);
                    table.Columns.Add(
                        columnName!,
                        new ColumnModel(
                            name: StringHelper.DeSnake(columnName!),
                            dbName: columnRef,
                            ordinalPosition: table.Columns.Count + 1,
                            columnType: mappedType!,
                            pk: null,
                            identity: metadata.IsIdentity(property),
                            defaultValue: metadata.GetDefaultValue(property, mappedType!),
                            fk: null));
                }

                ApplyPrimaryKey(metadata, entityType, table);
                ApplyForeignKeys(metadata, entityType, table, tables);
                ApplyIndexes(metadata, entityType, table);
            }

            var result = tables.Values
                .Select(t => new TableModel(
                    t.ClassName,
                    t.TableRef,
                    t.Columns.Values.OrderBy(c => c.Pk?.Index ?? 10000).ThenBy(c => c.OrdinalPosition).ToList(),
                    t.Indexes))
                .ToList();

            return result
                .OrderBy(GetForeignKeyDepth)
                .ThenBy(t => t.DbName)
                .ToList();

            int GetForeignKeyDepth(TableModel table)
            {
                var visited = new HashSet<TableRef>();
                return Count(table.DbName);

                int Count(TableRef tableRef)
                {
                    if (!visited.Add(tableRef))
                    {
                        return 0;
                    }

                    if (!tables.TryGetValue(tableRef, out var current))
                    {
                        return 0;
                    }

                    var parents = current.Columns.Values
                        .Where(c => c.Fk != null)
                        .SelectMany(c => c.Fk!)
                        .Select(c => c.Table)
                        .Where(t => !t.Equals(tableRef))
                        .Distinct()
                        .ToArray();

                    return parents.Length == 0 ? 0 : parents.Max(Count) + 1;
                }
            }
        }

        private static bool TryMapColumnType(EfRelationalMetadata metadata, object property, out ColumnType? columnType)
        {
            var clrType = metadata.GetClrType(property);
            var nullable = metadata.IsNullable(property);
            var storeType = (metadata.GetColumnType(property) ?? string.Empty).Trim().ToLowerInvariant();
            var maxLength = metadata.GetMaxLength(property) ?? ParseStoreTypeIntArgument(storeType, 0);
            var precision = metadata.GetPrecision(property) ?? ParseStoreTypeIntArgument(storeType, 0);
            var scale = metadata.GetScale(property) ?? ParseStoreTypeIntArgument(storeType, 1);
            var unicode = metadata.IsUnicode(property);

            var baseType = storeType.Split(new[] { '(', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            switch (baseType)
            {
                case "bigint":
                    columnType = new Int64ColumnType(nullable);
                    return true;
                case "int":
                    columnType = new Int32ColumnType(nullable);
                    return true;
                case "smallint":
                    columnType = new Int16ColumnType(nullable);
                    return true;
                case "tinyint":
                    columnType = new ByteColumnType(nullable);
                    return true;
                case "bit":
                    columnType = new BooleanColumnType(nullable);
                    return true;
                case "float":
                case "real":
                    columnType = new DoubleColumnType(nullable);
                    return true;
                case "decimal":
                case "numeric":
                case "money":
                case "smallmoney":
                    columnType = new DecimalColumnType(nullable, precision ?? 18, scale ?? 2);
                    return true;
                case "date":
                    columnType = new DateTimeColumnType(nullable, isDate: true);
                    return true;
                case "datetime":
                case "datetime2":
                case "smalldatetime":
                    columnType = new DateTimeColumnType(nullable, isDate: false);
                    return true;
                case "datetimeoffset":
                    columnType = new DateTimeOffsetColumnType(nullable);
                    return true;
                case "uniqueidentifier":
                    columnType = new GuidColumnType(nullable);
                    return true;
                case "xml":
                    columnType = new XmlColumnType(nullable);
                    return true;
                case "binary":
                    columnType = new ByteArrayColumnType(nullable, maxLength, isFixed: true);
                    return true;
                case "varbinary":
                case "image":
                    columnType = new ByteArrayColumnType(nullable, maxLength, isFixed: false);
                    return true;
                case "char":
                    columnType = new StringColumnType(nullable, maxLength, isFixed: true, isUnicode: false, isText: false);
                    return true;
                case "nchar":
                    columnType = new StringColumnType(nullable, maxLength, isFixed: true, isUnicode: true, isText: false);
                    return true;
                case "varchar":
                case "text":
                    columnType = new StringColumnType(nullable, maxLength, isFixed: false, isUnicode: false, isText: baseType == "text");
                    return true;
                case "nvarchar":
                case "ntext":
                    columnType = new StringColumnType(nullable, maxLength, isFixed: false, isUnicode: unicode ?? true, isText: baseType == "ntext");
                    return true;
            }

            if (clrType == typeof(int) || clrType == typeof(int?))
            {
                columnType = new Int32ColumnType(nullable);
                return true;
            }

            if (clrType == typeof(string))
            {
                columnType = new StringColumnType(nullable, maxLength, isFixed: false, isUnicode: unicode ?? true, isText: false);
                return true;
            }

            columnType = null;
            return false;
        }

        private static int? ParseStoreTypeIntArgument(string storeType, int index)
        {
            var open = storeType.IndexOf('(');
            var close = storeType.IndexOf(')', open + 1);
            if (open < 0 || close < 0)
            {
                return null;
            }

            var args = storeType.Substring(open + 1, close - open - 1)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .ToArray();
            if (index >= args.Length || string.Equals(args[index], "max", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return int.TryParse(args[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
        }

        private static void ApplyPrimaryKey(EfRelationalMetadata metadata, object entityType, MutableTableModel table)
        {
            var primaryKey = metadata.FindPrimaryKey(entityType);
            if (primaryKey == null)
            {
                return;
            }

            var index = 0;
            foreach (var property in metadata.GetKeyProperties(primaryKey))
            {
                var columnName = metadata.GetColumnName(property, table.TableRef);
                if (columnName == null || !table.Columns.TryGetValue(columnName, out var column))
                {
                    continue;
                }

                table.Columns[columnName] = new ColumnModel(
                    column.Name,
                    column.DbName,
                    column.OrdinalPosition,
                    column.ColumnType,
                    new PkInfo(index++, descending: false),
                    column.Identity,
                    column.DefaultValue,
                    column.Fk);
            }
        }

        private static void ApplyForeignKeys(
            EfRelationalMetadata metadata,
            object entityType,
            MutableTableModel table,
            Dictionary<TableRef, MutableTableModel> allTables)
        {
            foreach (var foreignKey in metadata.GetForeignKeys(entityType))
            {
                var principalEntityType = metadata.GetPrincipalEntityType(foreignKey);
                var principalTableName = metadata.GetTableName(principalEntityType);
                if (string.IsNullOrWhiteSpace(principalTableName))
                {
                    continue;
                }

                var principalTable = new TableRef(metadata.GetSchema(principalEntityType) ?? "dbo", principalTableName!);
                var dependentProperties = metadata.GetForeignKeyProperties(foreignKey).ToArray();
                var principalProperties = metadata.GetKeyProperties(metadata.GetPrincipalKey(foreignKey)).ToArray();
                for (var i = 0; i < dependentProperties.Length && i < principalProperties.Length; i++)
                {
                    var dependentColumn = metadata.GetColumnName(dependentProperties[i], table.TableRef);
                    var principalColumn = metadata.GetColumnName(principalProperties[i], principalTable);
                    if (dependentColumn == null || principalColumn == null || !table.Columns.TryGetValue(dependentColumn, out var column))
                    {
                        continue;
                    }

                    var fk = column.Fk == null ? new List<ColumnRef>() : new List<ColumnRef>(column.Fk);
                    fk.Add(new ColumnRef(principalTable.Schema, principalTable.Name, principalColumn));
                    table.Columns[dependentColumn] = new ColumnModel(
                        column.Name,
                        column.DbName,
                        column.OrdinalPosition,
                        column.ColumnType,
                        column.Pk,
                        column.Identity,
                        column.DefaultValue,
                        fk);
                }
            }
        }

        private static void ApplyIndexes(EfRelationalMetadata metadata, object entityType, MutableTableModel table)
        {
            foreach (var index in metadata.GetIndexes(entityType))
            {
                var columns = metadata.GetIndexProperties(index)
                    .Select(p => metadata.GetColumnName(p, table.TableRef))
                    .Where(c => c != null && table.Columns.ContainsKey(c))
                    .Select(c => new ColumnRef(table.TableRef.Schema, table.TableRef.Name, c!))
                    .ToList();

                if (columns.Count == 0)
                {
                    continue;
                }

                var descending = metadata.GetIndexDescending(index).ToArray();
                table.Indexes.Add(new IndexModel(
                    columns.Select((c, i) => new IndexColumnModel(i < descending.Length && descending[i], c)).ToList(),
                    metadata.GetIndexName(index, table.TableRef) ?? "IX_" + table.TableRef.Name + "_" + string.Join("_", columns.Select(c => c.Name)),
                    metadata.IsUnique(index),
                    metadata.IsClustered(index)));
            }
        }

        private sealed class MutableTableModel
        {
            public MutableTableModel(TableRef tableRef, string className)
            {
                this.TableRef = tableRef;
                this.ClassName = className;
            }

            public TableRef TableRef { get; }

            public string ClassName { get; }

            public Dictionary<string, ColumnModel> Columns { get; } = new Dictionary<string, ColumnModel>(StringComparer.OrdinalIgnoreCase);

            public List<IndexModel> Indexes { get; } = new List<IndexModel>();
        }

        private sealed class EfRelationalMetadata
        {
            // Keep EF access late-bound so CodeGenUtil does not pin a specific EF Core version.
            // Only public EF APIs are called; missing or changed APIs should fail explicitly.
            private readonly Type _relationalEntityExtensions;
            private readonly Type _relationalPropertyExtensions;
            private readonly Type _relationalIndexExtensions;
            private readonly object _storeObjectIdentifier;

            public EfRelationalMetadata(object model)
            {
                var efAssembly = model.GetType().Assembly;
                var relationalAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Microsoft.EntityFrameworkCore.Relational")
                    ?? efAssembly.GetReferencedAssemblies()
                        .Where(a => a.Name == "Microsoft.EntityFrameworkCore.Relational")
                        .Select(Assembly.Load)
                        .FirstOrDefault()
                    ?? throw new SqExpressCodeGenException("Could not load Microsoft.EntityFrameworkCore.Relational from the EF project.");

                this._relationalEntityExtensions = relationalAssembly.GetType("Microsoft.EntityFrameworkCore.RelationalEntityTypeExtensions")
                                                    ?? throw new SqExpressCodeGenException("Could not find EF relational entity extensions.");
                this._relationalPropertyExtensions = relationalAssembly.GetType("Microsoft.EntityFrameworkCore.RelationalPropertyExtensions")
                                                      ?? throw new SqExpressCodeGenException("Could not find EF relational property extensions.");
                this._relationalIndexExtensions = relationalAssembly.GetType("Microsoft.EntityFrameworkCore.RelationalIndexExtensions")
                                                   ?? throw new SqExpressCodeGenException("Could not find EF relational index extensions.");
                this._storeObjectIdentifier = relationalAssembly.GetType("Microsoft.EntityFrameworkCore.Metadata.StoreObjectIdentifier")
                                              ?? throw new SqExpressCodeGenException("Could not find EF StoreObjectIdentifier.");
            }

            public IEnumerable<object> GetEntityTypes(object model)
                => InvokeEnumerable(model, "GetEntityTypes");

            public IEnumerable<object> GetProperties(object entityType)
                => InvokeEnumerable(entityType, "GetProperties");

            public IEnumerable<object> GetForeignKeys(object entityType)
                => InvokeEnumerable(entityType, "GetForeignKeys");

            public IEnumerable<object> GetIndexes(object entityType)
                => InvokeEnumerable(entityType, "GetIndexes");

            public object? FindPrimaryKey(object entityType)
                => GetPublicMethod(entityType.GetType(), "FindPrimaryKey", Type.EmptyTypes)?.Invoke(entityType, Array.Empty<object>());

            public IEnumerable<object> GetKeyProperties(object key)
                => GetEnumerableProperty(key, "Properties");

            public IEnumerable<object> GetForeignKeyProperties(object foreignKey)
                => GetEnumerableProperty(foreignKey, "Properties");

            public object GetPrincipalEntityType(object foreignKey)
                => GetPublicProperty(foreignKey.GetType(), "PrincipalEntityType")?.GetValue(foreignKey)
                   ?? throw new SqExpressCodeGenException("Could not read EF foreign key principal entity type.");

            public object GetPrincipalKey(object foreignKey)
                => GetPublicProperty(foreignKey.GetType(), "PrincipalKey")?.GetValue(foreignKey)
                   ?? throw new SqExpressCodeGenException("Could not read EF foreign key principal key.");

            public IEnumerable<object> GetIndexProperties(object index)
                => GetEnumerableProperty(index, "Properties");

            public bool IsUnique(object index)
                => (bool?)GetPublicProperty(index.GetType(), "IsUnique")?.GetValue(index) ?? false;

            public bool IsClustered(object index)
                => GetAnnotationValue(index, "SqlServer:Clustered") as bool? ?? false;

            public bool[] GetIndexDescending(object index)
            {
                var value = GetPublicProperty(index.GetType(), "IsDescending")?.GetValue(index);
                return value is IEnumerable enumerable ? enumerable.Cast<object>().Select(v => v is true).ToArray() : Array.Empty<bool>();
            }

            public string? GetIndexName(object index, TableRef table)
                => InvokeRelationalString(this._relationalIndexExtensions, "GetDatabaseName", index, CreateStoreObject(table))
                   ?? InvokeRelationalString(this._relationalIndexExtensions, "GetDatabaseName", index);

            public string? GetTableName(object entityType)
                => InvokeRelationalString(this._relationalEntityExtensions, "GetTableName", entityType);

            public string? GetSchema(object entityType)
                => InvokeRelationalString(this._relationalEntityExtensions, "GetSchema", entityType);

            public string? GetColumnName(object property, TableRef table)
                => InvokeRelationalString(this._relationalPropertyExtensions, "GetColumnName", property, CreateStoreObject(table))
                   ?? InvokeRelationalString(this._relationalPropertyExtensions, "GetColumnName", property)
                   ?? GetPublicProperty(property.GetType(), "Name")?.GetValue(property) as string;

            public string? GetColumnType(object property)
                => InvokeRelationalString(this._relationalPropertyExtensions, "GetColumnType", property);

            public Type GetClrType(object property)
                => GetPublicProperty(property.GetType(), "ClrType")?.GetValue(property) as Type
                   ?? throw new SqExpressCodeGenException("Could not read EF property CLR type.");

            public bool IsNullable(object property)
                => (bool?)GetPublicProperty(property.GetType(), "IsNullable")?.GetValue(property) ?? false;

            public int? GetMaxLength(object property)
                => InvokeNullableInt(property, "GetMaxLength");

            public int? GetPrecision(object property)
                => InvokeNullableInt(property, "GetPrecision");

            public int? GetScale(object property)
                => InvokeNullableInt(property, "GetScale");

            public bool? IsUnicode(object property)
                => InvokeNullableBool(property, "IsUnicode");

            public bool IsIdentity(object property)
            {
                var value = GetAnnotationValue(property, "SqlServer:ValueGenerationStrategy")?.ToString();
                return value?.IndexOf("IdentityColumn", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            public DefaultValue? GetDefaultValue(object property, ColumnType columnType)
            {
                var hasDefaultSql = HasAnnotation(property, "Relational:DefaultValueSql");
                var hasDefaultValue = HasAnnotation(property, "Relational:DefaultValue");

                var sql = hasDefaultSql
                    ? InvokeRelationalObject(this._relationalPropertyExtensions, "GetDefaultValueSql", property) as string
                    : null;
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    var normalizedSql = sql.Trim();
                    if (string.Equals(normalizedSql, "getutcdate()", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(normalizedSql, "sysutcdatetime()", StringComparison.OrdinalIgnoreCase))
                    {
                        return new DefaultValue(DefaultValueType.GetUtcDate, null);
                    }

                    return new DefaultValue(DefaultValueType.Raw, sql);
                }

                if (!hasDefaultValue)
                {
                    return null;
                }

                var value = InvokeRelationalObject(this._relationalPropertyExtensions, "GetDefaultValue", property);
                if (value == null)
                {
                    return null;
                }

                if (value == DBNull.Value)
                {
                    return new DefaultValue(DefaultValueType.Null, null);
                }

                if (columnType is BooleanColumnType)
                {
                    return new DefaultValue(DefaultValueType.Bool, Convert.ToBoolean(value, CultureInfo.InvariantCulture) ? "1" : "0");
                }

                if (value is string text)
                {
                    return new DefaultValue(DefaultValueType.String, text);
                }

                return new DefaultValue(DefaultValueType.Raw, Convert.ToString(value, CultureInfo.InvariantCulture));
            }

            private object CreateStoreObject(TableRef table)
            {
                return ((Type)this._storeObjectIdentifier).GetMethod("Table", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(string) }, null)
                           ?.Invoke(null, new object[] { table.Name, table.Schema })
                       ?? throw new SqExpressCodeGenException("Could not create EF StoreObjectIdentifier.");
            }

            private static IEnumerable<object> InvokeEnumerable(object target, string name)
                => ((IEnumerable?)GetPublicMethod(target.GetType(), name, Type.EmptyTypes)?.Invoke(target, Array.Empty<object>()))
                    ?.Cast<object>()
                   ?? Enumerable.Empty<object>();

            private static IEnumerable<object> GetEnumerableProperty(object target, string name)
                => ((IEnumerable?)GetPublicProperty(target.GetType(), name)?.GetValue(target))?.Cast<object>() ?? Enumerable.Empty<object>();

            private static int? InvokeNullableInt(object target, string name)
                => GetPublicMethod(target.GetType(), name, Type.EmptyTypes)?.Invoke(target, Array.Empty<object>()) as int?;

            private static bool? InvokeNullableBool(object target, string name)
                => GetPublicMethod(target.GetType(), name, Type.EmptyTypes)?.Invoke(target, Array.Empty<object>()) as bool?;

            private string? InvokeRelationalString(Type extensionType, string methodName, params object[] args)
                => InvokeRelationalObject(extensionType, methodName, args) as string;

            private object? InvokeRelationalObject(Type extensionType, string methodName, params object[] args)
            {
                foreach (var method in extensionType.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == methodName))
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length != args.Length)
                    {
                        continue;
                    }

                    try
                    {
                        return method.Invoke(null, args);
                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException or NotSupportedException)
                    {
                    }
                }

                return null;
            }

            private static object? GetAnnotationValue(object target, string name)
            {
                var method = GetPublicMethod(target.GetType(), "FindAnnotation", new[] { typeof(string) });
                var annotation = method?.Invoke(target, new object[] { name });
                return annotation == null ? null : GetPublicProperty(annotation.GetType(), "Value")?.GetValue(annotation);
            }

            private static bool HasAnnotation(object target, string name)
            {
                var method = GetPublicMethod(target.GetType(), "FindAnnotation", new[] { typeof(string) });
                return method?.Invoke(target, new object[] { name }) != null;
            }

            private static MethodInfo? GetPublicMethod(Type type, string name, Type[] parameterTypes)
            {
                return type.GetMethod(name, parameterTypes)
                       ?? type.GetInterfaces()
                           .Select(i => i.GetMethod(name, parameterTypes))
                           .FirstOrDefault(m => m != null);
            }

            private static PropertyInfo? GetPublicProperty(Type type, string name)
            {
                return type.GetProperty(name)
                       ?? type.GetInterfaces()
                           .Select(i => i.GetProperty(name))
                           .FirstOrDefault(p => p != null);
            }
        }
    }
}
