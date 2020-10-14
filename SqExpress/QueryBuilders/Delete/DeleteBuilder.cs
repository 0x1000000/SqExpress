using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Update;

namespace SqExpress.QueryBuilders.Delete
{
    public readonly struct DeleteBuilder
    {
        private readonly ExprTable _target;

        public DeleteBuilder(ExprTable target)
        {
            this._target = target;
        }

        public ExprDelete All()
        {
            return new ExprDelete(target: this._target, source: null, filter: null);
        }

        public ExprDelete Where(ExprBoolean? filter)
        {
            return new ExprDelete(target: this._target, source: null, filter: filter);
        }

        public DeleteFromBuilder From(IExprTableSource source)
        {
            return new DeleteFromBuilder(target: this._target, source: source);
        }
    }
}