using SqExpress.TableDeclarationAttributes;

namespace SqExpress.GetStarted;

[TableDescriptor("dbo", "Company")]
[Int32Column("CompanyId", Pk = true, Identity = true, SqModels = "CompanyName.Id")]
[StringColumn("CompanyName", MaxLength = 250, SqModels = "CompanyName.Name")]
[Int32Column("Version", DefaultValue = "0", SqModels = "AuditData")]
[DateTimeColumn("ModifiedAt", DefaultValue = "$utcNow", SqModels = "AuditData")]
public partial class TableCompany;