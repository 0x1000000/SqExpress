using System;
using System.Collections.Generic;
using SqExpress.Internal;
using SqExpress.Syntax.Json;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;

namespace SqExpress.QueryBuilders.Json;

/// <summary>Builds a typed relational projection over a JSON array.</summary>
public sealed class JsonTableBuilder
{
    private readonly ExprValue _document;
    private readonly string _path;
    private readonly List<ExprJsonTableColumn> _columns = new();

    internal JsonTableBuilder(ExprValue document, string path) { this._document = document; this._path = path; }

    /// <summary>Adds a strictly typed scalar column.</summary>
    /// <param name="name">The relational output column name.</param>
    /// <param name="path">The scalar path relative to each array element.</param>
    /// <param name="sqlType">The supported SQL result type.</param>
    /// <returns>This builder.</returns>
    public JsonTableBuilder Value(string name, SqJsonPath path, ExprType sqlType)
    {
        EnsureUnique(name); ValidateType(sqlType);
        this._columns.Add(new ExprJsonTableValueColumn(new ExprColumnName(name), RequirePath(path), sqlType));
        return this;
    }

    /// <summary>Adds a column containing an object or array JSON fragment.</summary>
    /// <param name="name">The relational output column name.</param>
    /// <param name="path">The fragment path relative to each array element.</param>
    /// <returns>This builder.</returns>
    public JsonTableBuilder Query(string name, SqJsonPath path)
    {
        EnsureUnique(name);
        this._columns.Add(new ExprJsonTableQueryColumn(new ExprColumnName(name), RequirePath(path)));
        return this;
    }

    /// <summary>Adds a zero-based array-position column.</summary>
    /// <param name="name">The relational output column name.</param>
    /// <returns>This builder.</returns>
    public JsonTableBuilder Ordinal(string name)
    {
        EnsureUnique(name);
        this._columns.Add(new ExprJsonTableOrdinalColumn(new ExprColumnName(name)));
        return this;
    }

    /// <summary>Completes the JSON table with the specified alias.</summary>
    /// <param name="alias">The table alias.</param>
    /// <returns>A JSON table source.</returns>
    public ExprJsonTable As(string alias) => this.As(SqQueryBuilder.TableAlias(alias));

    /// <summary>Completes the JSON table with the specified alias.</summary>
    /// <param name="alias">The table alias.</param>
    /// <returns>A JSON table source.</returns>
    public ExprJsonTable As(ExprTableAlias alias)
    {
        if (this._columns.Count == 0) throw new SqExpressException("JSON table must define at least one column.");
        return new ExprJsonTable(this._document, this._path, this._columns.ToArray(), alias);
    }

    private void EnsureUnique(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new SqExpressException("JSON table column name cannot be empty.");
        foreach (var column in this._columns)
            if (string.Equals(column.Name.Name, name, StringComparison.Ordinal)) throw new SqExpressException($"Duplicate JSON table column '{name}'.");
    }

    internal static void ValidateType(ExprType type)
    {
        if (type is not (ExprTypeString or ExprTypeBoolean or ExprTypeInt32 or ExprTypeInt64 or ExprTypeDecimal or ExprTypeDouble))
            throw new SqExpressException($"SQL type '{type.GetType().Name}' is not supported for portable JSON scalar extraction.");
    }

    private static string RequirePath(SqJsonPath path)
    {
        if (path.Value == null) throw new SqExpressException("JSON path cannot be default or null.");
        SqJsonPathParser.Parse(path.Value);
        return path.Value;
    }
}
