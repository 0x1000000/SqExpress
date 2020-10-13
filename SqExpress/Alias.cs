using System;
using SqExpress.Syntax.Names;

namespace SqExpress
{
    public readonly struct Alias
    {
        private readonly bool _notAuto;

        private readonly string? _name;

        private Alias(bool notAuto, string? name)
        {
            this._notAuto = notAuto;
            this._name = name;
        }

        public static Alias Empty => new Alias(true, null);

        public static Alias Auto => new Alias(false, null);

        public static implicit operator Alias(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new Alias(true, null);
            }
            return new Alias(true, name);
        }

        public IExprAlias? BuildAliasExpression()
        {
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