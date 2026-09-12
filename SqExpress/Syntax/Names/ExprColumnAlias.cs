using System;

namespace SqExpress.Syntax.Names;

public class ExprColumnAlias : IExprName, IEquatable<ExprColumnAlias>
{
    private string? _lowerInvariantName;

    public ExprColumnAlias(string name)
    {
        this.Name = name.Trim();
    }

    public string Name { get; }

    public string LowerInvariantName
    {
        get
        {
            this._lowerInvariantName ??= this.Name.ToLowerInvariant();
            return this._lowerInvariantName;
        }
    }

    public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprColumnAlias(this, arg);

    public static implicit operator ExprColumnAlias(string name)=> new ExprColumnAlias(name);

    public static implicit operator ExprColumnAlias(ExprColumn column) => new ExprColumnAlias(column.ColumnName.Name);

    public static implicit operator ExprColumnAlias(ExprColumnName column) => new ExprColumnAlias(column.Name);

    public bool Equals(ExprColumnAlias? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return this.Name == other.Name;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ExprColumnAlias) obj);
    }

    public override int GetHashCode()
    {
        return this.Name.GetHashCode();
    }
}