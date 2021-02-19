using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Tables.Derived
{
    public class CustomerName : DerivedTableBase
    {
        private readonly Customer _tCustomer = TableList.Customer();

        public enum CustomerType : short
        {
            User = 1,
            Company = 2
        }

        public CustomerName()
        {
            this.CustomerId = this._tCustomer.CustomerId.AddToDerivedTable(this);
            this.CustomerTypeId = this.CreateInt16Column("CustomerTypeId");
            this.Name = this.CreateStringColumn("Name");
        }

        public Int32CustomColumn CustomerId { get; }

        public Int16CustomColumn CustomerTypeId { get; }

        public StringCustomColumn Name { get; }

        protected override IExprSubQuery CreateQuery()
        {
            var tUser = TableList.User();
            var tCompany = TableList.Company();

            return Select(
                    this._tCustomer.CustomerId,
                    Case()
                        .When(IsNotNull(tUser.UserId))
                        .Then((short)CustomerType.User)

                        .When(IsNotNull(tCompany.CompanyId))
                        .Then((short)CustomerType.Company)

                        .Else(Null)
                        .As(this.CustomerTypeId),
                    Case()
                        .When(IsNotNull(tUser.UserId))
                        .Then(tUser.FirstName + " " + tUser.LastName)

                        .When(IsNotNull(tCompany.CompanyId))
                        .Then(tCompany.CompanyName)

                        .Else("-")
                        .As(this.Name)
                    )
                .From(this._tCustomer)
                .LeftJoin(tUser, on: this._tCustomer.UserId == tUser.UserId)
                .LeftJoin(tCompany, on: this._tCustomer.CompanyId == tCompany.CompanyId)
                .Done();
        }
    }
}