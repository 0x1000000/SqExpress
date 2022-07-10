using System;
using System.Collections.Generic;
using SqExpress.Syntax;

namespace SqExpress.SyntaxTreeOperations
{
    public readonly struct VisitorResult<TCtx>
    {
        public readonly TCtx Context;

        public readonly bool IsStop;

        private VisitorResult(TCtx context, bool isTop)
        {
            this.Context = context;
            this.IsStop = isTop;
        }

        public static VisitorResult<TCtx> Continue(TCtx value) => new VisitorResult<TCtx>(value, false);

        public static VisitorResult<TCtx> Stop(TCtx value) => new VisitorResult<TCtx>(value, true);
    }

    public interface IWalkerVisitor<TCtx>
    {
        VisitorResult<TCtx> VisitExpr(IExpr expr, string typeTag, TCtx ctx);
        void EndVisitExpr(IExpr expr, TCtx ctx);

        void VisitProperty(string name, bool isArray, bool isNull, TCtx ctx);
        void EndVisitProperty(string name, bool isArray, bool isNull, TCtx ctx);

        void VisitArrayItem(string name, int arrayIndex, TCtx ctx);
        void EndVisitArrayItem(string name, int arrayIndex, TCtx ctx);

        void VisitPlainProperty(string name, string? value, TCtx ctx);
        void VisitPlainProperty(string name, bool? value, TCtx ctx);
        void VisitPlainProperty(string name, byte? value, TCtx ctx);
        void VisitPlainProperty(string name, short? value, TCtx ctx);
        void VisitPlainProperty(string name, int? value, TCtx ctx);
        void VisitPlainProperty(string name, long? value, TCtx ctx);
        void VisitPlainProperty(string name, decimal? value, TCtx ctx);
        void VisitPlainProperty(string name, double? value, TCtx ctx);
        void VisitPlainProperty(string name, DateTime? value, TCtx ctx);
        void VisitPlainProperty(string name, DateTimeOffset? value, TCtx ctx);
        void VisitPlainProperty(string name, Guid? value, TCtx ctx);
        void VisitPlainProperty(string name, IReadOnlyList<byte>? value, TCtx ctx);
    }
}