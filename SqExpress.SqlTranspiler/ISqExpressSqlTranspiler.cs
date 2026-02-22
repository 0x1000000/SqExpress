namespace SqExpress.SqlTranspiler
{
    public interface ISqExpressSqlTranspiler
    {
        SqExpressTranspileResult Transpile(string sql, SqExpressSqlTranspilerOptions? options = null);

        SqExpressTranspileResult TranspileSelect(string sql, SqExpressSqlTranspilerOptions? options = null);
    }
}
