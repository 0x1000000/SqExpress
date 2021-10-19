using System.Collections.Generic;
using SqExpress.Syntax.Names;

namespace SqExpress
{
    public interface ISqModelReader<out TEntity, in TTable>
    {
        IReadOnlyList<ExprColumn> GetColumns(TTable table);

        TEntity Read(ISqDataRecordReader record, TTable table);
    }
}