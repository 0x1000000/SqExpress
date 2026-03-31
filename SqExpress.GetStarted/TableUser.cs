using SqExpress.TableDecalationAttributes;

namespace SqExpress.GetStarted;

[TableDescriptor("dbo", "User")]
[Int32Column("UserId", Pk = true, Identity = true, SqModels = "UserName.Id")]
[StringColumn("FirstName", MaxLength = 255, Unicode = true, SqModels = "UserName")]
[StringColumn("LastName", MaxLength = 255, Unicode = true, SqModels = "UserName")]
[Int32Column("Version", DefaultValue = "0", SqModels = "AuditData")]
[DateTimeColumn("ModifiedAt", DefaultValue = "$utcNow", SqModels = "AuditData")]
public partial class TableUser;