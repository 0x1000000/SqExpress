using System;

namespace SqExpress.Syntax.Names
{
    public class ExprDbSchema : IExpr, IEquatable<ExprDbSchema>
    {
        public ExprDbSchema(ExprDatabaseName? database, ExprSchemaName schema)
        {
            this.Database = database;
            this.Schema = schema;
        }

        public ExprDatabaseName? Database { get; }

        public ExprSchemaName Schema { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprDbSchema(this, arg);

        public bool Equals(ExprDbSchema? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(this.Database, other.Database) && this.Schema.Equals(other.Schema);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ExprDbSchema) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((this.Database != null ? this.Database.GetHashCode() : 0) * 397) ^ this.Schema.GetHashCode();
            }
        }
    }
}