using System.Collections.Generic;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Functions
{
    public class ExprOver : IExpr
    {
        public ExprOver(IReadOnlyList<ExprValue>? partitions, ExprOrderBy? orderBy, ExprFrameClause? frameClause)
        {
            this.Partitions = partitions;
            this.OrderBy = orderBy;
            this.FrameClause = frameClause;
        }

        public IReadOnlyList<ExprValue>? Partitions { get; }

        public ExprOrderBy? OrderBy { get; }

        public ExprFrameClause? FrameClause { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprOver(this, arg);
    }

    public class ExprFrameClause : IExpr
    {
        public ExprFrameClause(ExprFrameBorder start, ExprFrameBorder? end)
        {
            this.Start = start;
            this.End = end;
        }

        public ExprFrameBorder Start { get; }

        public ExprFrameBorder? End { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprFrameClause(this, arg);
    }

    public abstract class ExprFrameBorder : IExpr
    {
        public abstract TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg);
    }

    public class ExprValueFrameBorder : ExprFrameBorder
    {
        public ExprValueFrameBorder(ExprValue value, FrameBorderDirection frameBorderDirection)
        {
            this.Value = value;
            this.FrameBorderDirection = frameBorderDirection;
        }

        public ExprValue Value { get; }

        public FrameBorderDirection FrameBorderDirection { get; }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprValueFrameBorder(this, arg);
    }

    public class ExprCurrentRowFrameBorder : ExprFrameBorder
    {
        public static readonly ExprCurrentRowFrameBorder Instance = new ExprCurrentRowFrameBorder();

        private ExprCurrentRowFrameBorder()
        {
        }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprCurrentRowFrameBorder(this, arg);
    }

    public class ExprUnboundedFrameBorder : ExprFrameBorder
    {
        public ExprUnboundedFrameBorder(FrameBorderDirection frameBorderDirection)
        {
            this.FrameBorderDirection = frameBorderDirection;
        }

        public FrameBorderDirection FrameBorderDirection { get; }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprUnboundedFrameBorder(this, arg);
    }

    public enum FrameBorderDirection
    {
        Preceding,
        Following
    }
}