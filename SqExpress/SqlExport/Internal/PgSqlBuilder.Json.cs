using SqExpress.Internal;
using SqExpress.Syntax;
using SqExpress.Syntax.Json;
using SqExpress.Syntax.Type;

namespace SqExpress.SqlExport.Internal;

internal partial class PgSqlBuilder
{
    public override bool VisitExprJsonValue(ExprJsonValue expr, IExpr? parent)
    {
        this.FormattingWriter.Append("CASE WHEN jsonb_typeof(jsonb_path_query_first(CAST(");
        expr.Document.Accept(this, expr);
        this.FormattingWriter.Append(" AS jsonb),");
        this.Str(expr.Path);
        this.FormattingWriter.Append("))=");
        this.Str(
            expr.ReturningType == null || expr.ReturningType is ExprTypeString ? "string" :
            expr.ReturningType is ExprTypeBoolean ? "boolean" : "number"
        );
        this.FormattingWriter.Append(" THEN CAST(jsonb_path_query_first(CAST(");
        expr.Document.Accept(this, expr);
        this.FormattingWriter.Append(" AS jsonb),");
        this.Str(expr.Path);
        this.FormattingWriter.Append(") #>> '{}' AS ");
        (expr.ReturningType ?? SqQueryBuilder.SqlType.String()).Accept(this, expr);
        this.FormattingWriter.Append(") END");
        return true;
    }

    public override bool VisitExprJsonQuery(ExprJsonQuery expr, IExpr? parent)
    {
        this.FormattingWriter.Append("CASE WHEN jsonb_typeof(jsonb_path_query_first(CAST(");
        expr.Document.Accept(this, expr);
        this.FormattingWriter.Append(" AS jsonb),");
        this.Str(expr.Path);
        this.FormattingWriter.Append(")) IN ('object','array') THEN jsonb_path_query_first(CAST(");
        expr.Document.Accept(this, expr);
        this.FormattingWriter.Append(" AS jsonb),");
        this.Str(expr.Path);
        this.FormattingWriter.Append(") END");
        return true;
    }

    public override bool VisitExprJsonNull(ExprJsonNull expr, IExpr? parent)
    {
        this.FormattingWriter.Append("'null'::jsonb");
        return true;
    }

    public override bool VisitExprJsonSet(ExprJsonSet expr, IExpr? parent)
    {
        this.FormattingWriter.Append("jsonb_set(CAST(");
        expr.Document.Accept(this, expr);
        this.FormattingWriter.Append(" AS jsonb),");
        this.PathArray(expr.Path);
        this.FormattingWriter.Append(',');
        if (expr.Value is ExprJson) expr.Value.Accept(this, expr);
        else
        {
            this.FormattingWriter.Append("to_jsonb(");
            expr.Value.Accept(this, expr);
            this.FormattingWriter.Append(')');
        }

        this.FormattingWriter.Append(",true)");
        return true;
    }

    public override bool VisitExprJsonRemove(ExprJsonRemove expr, IExpr? parent)
    {
        this.FormattingWriter.Append("CAST(");
        expr.Document.Accept(this, expr);
        this.FormattingWriter.Append(" AS jsonb)#-");
        this.PathArray(expr.Path);
        return true;
    }

    public override bool VisitExprJsonMember(ExprJsonMember expr, IExpr? parent)
    {
        this.Str(expr.Name);
        this.FormattingWriter.Append(',');
        expr.Value.Accept(this, expr);
        return true;
    }

    public override bool VisitExprJsonObject(ExprJsonObject expr, IExpr? parent)
    {
        this.FormattingWriter.Append("jsonb_build_object(");
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
        this.FormattingWriter.Append("jsonb_build_array(");
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
            expr.Columns[i].Accept(this, expr);
            this.FormattingWriter.Append(' ');
            expr.Columns[i].Name.Accept(this, expr);
        }

        this.FormattingWriter.Append(" FROM jsonb_array_elements(jsonb_path_query_first(CAST(");
        expr.Document.Accept(this, expr);
        this.FormattingWriter.Append(" AS jsonb),");
        this.Str(expr.Path);
        this.FormattingWriter.Append(")) WITH ORDINALITY J(value,ordinal)) ");
        expr.Alias.Accept(this, expr);
        return true;
    }

    public override bool VisitExprJsonTableValueColumn(ExprJsonTableValueColumn expr, IExpr? parent)
    {
        this.FormattingWriter.Append("CAST(jsonb_path_query_first(J.value,");
        this.Str(expr.Path);
        this.FormattingWriter.Append(")#>>'{}' AS ");
        expr.SqlType.Accept(this, expr);
        this.FormattingWriter.Append(')');
        return true;
    }

    public override bool VisitExprJsonTableQueryColumn(ExprJsonTableQueryColumn expr, IExpr? parent)
    {
        this.FormattingWriter.Append("jsonb_path_query_first(J.value,");
        this.Str(expr.Path);
        this.FormattingWriter.Append(')');
        return true;
    }

    public override bool VisitExprJsonTableOrdinalColumn(ExprJsonTableOrdinalColumn expr, IExpr? parent)
    {
        this.FormattingWriter.Append("J.ordinal-1");
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
        var shape = JsonOutputShape.Build(expr.Query);
        this.FormattingWriter.Append(expr.WithoutArrayWrapper ? "SELECT (SELECT " : "SELECT COALESCE(jsonb_agg(");
        this.AppendRowObject(shape, expr.IncludeNullValues);
        this.FormattingWriter.Append(expr.WithoutArrayWrapper ? " FROM (" : "),'[]'::jsonb) Json FROM (");
        this.RenderForJsonSource(expr.Query, expr);
        this.FormattingWriter.Append(expr.WithoutArrayWrapper ? ") J0) Json" : ") J0");
        return true;
    }

    private void AppendRowObject(System.Collections.Generic.IReadOnlyList<JsonOutputShape> nodes, bool includeNullValues)
    {
        if (!includeNullValues) this.FormattingWriter.Append("jsonb_strip_nulls(");
        this.FormattingWriter.Append("jsonb_build_object(");
        this.ObjectShape(nodes);
        this.FormattingWriter.Append(')');
        if (!includeNullValues) this.FormattingWriter.Append(')');
    }

    private void ObjectShape(System.Collections.Generic.IReadOnlyList<JsonOutputShape> nodes)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            if (i > 0) this.FormattingWriter.Append(',');
            var c = nodes[i];
            this.Str(c.Name);
            this.FormattingWriter.Append(',');
            if (c.ColumnName != null)
            {
                this.FormattingWriter.Append("J0.");
                this.AppendName(c.ColumnName);
            }
            else
            {
                this.FormattingWriter.Append("jsonb_build_object(");
                this.ObjectShape(c.Children);
                this.FormattingWriter.Append(')');
            }
        }
    }

    private void PathArray(string path)
    {
        var m = SqJsonPathParser.Parse(path);
        this.FormattingWriter.Append("ARRAY[");
        for (var i = 0; i < m.Segments.Count; i++)
        {
            if (i > 0) this.FormattingWriter.Append(',');
            var s = m.Segments[i];
            this.Str(
                s.Kind == SqJsonPathSegmentKind.Property
                    ? s.Property!
                    : s.Index.ToString(System.Globalization.CultureInfo.InvariantCulture)
            );
        }

        this.FormattingWriter.Append(']');
    }

    private void Str(string s)
    {
        this.FormattingWriter.Append('\'');
        this.FormattingWriter.AppendEscapedSingleQuote(s);
        this.FormattingWriter.Append('\'');
    }
}
