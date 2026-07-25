using System;
using System.Collections.Generic;
using SqExpress.Syntax;

namespace SqExpress.SyntaxTreeOperations
{
    /// <summary>
    /// Adapts a delegate into an expression-tree visitor while ignoring structural property callbacks.
    /// </summary>
    /// <typeparam name="TCtx">The context propagated through the traversal.</typeparam>
    public class DefaultWalkerVisitor<TCtx> : DefaultWalkerVisitorBase<TCtx>, IWalkerVisitor<TCtx>
    {
        private readonly Func<IExpr, TCtx, VisitorResult<TCtx>> _walkerBody;

        /// <summary>Creates a visitor whose initial context is the default value of <typeparamref name="TCtx"/>.</summary>
        /// <param name="walkerBody">The callback invoked for each visited expression.</param>
        public DefaultWalkerVisitor(Func<IExpr, TCtx, VisitorResult<TCtx>> walkerBody) : this(walkerBody, default!)
        {
        }

        /// <summary>Creates a visitor with an explicit initial context.</summary>
        /// <param name="walkerBody">The callback invoked for each visited expression.</param>
        /// <param name="currentCtx">The initial traversal context.</param>
        public DefaultWalkerVisitor(Func<IExpr, TCtx, VisitorResult<TCtx>> walkerBody, TCtx currentCtx) : base(currentCtx)
        {
            this._walkerBody = walkerBody;
        }

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides no-op structural callbacks for visitors interested primarily in expression nodes.
    /// </summary>
    /// <typeparam name="TCtx">The context propagated through the traversal.</typeparam>
    public class DefaultWalkerVisitorBase<TCtx> : IWalkerVisitorBase<TCtx>
    {
        /// <summary>Gets the context supplied when the most recently completed expression was exited.</summary>
        public TCtx CurrentCtx { get; private set; }

        /// <summary>Creates the base visitor with an initial context.</summary>
        /// <param name="currentCtx">The initial traversal context.</param>
        public DefaultWalkerVisitorBase(TCtx currentCtx)
        {
            this.CurrentCtx = currentCtx;
        }

        /// <inheritdoc/>
        public void EndVisitExpr(IExpr expr, TCtx ctx)
        {
            this.CurrentCtx = ctx;
        }

        /// <inheritdoc/>
        public void VisitProperty(string name, bool isArray, bool isNull, TCtx ctx)
        {
        }

        /// <inheritdoc/>
        public void EndVisitProperty(string name, bool isArray, bool isNull, TCtx ctx)
        {
        }

        /// <inheritdoc/>
        public void VisitArrayItem(string name, int arrayIndex, TCtx ctx)
        {
        }

        /// <inheritdoc/>
        public void EndVisitArrayItem(string name, int arrayIndex, TCtx ctx)
        {
        }

        /// <inheritdoc/>
        public void VisitPlainProperty(string name, string? value, TCtx ctx)
        {
        }

        /// <inheritdoc/>
        public void VisitPlainProperty(string name, bool? value, TCtx ctx)
        {
        }

        /// <inheritdoc/>
        public void VisitPlainProperty(string name, byte? value, TCtx ctx)
        {
        }

        /// <inheritdoc/>
        public void VisitPlainProperty(string name, short? value, TCtx ctx)
        {
        }

        /// <inheritdoc/>
        public void VisitPlainProperty(string name, int? value, TCtx ctx)
        {
        }

        /// <inheritdoc/>
        public void VisitPlainProperty(string name, long? value, TCtx ctx)
        {
        }

        /// <inheritdoc/>
        public void VisitPlainProperty(string name, decimal? value, TCtx ctx)
        {
        }

        /// <inheritdoc/>
        public void VisitPlainProperty(string name, double? value, TCtx ctx)
        {
        }

        /// <inheritdoc/>
        public void VisitPlainProperty(string name, DateTime? value, TCtx ctx)
        {
        }

        /// <inheritdoc/>
        public void VisitPlainProperty(string name, DateTimeOffset? value, TCtx ctx)
        {
        }

        /// <inheritdoc/>
        public void VisitPlainProperty(string name, Guid? value, TCtx ctx)
        {
        }

        /// <inheritdoc/>
        public void VisitPlainProperty(string name, IReadOnlyList<byte>? value, TCtx ctx)
        {
        }
    }
}
