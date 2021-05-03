using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Functions.Known
{
    public class ExprDateAdd : ExprValue
    {
        public ExprDateAdd(DateAddDatePart datePart, int number, ExprValue date)
        {
            this.DatePart = datePart;
            this.Number = number;
            this.Date = date;
        }

        public DateAddDatePart DatePart { get; }

        public int Number { get; }

        public ExprValue Date { get; }

        public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprDateAdd(this, arg);
    }

    public enum DateAddDatePart
    {
        Year,
        Month,
        Day,
        Week,
        Hour,
        Minute,
        Second,
        Millisecond
    }
}