using System;
using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Value;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.QueryBuilders.Case
{
    public readonly struct CaseThenNext
    {
        private readonly List<ExprCaseWhenThen> _cases;

        internal CaseThenNext(List<ExprCaseWhenThen> cases)
        {
            this._cases = cases;
        }

        public CaseThen When(ExprBoolean condition) => new CaseThen(this._cases, condition);

        public ExprCase Else(int? value) => this.Else(Literal(value));
        public ExprCase Else(int value) => this.Else(Literal(value));
        public ExprCase Else(string value) => this.Else(Literal(value));
        public ExprCase Else(Guid? value) => this.Else(LiteralCast(value));
        public ExprCase Else(Guid value) => this.Else(LiteralCast(value));
        public ExprCase Else(DateTime? value) => this.Else(LiteralCast(value));
        public ExprCase Else(DateTime value) => this.Else(LiteralCast(value));
        public ExprCase Else(bool? value) => this.Else(LiteralCast(value));
        public ExprCase Else(bool value) => this.Else(LiteralCast(value));
        public ExprCase Else(byte? value) => this.Else(LiteralCast(value));
        public ExprCase Else(byte value) => this.Else(LiteralCast(value));
        public ExprCase Else(short? value) => this.Else(LiteralCast(value));
        public ExprCase Else(short value) => this.Else(LiteralCast(value));
        public ExprCase Else(long? value) => this.Else(LiteralCast(value));
        public ExprCase Else(long value) => this.Else(LiteralCast(value));
        public ExprCase Else(decimal? value) => this.Else(LiteralCast(value));
        public ExprCase Else(decimal value) => this.Else(LiteralCast(value));
        public ExprCase Else(double? value) => this.Else(LiteralCast(value));
        public ExprCase Else(IReadOnlyList<byte> value) => this.Else(Literal(value));

        public ExprCase Else(ExprValue value) => new ExprCase(this._cases, value);
    }
}