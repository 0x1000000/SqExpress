using System;
using System.Collections;
using System.Collections.Generic;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Output;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress.SyntaxTreeOperations.Internal
{
    internal class ExprWalkerPull : IExprVisitor<bool, object?>, IEnumerator<IExpr>
    {
        private const int MaxDeep = 1000000;

        public static IEnumerable<IExpr> GetEnumerable(IExpr root, bool self) => new ExprWalkerPullEnumerable(root, self);

        private readonly IExpr _root;

        private readonly bool _self;

        private StackItem[] _stack;

        private HashSet<string>? _cteChecker;

        private int _stackIndex = -1;

        private IExpr? _current;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private ExprWalkerPull(IExpr root, bool self)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            this._root = root;
            this._self = self;
            this.Reset();
        }

        private void Push(IExpr expr)
        {
            this._stackIndex++;
            this.PushWatchDog();
            if (this._stackIndex >= this._stack.Length)
            {
                Array.Resize(ref this._stack, this._stack.Length * 2);
            }

            this._stack[this._stackIndex] = new StackItem(expr, null, 0, 0);
        }

        private void Push(IReadOnlyList<IExpr> expr)
        {
            this._stackIndex++;
            this.PushWatchDog();
            if (this._stackIndex >= this._stack.Length)
            {
                Array.Resize(ref this._stack, this._stack.Length * 2);
            }

            this._stack[this._stackIndex] = new StackItem(null, expr, 0, 0);
        }

        private void PushWatchDog()
        {
            if (this._stackIndex >= MaxDeep)
            {
                throw new Exception("Expression deep has reached its limit.");
            }
        }

        private ref StackItem Peek()
        {
            return ref this._stack[this._stackIndex];
        }

        private bool Pop()
        {
            ref var head = ref this.Peek();

            if (head.NodeList != null)
            {
                head.Index++;
                head.State = 0;
                if (head.Index < head.NodeList.Count)
                {
                    this._current = head.NodeList[head.Index];
                    return true;
                }
            }

            this._stack[this._stackIndex] = default;
            this._stackIndex--;
            return false;
        }

        private bool SetCurrent(IExpr? expr)
        {
            if (expr != null)
            {
                this._current = expr;
                this.Push(expr);
                return true;
            }

            return false;
        }

        private bool SetCurrent(IReadOnlyList<IExpr>? expr)
        {
            if (expr != null && expr.Count > 0)
            {
                this._current = expr[0];
                this.Push(expr);
                return true;
            }

            return false;
        }

        public bool MoveNext()
        {
            if (this._current == null)
            {
                if (this._self)
                {
                    this.SetCurrent(this._root);
                    return true;
                }

                throw new SqExpressException("Incorrect enumerator state for self");
            }

            bool next = true;
            while (next)
            {
                if (this._stackIndex < 0)
                {
                    return false;
                }

                ref var stackItem = ref this.Peek();

                if (stackItem.Node != null)
                {
                    stackItem.State++;
                    next = !stackItem.Node.Accept(this, null);
                }
                else if (stackItem.NodeList != null)
                {
                    stackItem.State++;
                    next = !stackItem.NodeList[stackItem.Index].Accept(this, null);
                }
                else
                {
                    throw new SqExpressException("Incorrect enumerator state");
                }
            }

            return this._stackIndex >= 0;
        }

        public void Reset()
        {
            this._stackIndex = -1;
            this._stack = new StackItem[8];
            this._cteChecker = new HashSet<string>();
            if (!this._self)
            {
                this.SetCurrent(this._root);
            }
        }

        public IExpr Current => this._current.AssertFatalNotNull("Current");

        object IEnumerator.Current => this.Current;

        public void Dispose()
        {
        }

        private class ExprWalkerPullEnumerable : IEnumerable<IExpr>
        {
            private readonly IExpr _root;

            private readonly bool _self;

            public ExprWalkerPullEnumerable(IExpr root, bool self)
            {
                this._root = root;
                this._self = self;
            }

            public IEnumerator<IExpr> GetEnumerator() => new ExprWalkerPull(this._root, this._self);

            IEnumerator IEnumerable.GetEnumerator() => new ExprWalkerPull(this._root, this._self);
        }

        private struct StackItem
        {
            public readonly IExpr? Node;
            public readonly IReadOnlyList<IExpr>? NodeList;
            public int State;
            public int Index;

            public StackItem(IExpr? node, IReadOnlyList<IExpr>? nodeList, int state, int index)
            {
                this.Node = node;
                this.NodeList = nodeList;
                this.State = state;
                this.Index = index;
            }
        }

        public bool VisitExprCteQuery(ExprCteQuery expr, object? arg)
        {
            this._cteChecker ??= new HashSet<string>();
            if (!this._cteChecker.Contains(expr.Name))
            {
                bool res;
                switch (this.Peek().State)
                {
                    case 1:
                        res = this.SetCurrent(expr.Alias);
                        break;
                    case 2:
                        this._cteChecker.Add(expr.Name);
                        res = this.SetCurrent(expr.Query);
                        break;
                    case 3:
                        res = this.Pop();
                        break;
                    default:
                        throw new SqExpressException("Incorrect enumerator visitor state");
                }

                return res;
            }

            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Alias);
                case 2:
                    return this.Pop();
                case 3:
                    this._cteChecker.Remove(expr.Name);
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }

        public bool VisitExprDerivedTableQuery(ExprDerivedTableQuery expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Query);
                case 2:
                    return this.SetCurrent(expr.Alias);
                case 3:
                    return this.SetCurrent(expr.Columns);
                case 4:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }

        //CodeGenStart
        public bool VisitExprAggregateFunction(ExprAggregateFunction expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Name);
                case 2:
                    return this.SetCurrent(expr.Expression);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprAlias(ExprAlias expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprAliasGuid(ExprAliasGuid expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprAliasedColumn(ExprAliasedColumn expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Column);
                case 2:
                    return this.SetCurrent(expr.Alias);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprAliasedColumnName(ExprAliasedColumnName expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Column);
                case 2:
                    return this.SetCurrent(expr.Alias);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprAliasedSelecting(ExprAliasedSelecting expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Value);
                case 2:
                    return this.SetCurrent(expr.Alias);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprAliasedTableFunction(ExprAliasedTableFunction expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Function);
                case 2:
                    return this.SetCurrent(expr.Alias);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprAllColumns(ExprAllColumns expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Source);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprAnalyticFunction(ExprAnalyticFunction expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Name);
                case 2:
                    return this.SetCurrent(expr.Arguments);
                case 3:
                    return this.SetCurrent(expr.Over);
                case 4:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprBitwiseAnd(ExprBitwiseAnd expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprBitwiseNot(ExprBitwiseNot expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Value);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprBitwiseOr(ExprBitwiseOr expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprBitwiseXor(ExprBitwiseXor expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprBoolLiteral(ExprBoolLiteral expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprBooleanAnd(ExprBooleanAnd expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprBooleanEq(ExprBooleanEq expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprBooleanGt(ExprBooleanGt expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprBooleanGtEq(ExprBooleanGtEq expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprBooleanLt(ExprBooleanLt expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprBooleanLtEq(ExprBooleanLtEq expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprBooleanNot(ExprBooleanNot expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Expr);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprBooleanNotEq(ExprBooleanNotEq expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprBooleanOr(ExprBooleanOr expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprByteArrayLiteral(ExprByteArrayLiteral expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprByteLiteral(ExprByteLiteral expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprCase(ExprCase expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Cases);
                case 2:
                    return this.SetCurrent(expr.DefaultValue);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprCaseWhenThen(ExprCaseWhenThen expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Condition);
                case 2:
                    return this.SetCurrent(expr.Value);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprCast(ExprCast expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Expression);
                case 2:
                    return this.SetCurrent(expr.SqlType);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprColumn(ExprColumn expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Source);
                case 2:
                    return this.SetCurrent(expr.ColumnName);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprColumnAlias(ExprColumnAlias expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprColumnName(ExprColumnName expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprColumnSetClause(ExprColumnSetClause expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Column);
                case 2:
                    return this.SetCurrent(expr.Value);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprCrossedTable(ExprCrossedTable expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        ////Default implementation
        //public bool VisitExprCteQuery(ExprCteQuery expr, object? arg)
        //{
            //switch (this.Peek().State)
            //{
                //case 1:
                    //return this.SetCurrent(expr.Alias);
                //case 2:
                    //return this.SetCurrent(expr.Query);
                //case 3:
                    //return this.Pop();
                //default:
                    //throw new SqExpressException("Incorrect enumerator visitor state");
            //}
        //}
        public bool VisitExprCurrentRowFrameBorder(ExprCurrentRowFrameBorder expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprDatabaseName(ExprDatabaseName expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprDateAdd(ExprDateAdd expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Date);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprDateTimeLiteral(ExprDateTimeLiteral expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprDateTimeOffsetLiteral(ExprDateTimeOffsetLiteral expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprDbSchema(ExprDbSchema expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Database);
                case 2:
                    return this.SetCurrent(expr.Schema);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprDecimalLiteral(ExprDecimalLiteral expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprDefault(ExprDefault expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprDelete(ExprDelete expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Target);
                case 2:
                    return this.SetCurrent(expr.Source);
                case 3:
                    return this.SetCurrent(expr.Filter);
                case 4:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprDeleteOutput(ExprDeleteOutput expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Delete);
                case 2:
                    return this.SetCurrent(expr.OutputColumns);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        ////Default implementation
        //public bool VisitExprDerivedTableQuery(ExprDerivedTableQuery expr, object? arg)
        //{
            //switch (this.Peek().State)
            //{
                //case 1:
                    //return this.SetCurrent(expr.Query);
                //case 2:
                    //return this.SetCurrent(expr.Alias);
                //case 3:
                    //return this.SetCurrent(expr.Columns);
                //case 4:
                    //return this.Pop();
                //default:
                    //throw new SqExpressException("Incorrect enumerator visitor state");
            //}
        //}
        public bool VisitExprDerivedTableValues(ExprDerivedTableValues expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Values);
                case 2:
                    return this.SetCurrent(expr.Alias);
                case 3:
                    return this.SetCurrent(expr.Columns);
                case 4:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprDiv(ExprDiv expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprDoubleLiteral(ExprDoubleLiteral expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprExists(ExprExists expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.SubQuery);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprExprMergeNotMatchedInsert(ExprExprMergeNotMatchedInsert expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.And);
                case 2:
                    return this.SetCurrent(expr.Columns);
                case 3:
                    return this.SetCurrent(expr.Values);
                case 4:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprExprMergeNotMatchedInsertDefault(ExprExprMergeNotMatchedInsertDefault expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.And);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprFrameClause(ExprFrameClause expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Start);
                case 2:
                    return this.SetCurrent(expr.End);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprFuncCoalesce(ExprFuncCoalesce expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Test);
                case 2:
                    return this.SetCurrent(expr.Alts);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprFuncIsNull(ExprFuncIsNull expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Test);
                case 2:
                    return this.SetCurrent(expr.Alt);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprFunctionName(ExprFunctionName expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprGetDate(ExprGetDate expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprGetUtcDate(ExprGetUtcDate expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprGuidLiteral(ExprGuidLiteral expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprIdentityInsert(ExprIdentityInsert expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Insert);
                case 2:
                    return this.SetCurrent(expr.IdentityColumns);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprInSubQuery(ExprInSubQuery expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.TestExpression);
                case 2:
                    return this.SetCurrent(expr.SubQuery);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprInValues(ExprInValues expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.TestExpression);
                case 2:
                    return this.SetCurrent(expr.Items);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprInsert(ExprInsert expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Target);
                case 2:
                    return this.SetCurrent(expr.TargetColumns);
                case 3:
                    return this.SetCurrent(expr.Source);
                case 4:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprInsertOutput(ExprInsertOutput expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Insert);
                case 2:
                    return this.SetCurrent(expr.OutputColumns);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprInsertQuery(ExprInsertQuery expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Query);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprInsertValueRow(ExprInsertValueRow expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Items);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprInsertValues(ExprInsertValues expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Items);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprInt16Literal(ExprInt16Literal expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprInt32Literal(ExprInt32Literal expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprInt64Literal(ExprInt64Literal expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprIsNull(ExprIsNull expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Test);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprJoinedTable(ExprJoinedTable expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.SetCurrent(expr.SearchCondition);
                case 4:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprLateralCrossedTable(ExprLateralCrossedTable expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprLike(ExprLike expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Test);
                case 2:
                    return this.SetCurrent(expr.Pattern);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprList(ExprList expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Expressions);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprMerge(ExprMerge expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.TargetTable);
                case 2:
                    return this.SetCurrent(expr.Source);
                case 3:
                    return this.SetCurrent(expr.On);
                case 4:
                    return this.SetCurrent(expr.WhenMatched);
                case 5:
                    return this.SetCurrent(expr.WhenNotMatchedByTarget);
                case 6:
                    return this.SetCurrent(expr.WhenNotMatchedBySource);
                case 7:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprMergeMatchedDelete(ExprMergeMatchedDelete expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.And);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprMergeMatchedUpdate(ExprMergeMatchedUpdate expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.And);
                case 2:
                    return this.SetCurrent(expr.Set);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprMergeOutput(ExprMergeOutput expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.TargetTable);
                case 2:
                    return this.SetCurrent(expr.Source);
                case 3:
                    return this.SetCurrent(expr.On);
                case 4:
                    return this.SetCurrent(expr.WhenMatched);
                case 5:
                    return this.SetCurrent(expr.WhenNotMatchedByTarget);
                case 6:
                    return this.SetCurrent(expr.WhenNotMatchedBySource);
                case 7:
                    return this.SetCurrent(expr.Output);
                case 8:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprModulo(ExprModulo expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprMul(ExprMul expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprNull(ExprNull expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprOffsetFetch(ExprOffsetFetch expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Offset);
                case 2:
                    return this.SetCurrent(expr.Fetch);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprOrderBy(ExprOrderBy expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.OrderList);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprOrderByItem(ExprOrderByItem expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Value);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprOrderByOffsetFetch(ExprOrderByOffsetFetch expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.OrderList);
                case 2:
                    return this.SetCurrent(expr.OffsetFetch);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprOutput(ExprOutput expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Columns);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprOutputAction(ExprOutputAction expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Alias);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprOutputColumn(ExprOutputColumn expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Column);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprOutputColumnDeleted(ExprOutputColumnDeleted expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.ColumnName);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprOutputColumnInserted(ExprOutputColumnInserted expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.ColumnName);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprOver(ExprOver expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Partitions);
                case 2:
                    return this.SetCurrent(expr.OrderBy);
                case 3:
                    return this.SetCurrent(expr.FrameClause);
                case 4:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprQueryExpression(ExprQueryExpression expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprQueryList(ExprQueryList expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Expressions);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprQuerySpecification(ExprQuerySpecification expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.SelectList);
                case 2:
                    return this.SetCurrent(expr.Top);
                case 3:
                    return this.SetCurrent(expr.From);
                case 4:
                    return this.SetCurrent(expr.Where);
                case 5:
                    return this.SetCurrent(expr.GroupBy);
                case 6:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprScalarFunction(ExprScalarFunction expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Schema);
                case 2:
                    return this.SetCurrent(expr.Name);
                case 3:
                    return this.SetCurrent(expr.Arguments);
                case 4:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprSchemaName(ExprSchemaName expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprSelect(ExprSelect expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.SelectQuery);
                case 2:
                    return this.SetCurrent(expr.OrderBy);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprSelectOffsetFetch(ExprSelectOffsetFetch expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.SelectQuery);
                case 2:
                    return this.SetCurrent(expr.OrderBy);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprStringConcat(ExprStringConcat expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprStringLiteral(ExprStringLiteral expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprSub(ExprSub expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprSum(ExprSum expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Left);
                case 2:
                    return this.SetCurrent(expr.Right);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTable(ExprTable expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.FullName);
                case 2:
                    return this.SetCurrent(expr.Alias);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTableAlias(ExprTableAlias expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Alias);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTableFullName(ExprTableFullName expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.DbSchema);
                case 2:
                    return this.SetCurrent(expr.TableName);
                case 3:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTableFunction(ExprTableFunction expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Schema);
                case 2:
                    return this.SetCurrent(expr.Name);
                case 3:
                    return this.SetCurrent(expr.Arguments);
                case 4:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTableName(ExprTableName expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTableValueConstructor(ExprTableValueConstructor expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Items);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTempTableName(ExprTempTableName expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTypeBoolean(ExprTypeBoolean expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTypeByte(ExprTypeByte expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTypeByteArray(ExprTypeByteArray expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTypeDateTime(ExprTypeDateTime expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTypeDateTimeOffset(ExprTypeDateTimeOffset expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTypeDecimal(ExprTypeDecimal expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTypeDouble(ExprTypeDouble expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTypeFixSizeByteArray(ExprTypeFixSizeByteArray expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTypeFixSizeString(ExprTypeFixSizeString expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTypeGuid(ExprTypeGuid expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTypeInt16(ExprTypeInt16 expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTypeInt32(ExprTypeInt32 expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTypeInt64(ExprTypeInt64 expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTypeString(ExprTypeString expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprTypeXml(ExprTypeXml expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprUnboundedFrameBorder(ExprUnboundedFrameBorder expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprUnsafeValue(ExprUnsafeValue expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprUpdate(ExprUpdate expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Target);
                case 2:
                    return this.SetCurrent(expr.SetClause);
                case 3:
                    return this.SetCurrent(expr.Source);
                case 4:
                    return this.SetCurrent(expr.Filter);
                case 5:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprValueFrameBorder(ExprValueFrameBorder expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Value);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprValueQuery(ExprValueQuery expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Query);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        public bool VisitExprValueRow(ExprValueRow expr, object? arg)
        {
            switch (this.Peek().State)
            {
                case 1:
                    return this.SetCurrent(expr.Items);
                case 2:
                    return this.Pop();
                default:
                    throw new SqExpressException("Incorrect enumerator visitor state");
            }
        }
        //CodeGenEnd
    }
}
