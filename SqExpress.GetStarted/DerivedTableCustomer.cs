using static SqExpress.SqQueryBuilder;

namespace SqExpress.GetStarted
{
    public class DerivedTableCustomer : DerivedTableBase
    {
        public readonly Int32CustomColumn CustomerId;

        public readonly Int16CustomColumn Type;

        public readonly StringCustomColumn Name;

        public DerivedTableCustomer(Alias alias = default) : base(alias)
        {
            this.CustomerId = this.CreateInt32Column("CustomerId");
            this.Type = this.CreateInt16Column("Type");
            this.Name = this.CreateStringColumn("Name");
        }

        protected override IExprSubQuery CreateQuery()
        {
            var tUser = new TableUser();
            var tCompany = new TableCompany();
            var tCustomer = new TableCustomer();

            return Select(
                    tCustomer.CustomerId.As(this.CustomerId),
                    Case()
                        .When(IsNotNull(tUser.UserId))
                        .Then(Cast(Literal(1), SqlType.Int16))
                        .When(IsNotNull(tCompany.CompanyId))
                        .Then(Cast(Literal(2), SqlType.Int16))
                        .Else(Null)
                        .As(this.Type),
                    Case()
                        .When(IsNotNull(tUser.UserId))
                        .Then(tUser.FirstName + " " + tUser.LastName)
                        .When(IsNotNull(tCompany.CompanyId))
                        .Then(tCompany.CompanyName)
                        .Else(Null)
                        .As(this.Name)
                )
                .From(tCustomer)
                .LeftJoin(tUser, on: tUser.UserId == tCustomer.UserId)
                .LeftJoin(tCompany, on: tCompany.CompanyId == tCustomer.CompanyId)
                .Done();
        }
    }
}