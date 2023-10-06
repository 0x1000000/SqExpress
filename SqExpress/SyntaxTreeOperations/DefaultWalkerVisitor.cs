using System;
using System.Collections.Generic;
using SqExpress.Syntax;

namespace SqExpress.SyntaxTreeOperations
{
    public class DefaultWalkerVisitor<TCtx> : DefaultWalkerVisitorBase<TCtx>, IWalkerVisitor<TCtx>
    {
        private readonly Func<IExpr, TCtx, VisitorResult<TCtx>> _walkerBody;

        public DefaultWalkerVisitor(Func<IExpr, TCtx, VisitorResult<TCtx>> walkerBody) : this(walkerBody, default!)
        {
        }

        public DefaultWalkerVisitor(Func<IExpr, TCtx, VisitorResult<TCtx>> walkerBody, TCtx currentCtx) : base(currentCtx)
        {
            this._walkerBody = walkerBody;
        }

        public VisitorResult<TCtx> VisitExpr(IExpr expr, string typeTag, TCtx ctx)
        {
            return this._walkerBody.Invoke(expr, ctx);
        }
    }

    internal class DefaultParentWalkerVisitorWithParent<TCtx> : DefaultWalkerVisitorBase<TCtx>, IWalkerVisitorWithParent<TCtx>
    {
        private readonly Func<IExpr, IExpr?, TCtx, VisitorResult<TCtx>> _walkerBody;

        public DefaultParentWalkerVisitorWithParent(Func<IExpr, IExpr?, TCtx, VisitorResult<TCtx>> walkerBody, TCtx currentCtx) : base(currentCtx)
        {
            this._walkerBody = walkerBody;
        }

        public VisitorResult<TCtx> VisitExpr(IExpr expr, IExpr? parent, string typeTag, TCtx ctx)
        {
            return this._walkerBody.Invoke(expr, parent, ctx);
        }
    }

    public class DefaultWalkerVisitorBase<TCtx> : IWalkerVisitorBase<TCtx>
    {
        public TCtx CurrentCtx { get; private set; }

        public DefaultWalkerVisitorBase(TCtx currentCtx)
        {
            this.CurrentCtx = currentCtx;
        }

        public void EndVisitExpr(IExpr expr, TCtx ctx)
        {
            this.CurrentCtx = ctx;
        }

        public void VisitProperty(string name, bool isArray, bool isNull, TCtx ctx)
        {
        }

        public void EndVisitProperty(string name, bool isArray, bool isNull, TCtx ctx)
        {
        }

        public void VisitArrayItem(string name, int arrayIndex, TCtx ctx)
        {
        }

        public void EndVisitArrayItem(string name, int arrayIndex, TCtx ctx)
        {
        }

        public void VisitPlainProperty(string name, string? value, TCtx ctx)
        {
        }

        public void VisitPlainProperty(string name, bool? value, TCtx ctx)
        {
        }

        public void VisitPlainProperty(string name, byte? value, TCtx ctx)
        {
        }

        public void VisitPlainProperty(string name, short? value, TCtx ctx)
        {
        }

        public void VisitPlainProperty(string name, int? value, TCtx ctx)
        {
        }

        public void VisitPlainProperty(string name, long? value, TCtx ctx)
        {
        }

        public void VisitPlainProperty(string name, decimal? value, TCtx ctx)
        {
        }

        public void VisitPlainProperty(string name, double? value, TCtx ctx)
        {
        }

        public void VisitPlainProperty(string name, DateTime? value, TCtx ctx)
        {
        }

        public void VisitPlainProperty(string name, DateTimeOffset? value, TCtx ctx)
        {
        }

        public void VisitPlainProperty(string name, Guid? value, TCtx ctx)
        {
        }

        public void VisitPlainProperty(string name, IReadOnlyList<byte>? value, TCtx ctx)
        {
        }
    }
}