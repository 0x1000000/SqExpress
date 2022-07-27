using System;
using System.Collections.Generic;
using SqExpress.Syntax;

namespace SqExpress.SyntaxTreeOperations
{
    internal enum WalkResult
    {
        Continue,
        StopNode,
        Stop
    }

    public static class VisitorResult
    {
        public static VisitorResult<TCtx> Continue<TCtx>(TCtx value) => VisitorResult<TCtx>.Continue(value);

        public static VisitorResult<TCtx> Stop<TCtx>(TCtx value) => VisitorResult<TCtx>.Stop(value);

        public static VisitorResult<TCtx> StopNode<TCtx>(TCtx value) => VisitorResult<TCtx>.StopNode(value);
    }

    public readonly struct VisitorResult<TCtx>
    {
        public readonly TCtx Context;

        public bool IsStop => this.WalkResult == WalkResult.Stop;

        internal readonly WalkResult WalkResult;

        private VisitorResult(TCtx context, WalkResult walkResult)
        {
            this.Context = context;
            this.WalkResult = walkResult;
        }

        public static VisitorResult<TCtx> Continue(TCtx value) => new VisitorResult<TCtx>(value, WalkResult.Continue);

        public static VisitorResult<TCtx> Stop(TCtx value) => new VisitorResult<TCtx>(value, WalkResult.Stop);

        public static VisitorResult<TCtx> StopNode(TCtx value) => new VisitorResult<TCtx>(value, WalkResult.StopNode);
    }

    public interface IWalkerVisitorBase<in TCtx>
    {
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

    public interface IWalkerVisitor<TCtx> : IWalkerVisitorBase<TCtx>
    {
        VisitorResult<TCtx> VisitExpr(IExpr expr, string typeTag, TCtx ctx);
    }

    public interface IWalkerVisitorWithParent<TCtx> : IWalkerVisitorBase<TCtx>
    {
        VisitorResult<TCtx> VisitExpr(IExpr expr, IExpr? parent, string typeTag, TCtx ctx);
    }
}