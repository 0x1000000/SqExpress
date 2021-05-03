using SqExpress.Syntax.Names;

namespace SqExpress
{
    public interface ISqModelReader<out TEntity, in TTable>
    {
        TableColumn[] GetColumns(TTable table);

        TEntity Read(ISqDataRecordReader record, TTable table);
    }

    public interface ISqModelDerivedReaderReader<out TEntity, in TDerivedTable>
    {
        ExprColumn[] GetColumns(TDerivedTable table);

        TEntity Read(ISqDataRecordReader record, TDerivedTable table);
    }
}