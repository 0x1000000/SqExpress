using SqExpress.StatementSyntax;

namespace SqExpress.Syntax.Internal
{
    internal class ExprStatement : IExprExec
    {
        public ExprStatement(IStatement statement)
        {
            this.Statement = statement;
        }

        public IStatement Statement { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        {
            if (visitor is not IExprVisitorInternal<TRes, TArg> vi)
            {
                throw new SqExpressException($"Only internal visitors can work with \"{nameof(ExprStatement)}\"");
            }

            return vi.VisitExprStatement(this, arg);
        }
    }
}