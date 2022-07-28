using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.IntTest.Tables.Models;

namespace SqExpress.IntTest.Scenarios
{
    public class ScSelectSeveralModelsWithPrefix : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var tblCustomer = AllTables.GetItCustomer();
            var tblUser = AllTables.GetItUser();
            var tblCompany = AllTables.GetItCompany();

            var result = await SqQueryBuilder.Select(
                    Customer.GetColumnsWithPrefix(tblCustomer, "cst")
                        .Combine(UserName.GetColumnsWithPrefix(tblUser, "usr"))
                        .Combine(CompanyName.GetColumnsWithPrefix(tblCompany, "comp")))
                .From(tblCustomer)
                .LeftJoin(tblUser, on: tblUser.UserId == tblCustomer.UserId)
                .LeftJoin(tblCompany, on: tblCompany.CompanyId == tblCustomer.CompanyId)
                .QueryList(context.Database,
                    r => (
                        Customer: Customer.ReadWithPrefix(r, tblCustomer, "cst"),
                        User: UserName.IsNullWithPrefix(r, tblUser, "usr")
                            ? null
                            : UserName.ReadWithPrefix(r, tblUser, "usr"),
                        Company: CompanyName.IsNullWithPrefix(r, tblCompany, "comp")
                            ? null
                            : CompanyName.ReadWithPrefix(r, tblCompany, "comp")));
        }
    }
}