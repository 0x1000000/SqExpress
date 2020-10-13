namespace SqExpress.QueryBuilders
{
    public interface IExprSubQueryFinal : IExprQueryFinal
    {
        new IExprSubQuery Done();
    }
}