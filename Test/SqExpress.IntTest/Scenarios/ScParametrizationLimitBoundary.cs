using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.IntTest.Context;
using SqExpress.Syntax.Value;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScParametrizationLimitBoundary : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            if (context.ParametrizationMode == ParametrizationMode.None)
            {
                return;
            }

            var table = new ParamLimitTable();
            await table.Script.DropIfExist().Exec(context.Database);
            await table.Script.Create().Exec(context.Database);

            var boundaryCase = BuildBoundaryCase(context.Dialect);

            await InsertRows(table, context, boundaryCase.BoundaryRows, boundaryCase.BoundaryColumns);

            var count = (long?)await Select(Cast(CountOne(), SqlType.Int64)).From(table).QueryScalar(context.Database);
            if (count != boundaryCase.BoundaryRows)
            {
                throw new Exception($"{boundaryCase.BoundaryRows} rows are expected but was {count}");
            }

            await table.Script.Drop().Exec(context.Database);
            await table.Script.Create().Exec(context.Database);

            if (context.ParametrizationMode == ParametrizationMode.ThrowOnLimit)
            {
                var throwExpected = false;
                try
                {
                    await InsertRows(table, context, boundaryCase.OverflowRows, boundaryCase.OverflowColumns);
                }
                catch (SqExpressException)
                {
                    throwExpected = true;
                }

                if (!throwExpected)
                {
                    throw new Exception(
                        $"Expected SqExpressException for >{boundaryCase.ParametersLimit} parameters in ThrowOnLimit mode");
                }
            }
            else
            {
                await InsertRows(table, context, boundaryCase.OverflowRows, boundaryCase.OverflowColumns);
                count = (long?)await Select(Cast(CountOne(), SqlType.Int64)).From(table).QueryScalar(context.Database);
                if (count != boundaryCase.OverflowRows)
                {
                    throw new Exception($"{boundaryCase.OverflowRows} rows are expected but was {count}");
                }
            }

            await table.Script.Drop().Exec(context.Database);
        }

        private static BoundaryCase BuildBoundaryCase(SqlDialect dialect)
        {
            return dialect switch
            {
                SqlDialect.TSql => new BoundaryCase(
                    parametersLimit: 2000,
                    boundaryRows: 1000,
                    boundaryColumns: 2,
                    overflowRows: 667,
                    overflowColumns: 3
                ),
                _ => new BoundaryCase(
                    parametersLimit: 65535,
                    boundaryRows: 21845,
                    boundaryColumns: 3,
                    overflowRows: 21846,
                    overflowColumns: 3
                )
            };
        }

        private static async Task InsertRows(
            ParamLimitTable table,
            IScenarioContext context,
            int rowCount,
            int columnsCount)
        {
            var rows = Enumerable.Range(1, rowCount)
                .Select(i => CreateRow(i, columnsCount))
                .ToList();

            switch (columnsCount)
            {
                case 2:
                    await InsertInto(table, table.Value1, table.Value2)
                        .Values(rows)
                        .Exec(context.Database);
                    break;
                case 3:
                    await InsertInto(table, table.Value1, table.Value2, table.Value3)
                        .Values(rows)
                        .Exec(context.Database);
                    break;
                default:
                    throw new Exception($"Unsupported number of columns: {columnsCount}");
            }
        }

        private static IReadOnlyList<ExprValue> CreateRow(int i, int columnsCount)
        {
            return columnsCount switch
            {
                2 => new ExprValue[] { Literal(i), Literal(-i) },
                3 => new ExprValue[] { Literal(i), Literal(i + 10000), Literal(i + 20000) },
                _ => throw new Exception($"Unsupported number of columns: {columnsCount}")
            };
        }

        private class ParamLimitTable : TempTableBase
        {
            public ParamLimitTable(Alias alias = default) : base("ParamLimitProbe", alias)
            {
                this.Value1 = this.CreateInt32Column("Value1");
                this.Value2 = this.CreateNullableInt32Column("Value2");
                this.Value3 = this.CreateNullableInt32Column("Value3");
            }

            public Int32TableColumn Value1 { get; }

            public NullableInt32TableColumn Value2 { get; }

            public NullableInt32TableColumn Value3 { get; }
        }

        private readonly struct BoundaryCase
        {
            public readonly int ParametersLimit;
            public readonly int BoundaryRows;
            public readonly int BoundaryColumns;
            public readonly int OverflowRows;
            public readonly int OverflowColumns;

            public BoundaryCase(
                int parametersLimit,
                int boundaryRows,
                int boundaryColumns,
                int overflowRows,
                int overflowColumns)
            {
                this.ParametersLimit = parametersLimit;
                this.BoundaryRows = boundaryRows;
                this.BoundaryColumns = boundaryColumns;
                this.OverflowRows = overflowRows;
                this.OverflowColumns = overflowColumns;
            }
        }
    }
}
