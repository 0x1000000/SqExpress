using System.Collections.Generic;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax;
using SqExpress.Syntax.Json;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;

namespace SqExpress.SqlExport.Internal;

internal partial class MySqlBuilder
{
    public override bool VisitExprJsonValue(ExprJsonValue expr, IExpr? parent)
    {
        this.FormattingWriter.Append("CASE WHEN JSON_TYPE(JSON_EXTRACT(");
        expr.Document.Accept(this, expr);
        this.JsonPath(expr.Path, expr);
        this.FormattingWriter.Append("))");
        if (expr.ReturningType is ExprTypeDecimal or ExprTypeDouble)
            this.FormattingWriter.Append(" IN ('INTEGER','DOUBLE','DECIMAL')");
        else
        {
            this.FormattingWriter.Append('=');
            this.JsonString(
                expr.ReturningType == null || expr.ReturningType is ExprTypeString ? "STRING" :
                expr.ReturningType is ExprTypeBoolean ? "BOOLEAN" : "INTEGER"
            );
        }
        this.FormattingWriter.Append(" THEN ");
        if (expr.ReturningType is ExprTypeBoolean)
        {
            this.FormattingWriter.Append("CASE JSON_UNQUOTE(JSON_EXTRACT(");
            expr.Document.Accept(this, expr);
            this.JsonPath(expr.Path, expr);
            this.FormattingWriter.Append(")) WHEN 'true' THEN 1 WHEN 'false' THEN 0 END");
        }
        else
        {
            if (expr.ReturningType == null || expr.ReturningType is ExprTypeString)
                this.FormattingWriter.Append("JSON_UNQUOTE(");
            else
                this.FormattingWriter.Append("CAST(JSON_UNQUOTE(");

            this.FormattingWriter.Append("JSON_EXTRACT(");
            expr.Document.Accept(this, expr);
            this.JsonPath(expr.Path, expr);
            this.FormattingWriter.Append(')');
            if (expr.ReturningType == null || expr.ReturningType is ExprTypeString) this.FormattingWriter.Append(')');
            else
            {
                this.FormattingWriter.Append(") AS ");
                this.RenderJsonCastType(expr.ReturningType!, expr);
                this.FormattingWriter.Append(')');
            }
        }

        this.FormattingWriter.Append(" END");
        return true;
    }

    public override bool VisitExprJsonQuery(ExprJsonQuery expr, IExpr? parent)
    {
        if (this.RenderingForJson && this.Flavor == MySqlFlavor.MariaDb && expr.Path == "$" && parent is ExprAliasedSelecting or ExprJsonOutputColumn)
        {
            expr.Document.Accept(this, expr);
            return true;
        }

        this.FormattingWriter.Append("CASE WHEN JSON_TYPE(JSON_EXTRACT(");
        expr.Document.Accept(this, expr);
        this.JsonPath(expr.Path, expr);
        this.FormattingWriter.Append(")) IN ('OBJECT','ARRAY') THEN JSON_EXTRACT(");
        expr.Document.Accept(this, expr);
        this.JsonPath(expr.Path, expr);
        this.FormattingWriter.Append(") END");
        return true;
    }

    public override bool VisitExprJsonNull(ExprJsonNull expr, IExpr? parent)
    {
        this.FormattingWriter.Append("JSON_EXTRACT('null','$')");
        return true;
    }

    public override bool VisitExprJsonSet(ExprJsonSet expr, IExpr? parent)
    {
        this.FormattingWriter.Append("JSON_SET(");
        expr.Document.Accept(this, expr);
        this.JsonPath(expr.Path, expr);
        this.FormattingWriter.Append(',');
        expr.Value.Accept(this, expr);
        this.FormattingWriter.Append(')');
        return true;
    }

    public override bool VisitExprJsonRemove(ExprJsonRemove expr, IExpr? parent)
    {
        this.FormattingWriter.Append("JSON_REMOVE(");
        expr.Document.Accept(this, expr);
        this.JsonPath(expr.Path, expr);
        this.FormattingWriter.Append(')');
        return true;
    }

    public override bool VisitExprJsonMember(ExprJsonMember expr, IExpr? parent)
    {
        this.JsonString(expr.Name);
        this.FormattingWriter.Append(',');
        if (this.Flavor == MySqlFlavor.MariaDb && expr.Value is ExprValueQuery { Query: ExprQueryAsJson })
        {
            this.FormattingWriter.Append("JSON_EXTRACT(CAST(");
            expr.Value.Accept(this, expr);
            this.FormattingWriter.Append(" AS CHAR),'$')");
            return true;
        }
        expr.Value.Accept(this, expr);
        return true;
    }

    public override bool VisitExprJsonObject(ExprJsonObject expr, IExpr? parent)
    {
        this.FormattingWriter.Append("JSON_OBJECT(");
        for (var i = 0; i < expr.Members.Count; i++)
        {
            if (i > 0) this.FormattingWriter.Append(',');
            expr.Members[i].Accept(this, expr);
        }

        this.FormattingWriter.Append(')');
        return true;
    }

    public override bool VisitExprJsonArray(ExprJsonArray expr, IExpr? parent)
    {
        this.FormattingWriter.Append("JSON_ARRAY(");
        for (var i = 0; i < expr.Items.Count; i++)
        {
            if (i > 0) this.FormattingWriter.Append(',');
            expr.Items[i].Accept(this, expr);
        }

        this.FormattingWriter.Append(')');
        return true;
    }

    public override bool VisitExprJsonTable(ExprJsonTable expr, IExpr? parent)
    {
        this.FormattingWriter.Append("(SELECT ");
        for (var i = 0; i < expr.Columns.Count; i++)
        {
            if (i > 0) this.FormattingWriter.Append(',');
            this.FormattingWriter.Append("J.");
            expr.Columns[i].Name.Accept(this, expr);
            if (expr.Columns[i] is ExprJsonTableOrdinalColumn) this.FormattingWriter.Append("-1");
            this.FormattingWriter.Append(' ');
            expr.Columns[i].Name.Accept(this, expr);
        }
        this.FormattingWriter.Append(" FROM JSON_TABLE(");
        this.FormattingWriter.Append("JSON_EXTRACT(");
        expr.Document.Accept(this, expr);
        this.FormattingWriter.Append(",'$')");
        this.JsonPath(expr.Path + "[*]", expr);
        this.FormattingWriter.Append(" COLUMNS(");
        for (var i = 0; i < expr.Columns.Count; i++)
        {
            if (i > 0) this.FormattingWriter.Append(',');
            expr.Columns[i].Name.Accept(this, expr);
            this.FormattingWriter.Append(' ');
            expr.Columns[i].Accept(this, expr);
        }

        this.FormattingWriter.Append(")) J) ");
        expr.Alias.Accept(this, expr);
        return true;
    }

    private void AppendMariaDbCorrelatedJsonTable(ExprJsonTable expr)
    {
        foreach (var column in expr.Columns)
        {
            if (column is ExprJsonTableOrdinalColumn)
                throw new SqExpressException("MariaDB cannot expose zero-based ordinality from a correlated JsonTable().");
        }

        this.FormattingWriter.Append("JSON_TABLE(JSON_EXTRACT(");
        expr.Document.Accept(this, expr);
        this.FormattingWriter.Append(",'$')");
        this.JsonPath(expr.Path + "[*]", expr);
        this.FormattingWriter.Append(" COLUMNS(");
        for (var i = 0; i < expr.Columns.Count; i++)
        {
            if (i > 0) this.FormattingWriter.Append(',');
            expr.Columns[i].Name.Accept(this, expr);
            this.FormattingWriter.Append(' ');
            expr.Columns[i].Accept(this, expr);
        }
        this.FormattingWriter.Append(")) ");
        expr.Alias.Accept(this, expr);
    }

    public override bool VisitExprJsonTableValueColumn(ExprJsonTableValueColumn expr, IExpr? parent)
    {
        if (expr.SqlType is ExprTypeDecimal { PrecisionScale: null }) this.FormattingWriter.Append("decimal(38,18)");
        else if (expr.SqlType is ExprTypeBoolean) this.FormattingWriter.Append("tinyint");
        else expr.SqlType.Accept(this, expr);
        this.FormattingWriter.Append(" PATH ");
        this.JsonString(expr.Path);
        this.FormattingWriter.Append(" NULL ON EMPTY NULL ON ERROR");
        return true;
    }

    public override bool VisitExprJsonTableQueryColumn(ExprJsonTableQueryColumn expr, IExpr? parent)
    {
        this.FormattingWriter.Append("JSON PATH ");
        this.JsonString(expr.Path);
        this.FormattingWriter.Append(" NULL ON EMPTY NULL ON ERROR");
        return true;
    }

    public override bool VisitExprJsonTableOrdinalColumn(ExprJsonTableOrdinalColumn expr, IExpr? parent)
    {
        this.FormattingWriter.Append("FOR ORDINALITY");
        return true;
    }

    public override bool VisitExprJsonOutputColumn(ExprJsonOutputColumn expr, IExpr? parent)
    {
        if (!this.RenderingForJson)
            throw new SqExpressException("AsJson() output columns can only be exported inside ForJson().");
        expr.Value.Accept(this, expr);
        this.FormattingWriter.Append(' ');
        this.AppendJsonOutputAlias(expr);
        return true;
    }

    public override bool VisitExprQueryAsJson(ExprQueryAsJson expr, IExpr? parent)
    {
        if (this.Flavor == MySqlFlavor.MariaDb && parent is ExprValueQuery && expr.Query is ExprQuerySpecification specification)
        {
            this.AppendMariaDbCorrelatedForJson(expr, specification);
            return true;
        }
        var shape = JsonOutputShape.Build(expr.Query);
        this.FormattingWriter.Append(expr.WithoutArrayWrapper ? "SELECT (SELECT " : "SELECT COALESCE(JSON_ARRAYAGG(");
        this.AppendRowObject(shape, expr.IncludeNullValues);
        this.FormattingWriter.Append(expr.WithoutArrayWrapper ? " FROM (" : "),JSON_ARRAY()) Json FROM (");
        this.RenderForJsonSource(expr.Query, expr);
        this.FormattingWriter.Append(expr.WithoutArrayWrapper ? ") J0) Json" : ") J0");
        return true;
    }

    private void AppendMariaDbCorrelatedForJson(ExprQueryAsJson expression, ExprQuerySpecification specification)
    {
        var shape = JsonOutputShape.Build(expression.Query);
        var values = new Dictionary<string, ExprValue>();
        var jsonColumnIndex = 0;
        foreach (var selecting in specification.SelectList)
        {
            var name = selecting is ExprJsonOutputColumn
                ? JsonOutputShape.InternalColumnName(jsonColumnIndex++)
                : ((IExprNamedSelecting)selecting).OutputName
                  ?? throw new SqExpressException("ForJson() requires every selected expression to have an output name.");
            values.Add(name, SelectingValue(selecting));
        }

        ExprValue row = BuildObject(shape, values);
        if (!expression.IncludeNullValues)
            row = SqQueryBuilder.ScalarFunctionSys("JSON_MERGE_PATCH", new ExprJsonObject(new ExprJsonMember[0]), row);

        IExprSelecting selectingRow = expression.WithoutArrayWrapper
            ? row
            : new ExprAggregateFunction(false, new ExprFunctionName(true, "JSON_ARRAYAGG"), row);
        var query = specification.WithSelectList([selectingRow]);

        this.FormattingWriter.Append("SELECT ");
        if (!expression.WithoutArrayWrapper) this.FormattingWriter.Append("COALESCE(");
        this.AcceptPar('(', query, ')', expression);
        if (!expression.WithoutArrayWrapper) this.FormattingWriter.Append(",JSON_ARRAY())");
        this.FormattingWriter.Append(" Json");
    }

    private static ExprJsonObject BuildObject(IReadOnlyList<JsonOutputShape> nodes, IReadOnlyDictionary<string, ExprValue> values)
    {
        var members = new ExprJsonMember[nodes.Count];
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            members[i] = new ExprJsonMember(node.Name,
                node.ColumnName != null ? values[node.ColumnName] : BuildObject(node.Children, values));
        }
        return new ExprJsonObject(members);
    }

    private static ExprValue SelectingValue(IExprSelecting selecting)
        => selecting switch
        {
            ExprJsonOutputColumn json => SelectingValue(json.Value),
            ExprAliasedSelecting aliased => SelectingValue(aliased.Value),
            ExprAliasedColumn aliased => aliased.Column,
            ExprValue value => value,
            _ => selecting.AsValue()
        };

    private void AppendRowObject(System.Collections.Generic.IReadOnlyList<JsonOutputShape> nodes, bool includeNullValues)
    {
        if (!includeNullValues) this.FormattingWriter.Append("JSON_MERGE_PATCH(JSON_OBJECT(),");
        this.FormattingWriter.Append("JSON_OBJECT(");
        this.AppendObjectShape(nodes);
        this.FormattingWriter.Append(')');
        if (!includeNullValues) this.FormattingWriter.Append(')');
    }

    private void AppendObjectShape(System.Collections.Generic.IReadOnlyList<JsonOutputShape> nodes)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            if (i > 0) this.FormattingWriter.Append(',');
            var c = nodes[i];
            this.JsonString(c.Name);
            this.FormattingWriter.Append(',');
            if (c.ColumnName != null)
            {
                if (c.IsJson && this.Flavor == MySqlFlavor.MariaDb) this.FormattingWriter.Append("JSON_EXTRACT(CAST(");
                this.FormattingWriter.Append("J0.");
                this.AppendName(c.ColumnName);
                if (c.IsJson && this.Flavor == MySqlFlavor.MariaDb) this.FormattingWriter.Append(" AS CHAR),'$')");
            }
            else
            {
                this.FormattingWriter.Append("JSON_OBJECT(");
                this.AppendObjectShape(c.Children);
                this.FormattingWriter.Append(')');
            }
        }
    }

    private void JsonPath(string path, IExpr parent)
    {
        this.FormattingWriter.Append(',');
        this.JsonString(path);
    }

    private void JsonString(string value)
    {
        this.FormattingWriter.Append('\'');
        this.FormattingWriter.AppendEscapedSingleQuote(value);
        this.FormattingWriter.Append('\'');
    }

    private void RenderJsonCastType(ExprType type, IExpr parent)
    {
        if (type is ExprTypeInt32 or ExprTypeInt64)
        {
            this.FormattingWriter.Append("SIGNED");
            return;
        }
        if (type is ExprTypeDecimal { PrecisionScale: null })
        {
            this.FormattingWriter.Append("decimal(38,18)");
            return;
        }
        type.Accept(this, parent);
    }
}
