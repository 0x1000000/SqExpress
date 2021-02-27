using System;
using System.Collections.Generic;
using SqExpress.Syntax.Boolean;

namespace SqExpress.Syntax.Select
{
    public interface IExprTableSource : IExpr
    {
        public TableMultiplication ToTableMultiplication();
    }

    public readonly struct TableMultiplication : IEquatable<TableMultiplication>
    {
        public readonly IReadOnlyList<IExprTableSource> Tables;
        public readonly ExprBoolean? On;

        public TableMultiplication(IReadOnlyList<IExprTableSource> tables, ExprBoolean? on)
        {
            this.Tables = tables;
            this.On = on;
        }

        public void Deconstruct(out IReadOnlyList<IExprTableSource> tables, out ExprBoolean? on)
        {
            tables = this.Tables;
            on = this.On;
        }

        public bool Equals(TableMultiplication other)
        {
            return this.Tables.Equals(other.Tables) && Equals(this.On, other.On);
        }

        public override bool Equals(object? obj)
        {
            return obj is TableMultiplication other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (this.Tables.GetHashCode() * 397) ^ (this.On != null ? this.On.GetHashCode() : 0);
            }
        }
    }
}