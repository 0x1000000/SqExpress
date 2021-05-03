using System;
using SqExpress.Syntax.Names;

namespace SqExpress
{
    public readonly struct Alias
    {
        private readonly bool _notAuto;

        private readonly string? _name;

        private readonly IExprAlias? _proxy;

        private Alias(bool notAuto, string? name, IExprAlias? proxy)
        {
            this._notAuto = notAuto;
            this._name = name;
            this._proxy = proxy;
        }

        public static Alias Empty => new Alias(true, null, null);

        public static Alias Auto => new Alias(false, null, null);

        public static Alias From(IExprAlias exprAlias) => new Alias(false, null, exprAlias);

        public static implicit operator Alias(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new Alias(true, null, null);
            }
            return new Alias(true, name, null);
        }

        public IExprAlias? BuildAliasExpression()
        {
            if (this._proxy != null)
            {
                return this._proxy;
            }

            if (!this._notAuto)
            {
                return new ExprAliasGuid(Guid.NewGuid());
            }

            if (this._name == null || string.IsNullOrWhiteSpace(this._name))
            {
                return null;
            }

            return new ExprAlias(this._name);
        }
    }
}