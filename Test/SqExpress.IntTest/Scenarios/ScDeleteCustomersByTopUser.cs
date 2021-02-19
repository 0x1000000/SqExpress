using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScDeleteCustomersByTopUser : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var tCustomer = TableList.Customer();
            var tUser = TableList.User();
            var topUsers = new TopFirstNameUsers(5);

            await Delete(tCustomer)
                .From(tCustomer)
                .InnerJoin(topUsers, @on: topUsers.UserId == tCustomer.UserId)
                .All()
                .Exec(context.Database);

            context.WriteLine("Deleted:");

            var list = await Select(tUser.FirstName, tUser.LastName)
                .From(tUser)
                .Where(!Exists(SelectOne().From(tCustomer).Where(tCustomer.UserId == tUser.UserId)))
                .OrderBy(tUser.FirstName)
                .QueryList(context.Database, r=> (FirstName: tUser.FirstName.Read(r), LastName: tUser.LastName.Read(r)));

            foreach (var name in list)
            {
                context.WriteLine($"{name.FirstName} {name.LastName}");
            }
        }

        private class TopFirstNameUsers : DerivedTableBase
        {
            private readonly User _tUser;

            private readonly int _top;

            public TopFirstNameUsers(int top, Alias alias = default) : base(alias)
            {
                this._top = top;
                this._tUser = TableList.User();
                this.UserId = this._tUser.UserId.AddToDerivedTable(this);
            }

            public Int32CustomColumn UserId { get; }

            protected override IExprSubQuery CreateQuery()
            {
                return Select(this._tUser.UserId)
                    .From(this._tUser)
                    .OrderBy(this._tUser.FirstName)
                    .OffsetFetch(0, this._top)
                    .Done();
            }
        }

    }
}