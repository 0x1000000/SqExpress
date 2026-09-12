using System.Collections.Generic;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Json;

/// <summary>Base class for expressions whose value retains JSON fragment semantics.</summary>
public abstract class ExprJson : ExprValue
{
}

/// <summary>Represents strict scalar extraction from a JSON document.</summary>
public sealed class ExprJsonValue : ExprValue
{
    /// <summary>Creates a JSON scalar extraction expression.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="path">The validated portable JSON path.</param>
    /// <param name="returningType">The requested SQL type, or null for string extraction.</param>
    public ExprJsonValue(ExprValue document, string path, ExprType? returningType)
    {
        this.Document = document;
        this.Path = path;
        this.ReturningType = returningType;
    }

    /// <summary>Gets the source document.</summary>
    public ExprValue Document { get; }

    /// <summary>Gets the validated portable JSON path.</summary>
    public string Path { get; }

    /// <summary>Gets the requested SQL type, or null for string extraction.</summary>
    public ExprType? ReturningType { get; }

    /// <inheritdoc />
    public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprJsonValue(this, arg);
}

/// <summary>Represents object-or-array extraction from a JSON document.</summary>
public sealed class ExprJsonQuery : ExprJson
{
    /// <summary>Creates a JSON fragment extraction expression.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="path">The validated portable JSON path.</param>
    public ExprJsonQuery(ExprValue document, string path)
    {
        this.Document = document;
        this.Path = path;
    }

    /// <summary>Gets the source document.</summary>
    public ExprValue Document { get; }

    /// <summary>Gets the validated portable JSON path.</summary>
    public string Path { get; }

    /// <inheritdoc />
    public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprJsonQuery(this, arg);
}

/// <summary>Represents the JSON null value.</summary>
public sealed class ExprJsonNull : ExprJson
{
    /// <summary>Gets the shared JSON null expression.</summary>
    public static readonly ExprJsonNull Instance = new();

    private ExprJsonNull()
    {
    }

    /// <inheritdoc />
    public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprJsonNull(this, arg);
}

/// <summary>Represents creation or replacement of a value in a JSON document.</summary>
public sealed class ExprJsonSet : ExprJson
{
    /// <summary>Creates a JSON set expression.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="path">The path to modify.</param>
    /// <param name="value">The scalar value or JSON fragment to store.</param>
    public ExprJsonSet(ExprValue document, string path, ExprValue value)
    {
        this.Document = document;
        this.Path = path;
        this.Value = value;
    }

    /// <summary>Gets the source document.</summary>
    public ExprValue Document { get; }

    /// <summary>Gets the path to modify.</summary>
    public string Path { get; }

    /// <summary>Gets the scalar value or JSON fragment to store.</summary>
    public ExprValue Value { get; }

    /// <inheritdoc />
    public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprJsonSet(this, arg);
}

/// <summary>Represents removal of a value from a JSON document.</summary>
public sealed class ExprJsonRemove : ExprJson
{
    /// <summary>Creates a JSON removal expression.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="path">The non-root path to remove.</param>
    public ExprJsonRemove(ExprValue document, string path)
    {
        this.Document = document;
        this.Path = path;
    }

    /// <summary>Gets the source document.</summary>
    public ExprValue Document { get; }

    /// <summary>Gets the non-root path to remove.</summary>
    public string Path { get; }

    /// <inheritdoc />
    public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprJsonRemove(this, arg);
}

/// <summary>Represents a statically named JSON object member.</summary>
public sealed class ExprJsonMember : IExpr
{
    /// <summary>Creates a JSON object member.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The scalar value or JSON fragment.</param>
    public ExprJsonMember(string name, ExprValue value)
    {
        this.Name = name;
        this.Value = value;
    }

    /// <summary>Gets the property name.</summary>
    public string Name { get; }

    /// <summary>Gets the scalar value or JSON fragment.</summary>
    public ExprValue Value { get; }

    /// <inheritdoc />
    public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprJsonMember(this, arg);
}

/// <summary>Represents JSON object construction.</summary>
public sealed class ExprJsonObject : ExprJson
{
    /// <summary>Creates a JSON object expression.</summary>
    /// <param name="members">The object members.</param>
    public ExprJsonObject(IReadOnlyList<ExprJsonMember> members)
    {
        this.Members = members;
    }

    /// <summary>Gets the object members in declaration order.</summary>
    public IReadOnlyList<ExprJsonMember> Members { get; }

    /// <inheritdoc />
    public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprJsonObject(this, arg);
}

/// <summary>Represents JSON array construction.</summary>
public sealed class ExprJsonArray : ExprJson
{
    /// <summary>Creates a JSON array expression.</summary>
    /// <param name="items">The array items in output order.</param>
    public ExprJsonArray(IReadOnlyList<ExprValue> items)
    {
        this.Items = items;
    }

    /// <summary>Gets the array items in output order.</summary>
    public IReadOnlyList<ExprValue> Items { get; }

    /// <inheritdoc />
    public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprJsonArray(this, arg);
}

/// <summary>Base class for a logical column produced by <see cref="ExprJsonTable"/>.</summary>
public abstract class ExprJsonTableColumn : IExpr
{
    protected ExprJsonTableColumn(ExprColumnName name)
    {
        this.Name = name;
    }

    /// <summary>Gets the relational output column name.</summary>
    public ExprColumnName Name { get; }

    /// <inheritdoc />
    public abstract TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg);
}

/// <summary>Describes a strictly typed scalar JSON table column.</summary>
public sealed class ExprJsonTableValueColumn : ExprJsonTableColumn
{
    /// <summary>Creates a typed scalar JSON table column.</summary>
    /// <param name="name">The relational output column name.</param>
    /// <param name="path">The path relative to the array element.</param>
    /// <param name="sqlType">The requested SQL result type.</param>
    public ExprJsonTableValueColumn(ExprColumnName name, string path, ExprType sqlType) : base(name)
    {
        this.Path = path;
        this.SqlType = sqlType;
    }

    /// <summary>Gets the path relative to the current array element.</summary>
    public string Path { get; }

    /// <summary>Gets the requested SQL result type.</summary>
    public ExprType SqlType { get; }

    /// <inheritdoc />
    public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprJsonTableValueColumn(this, arg);
}

/// <summary>Describes an object-or-array JSON table column.</summary>
public sealed class ExprJsonTableQueryColumn : ExprJsonTableColumn
{
    /// <summary>Creates a JSON fragment table column.</summary>
    /// <param name="name">The relational output column name.</param>
    /// <param name="path">The path relative to the array element.</param>
    public ExprJsonTableQueryColumn(ExprColumnName name, string path) : base(name)
    {
        this.Path = path;
    }

    /// <summary>Gets the path relative to the current array element.</summary>
    public string Path { get; }

    /// <inheritdoc />
    public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprJsonTableQueryColumn(this, arg);
}

/// <summary>Describes a zero-based array-position column.</summary>
public sealed class ExprJsonTableOrdinalColumn : ExprJsonTableColumn
{
    /// <summary>Creates an ordinal JSON table column.</summary>
    /// <param name="name">The relational output column name.</param>
    public ExprJsonTableOrdinalColumn(ExprColumnName name) : base(name)
    {
    }

    /// <inheritdoc />
    public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprJsonTableOrdinalColumn(this, arg);
}

/// <summary>Represents a typed relational projection over a JSON array.</summary>
public sealed class ExprJsonTable : IExprTableSource
{
    /// <summary>Creates a JSON table source.</summary>
    /// <param name="document">The source JSON document.</param>
    /// <param name="path">The path of the array to expand.</param>
    /// <param name="columns">The logical output columns.</param>
    /// <param name="alias">The table alias.</param>
    public ExprJsonTable(
        ExprValue document,
        string path,
        IReadOnlyList<ExprJsonTableColumn> columns,
        ExprTableAlias alias)
    {
        this.Document = document;
        this.Path = path;
        this.Columns = columns;
        this.Alias = alias;
    }

    /// <summary>Gets the source JSON document.</summary>
    public ExprValue Document { get; }

    /// <summary>Gets the path of the array to expand.</summary>
    public string Path { get; }

    /// <summary>Gets the logical output-column definitions.</summary>
    public IReadOnlyList<ExprJsonTableColumn> Columns { get; }

    /// <summary>Gets the table alias.</summary>
    public ExprTableAlias Alias { get; }

    ExprTableAlias IExprTableSource.Alias => this.Alias;

    /// <summary>Creates a reference to a logical output column.</summary>
    /// <param name="name">The configured output column name.</param>
    /// <returns>A column expression qualified by this table's alias.</returns>
    public ExprColumn Column(string name) => new(this.Alias, new ExprColumnName(name));

    /// <inheritdoc />
    public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg) => visitor.VisitExprJsonTable(this, arg);

    /// <inheritdoc />
    public TableMultiplication ToTableMultiplication() => new([this], null);

    /// <inheritdoc />
    public IReadOnlyList<IExprSelecting> ExtractSelecting()
    {
        var result = new IExprSelecting[this.Columns.Count];
        for (var i = 0; i < result.Length; i++) result[i] = new ExprColumn(this.Alias, this.Columns[i].Name);
        return result;
    }

    /// <inheritdoc />
    public IExprSubQuery CreateSubQuery() => SqQueryBuilder.Select(this.ExtractSelecting()).From(this).Done();
}

/// <summary>Represents a terminal selection mapped to a property path by <c>ForJson()</c>.</summary>
public sealed class ExprJsonOutputColumn : IExprSelecting
{
    /// <summary>Creates a terminal JSON output selection.</summary>
    /// <param name="value">The underlying selection.</param>
    /// <param name="jsonPath">The property-only output path.</param>
    public ExprJsonOutputColumn(IExprSelecting value, string jsonPath)
    {
        this.Value = value;
        this.JsonPath = jsonPath;
    }

    /// <summary>Gets the underlying selection.</summary>
    public IExprSelecting Value { get; }

    /// <summary>Gets the property-only output path.</summary>
    public string JsonPath { get; }

    /// <inheritdoc />
    public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprJsonOutputColumn(this, arg);

    /// <inheritdoc />
    public TRes Accept<TRes, TArg>(IExprSelectingVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprJsonOutputColumn(this, arg);
}

/// <summary>Represents a relational query serialized as a JSON array or object.</summary>
public sealed class ExprQueryAsJson : IExprSubQuery
{
    /// <summary>Creates a relational JSON output query.</summary>
    /// <param name="query">The relational source query.</param>
    /// <param name="withoutArrayWrapper">Whether to return a single object instead of an array.</param>
    /// <param name="includeNullValues">Whether SQL-null properties are included.</param>
    public ExprQueryAsJson(IExprQuery query, bool withoutArrayWrapper, bool includeNullValues)
    {
        this.Query = query;
        this.WithoutArrayWrapper = withoutArrayWrapper;
        this.IncludeNullValues = includeNullValues;
    }

    /// <summary>Gets the relational source query.</summary>
    public IExprQuery Query { get; }

    /// <summary>Gets whether a single object is returned instead of an array.</summary>
    public bool WithoutArrayWrapper { get; }

    /// <summary>Gets whether SQL-null properties are included.</summary>
    public bool IncludeNullValues { get; }

    /// <inheritdoc />
    public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprQueryAsJson(this, arg);

    /// <inheritdoc />
    public IReadOnlyList<IExprSelecting> ExtractSelecting() => [new ExprColumnName("Json")];

    IExprSubQuery ISubQuerySource.CreateSubQuery() => this;

    /// <inheritdoc />
    public IReadOnlyList<string?> GetOutputColumnNames() => ["Json"];

    /// <inheritdoc />
    public IExprSubQuery CreateSubQuery() => this;
}
