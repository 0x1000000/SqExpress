using SqExpress.TableDecalationAttributes;

namespace SqExpress.GetStarted;

[TableDescriptor("dbo", "Customer")]
[Int32Column("CustomerId", Pk = true, Identity = true)]
[NullableInt32Column("UserId", FkTable = "User", FkColumn = "UserId")]
[NullableInt32Column("CompanyId", FkTable = "Company", FkColumn = "CompanyId")]
[Index("UserId", "CompanyId", Unique = true)]
[Index("CompanyId", "UserId", Unique = true)]
public partial class TableCustomer;