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

    /// <summary>
    /// Creates traversal results without requiring the context type to be written explicitly.
    /// </summary>
    public static class VisitorResult
    {
        /// <summary>Continues traversal into the current node and then with its siblings.</summary>
        /// <typeparam name="TCtx">The traversal context type.</typeparam>
        /// <param name="value">The context passed to subsequent callbacks.</param>
        /// <returns>A continue result.</returns>
        public static VisitorResult<TCtx> Continue<TCtx>(TCtx value) => VisitorResult<TCtx>.Continue(value);

        /// <summary>Stops the entire traversal.</summary>
        /// <typeparam name="TCtx">The traversal context type.</typeparam>
        /// <param name="value">The final traversal context.</param>
        /// <returns>A stop result.</returns>
        public static VisitorResult<TCtx> Stop<TCtx>(TCtx value) => VisitorResult<TCtx>.Stop(value);

        /// <summary>Skips the current node's descendants and continues with its siblings.</summary>
        /// <typeparam name="TCtx">The traversal context type.</typeparam>
        /// <param name="value">The context passed to subsequent callbacks.</param>
        /// <returns>A skip-current-node result.</returns>
        public static VisitorResult<TCtx> StopNode<TCtx>(TCtx value) => VisitorResult<TCtx>.StopNode(value);
    }

    /// <summary>
    /// Carries an updated context and flow-control instruction from a syntax-tree visitor callback.
    /// </summary>
    /// <typeparam name="TCtx">The context propagated through the traversal.</typeparam>
    public readonly struct VisitorResult<TCtx>
    {
        /// <summary>Gets the context to propagate.</summary>
        public readonly TCtx Context;

        /// <summary>Gets whether the entire traversal should stop.</summary>
        public bool IsStop => this.WalkResult == WalkResult.Stop;

        internal readonly WalkResult WalkResult;

        private VisitorResult(TCtx context, WalkResult walkResult)
        {
            this.Context = context;
            this.WalkResult = walkResult;
        }

        /// <summary>Creates a result that continues into descendants and subsequent nodes.</summary>
        /// <param name="value">The context to propagate.</param>
        /// <returns>A continue result.</returns>
        public static VisitorResult<TCtx> Continue(TCtx value) => new VisitorResult<TCtx>(value, WalkResult.Continue);

        /// <summary>Creates a result that stops the entire traversal.</summary>
        /// <param name="value">The final context.</param>
        /// <returns>A stop result.</returns>
        public static VisitorResult<TCtx> Stop(TCtx value) => new VisitorResult<TCtx>(value, WalkResult.Stop);

        /// <summary>Creates a result that skips descendants of the current node.</summary>
        /// <param name="value">The context to propagate to subsequent nodes.</param>
        /// <returns>A skip-current-node result.</returns>
        public static VisitorResult<TCtx> StopNode(TCtx value) => new VisitorResult<TCtx>(value, WalkResult.StopNode);
    }

    /// <summary>
    /// Receives structural callbacks while an expression tree is walked.
    /// </summary>
    /// <typeparam name="TCtx">The context propagated through the traversal.</typeparam>
    /// <remarks>
    /// Property callbacks describe the serialized shape of a node. Most consumers that only need expression
    /// nodes can derive from <see cref="DefaultWalkerVisitorBase{TCtx}"/> and override node handling separately.
    /// </remarks>
    public interface IWalkerVisitorBase<in TCtx>
    {
        /// <summary>Called after an expression and all visited descendants have been processed.</summary>
        /// <param name="expr">The expression being completed.</param>
        /// <param name="ctx">The context produced while visiting it.</param>
        void EndVisitExpr(IExpr expr, TCtx ctx);

        /// <summary>Called before an expression-valued property is visited.</summary>
        /// <param name="name">The property name.</param><param name="isArray">Whether the property is a sequence.</param>
        /// <param name="isNull">Whether its value is null.</param><param name="ctx">The current context.</param>
        void VisitProperty(string name, bool isArray, bool isNull, TCtx ctx);
        /// <summary>Called after an expression-valued property has been visited.</summary>
        /// <param name="name">The property name.</param><param name="isArray">Whether the property is a sequence.</param>
        /// <param name="isNull">Whether its value is null.</param><param name="ctx">The current context.</param>
        void EndVisitProperty(string name, bool isArray, bool isNull, TCtx ctx);

        /// <summary>Called before an item of an expression sequence is visited.</summary>
        /// <param name="name">The containing property name.</param><param name="arrayIndex">The zero-based item index.</param><param name="ctx">The current context.</param>
        void VisitArrayItem(string name, int arrayIndex, TCtx ctx);
        /// <summary>Called after an item of an expression sequence has been visited.</summary>
        /// <param name="name">The containing property name.</param><param name="arrayIndex">The zero-based item index.</param><param name="ctx">The current context.</param>
        void EndVisitArrayItem(string name, int arrayIndex, TCtx ctx);

        /// <summary>Visits a scalar string property.</summary>
        /// <param name="name">The property name.</param><param name="value">The property value.</param><param name="ctx">The current context.</param>
        void VisitPlainProperty(string name, string? value, TCtx ctx);
        /// <summary>Visits a scalar Boolean property.</summary>
        /// <param name="name">The property name.</param><param name="value">The property value.</param><param name="ctx">The current context.</param>
        void VisitPlainProperty(string name, bool? value, TCtx ctx);
        /// <summary>Visits a scalar byte property.</summary>
        /// <param name="name">The property name.</param><param name="value">The property value.</param><param name="ctx">The current context.</param>
        void VisitPlainProperty(string name, byte? value, TCtx ctx);
        /// <summary>Visits a scalar 16-bit integer property.</summary>
        /// <param name="name">The property name.</param><param name="value">The property value.</param><param name="ctx">The current context.</param>
        void VisitPlainProperty(string name, short? value, TCtx ctx);
        /// <summary>Visits a scalar 32-bit integer property.</summary>
        /// <param name="name">The property name.</param><param name="value">The property value.</param><param name="ctx">The current context.</param>
        void VisitPlainProperty(string name, int? value, TCtx ctx);
        /// <summary>Visits a scalar 64-bit integer property.</summary>
        /// <param name="name">The property name.</param><param name="value">The property value.</param><param name="ctx">The current context.</param>
        void VisitPlainProperty(string name, long? value, TCtx ctx);
        /// <summary>Visits a scalar decimal property.</summary>
        /// <param name="name">The property name.</param><param name="value">The property value.</param><param name="ctx">The current context.</param>
        void VisitPlainProperty(string name, decimal? value, TCtx ctx);
        /// <summary>Visits a scalar double-precision property.</summary>
        /// <param name="name">The property name.</param><param name="value">The property value.</param><param name="ctx">The current context.</param>
        void VisitPlainProperty(string name, double? value, TCtx ctx);
        /// <summary>Visits a scalar date and time property.</summary>
        /// <param name="name">The property name.</param><param name="value">The property value.</param><param name="ctx">The current context.</param>
        void VisitPlainProperty(string name, DateTime? value, TCtx ctx);
        /// <summary>Visits a scalar date, time, and offset property.</summary>
        /// <param name="name">The property name.</param><param name="value">The property value.</param><param name="ctx">The current context.</param>
        void VisitPlainProperty(string name, DateTimeOffset? value, TCtx ctx);
        /// <summary>Visits a scalar GUID property.</summary>
        /// <param name="name">The property name.</param><param name="value">The property value.</param><param name="ctx">The current context.</param>
        void VisitPlainProperty(string name, Guid? value, TCtx ctx);
        /// <summary>Visits a scalar binary property.</summary>
        /// <param name="name">The property name.</param><param name="value">The property value.</param><param name="ctx">The current context.</param>
        void VisitPlainProperty(string name, IReadOnlyList<byte>? value, TCtx ctx);
    }

    /// <summary>Visits expression nodes without exposing their parent nodes.</summary>
    /// <typeparam name="TCtx">The context propagated through the traversal.</typeparam>
    public interface IWalkerVisitor<TCtx> : IWalkerVisitorBase<TCtx>
    {
        /// <summary>Called when an expression node is entered.</summary>
        /// <param name="expr">The current expression.</param><param name="typeTag">The stable serialized node-type tag.</param>
        /// <param name="ctx">The current context.</param><returns>The next context and traversal instruction.</returns>
        VisitorResult<TCtx> VisitExpr(IExpr expr, string typeTag, TCtx ctx);
    }

    /// <summary>Visits expression nodes and exposes the containing expression when one exists.</summary>
    /// <typeparam name="TCtx">The context propagated through the traversal.</typeparam>
    public interface IWalkerVisitorWithParent<TCtx> : IWalkerVisitorBase<TCtx>
    {
        /// <summary>Called when an expression node is entered.</summary>
        /// <param name="expr">The current expression.</param><param name="parent">Its parent, or null for the root.</param>
        /// <param name="typeTag">The stable serialized node-type tag.</param><param name="ctx">The current context.</param>
        /// <returns>The next context and traversal instruction.</returns>
        VisitorResult<TCtx> VisitExpr(IExpr expr, IExpr? parent, string typeTag, TCtx ctx);
    }
}
