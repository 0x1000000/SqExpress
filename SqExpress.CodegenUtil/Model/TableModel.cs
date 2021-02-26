using System.Collections.Generic;

namespace SqExpress.CodeGenUtil.Model
{
    public record TableModel(
        TableNameModel NameModel,
        IReadOnlyList<ColumnModel> Columns
    );
}