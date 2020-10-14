using System;

namespace SqExpress.Syntax.Names
{
    public class ExprTableFullName : IExprColumnSource, IEquatable<ExprTableFullName>
    {
        public ExprTableFullName(ExprSchemaName schema, ExprTableName tableName)
        {
            this.Schema = schema;
            this.TableName = tableName;
        }

        public ExprSchemaName Schema { get; }

        public ExprTableName TableName { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTableFullName(this, arg);

        public bool Equals(ExprTableFullName? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return this.Schema.Equals(other.Schema) && this.TableName.Equals(other.TableName);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return this.Equals((ExprTableFullName) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (this.Schema.GetHashCode() * 397) ^ this.TableName.GetHashCode();
            }
        }
    }
}