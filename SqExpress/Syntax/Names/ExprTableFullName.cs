﻿using System;

namespace SqExpress.Syntax.Names
{
    public class ExprTableFullName : IExprTableFullName, IEquatable<ExprTableFullName>
    {
        public ExprTableFullName(ExprDbSchema? dbSchema, ExprTableName tableName)
        {
            this.DbSchema = dbSchema;
            this.TableName = tableName;
        }

        public ExprDbSchema? DbSchema { get; }

        public ExprTableName TableName { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTableFullName(this, arg);

        public ExprTableFullName AsExprTableFullName()
        {
            return this;
        }

        IExprTableFullName IExprTableFullName.WithTableName(string tableName) => new ExprTableFullName(this.DbSchema, new ExprTableName(tableName));

        IExprTableFullName IExprTableFullName.WithSchemaName(string? schemaName) => new ExprTableFullName(
            schemaName == null ? null : new ExprDbSchema(this.DbSchema?.Database, new ExprSchemaName(schemaName)),
            this.TableName
        );

        string? IExprTableFullName.SchemaName => this.DbSchema?.Schema.Name;

        string? IExprTableFullName.LowerInvariantSchemaName => this.DbSchema?.Schema.LowerInvariantName;

        string IExprTableFullName.TableName => this.TableName.Name;

        string IExprTableFullName.LowerInvariantTableName => this.TableName.LowerInvariantName;

        public bool Equals(ExprTableFullName? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(this.DbSchema, other.DbSchema) && this.TableName.Equals(other.TableName);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ExprTableFullName) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((this.DbSchema != null ? this.DbSchema.GetHashCode() : 0) * 397) ^ this.TableName.GetHashCode();
            }
        }
    }
}