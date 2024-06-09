using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Functions.Known;

public class ExprDateDiff : ExprValue
{
    public ExprDateDiff(DateDiffDatePart datePart, ExprValue startDate, ExprValue endDate)
    {
        this.DatePart = datePart;
        this.StartDate = startDate;
        this.EndDate = endDate;
    }

    public DateDiffDatePart DatePart { get; }

    public ExprValue StartDate { get; }

    public ExprValue EndDate { get; }

    public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprDateDiff(this, arg);
}

public enum DateDiffDatePart
{
    Year = 0,
    Month = 1,
    Day = 2,
    //Week = 3, It is challenging to create a polyfill for PGSQL and MYSQL
    Hour = 4,
    Minute = 5,
    Second = 6,
    Millisecond = 7
}
