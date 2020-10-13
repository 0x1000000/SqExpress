using System;
using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress.QueryBuilders.Case
{
    public readonly struct CaseThen
    {
        private readonly List<ExprCaseWhenThen> _cases;

        private readonly ExprBoolean _nextCondition;

        internal CaseThen(List<ExprCaseWhenThen> cases, ExprBoolean nextCondition)
        {
            this._cases = cases;
            this._nextCondition = nextCondition;
        }

        public CaseThenNext Then(int? value) => this.Then(SqQueryBuilder.Literal(value));
        public CaseThenNext Then(int value) => this.Then(SqQueryBuilder.Literal(value));
        public CaseThenNext Then(string value) => this.Then(SqQueryBuilder.Literal(value));
        public CaseThenNext Then(Guid? value) => this.Then(SqQueryBuilder.LiteralCast(value));
        public CaseThenNext Then(Guid value) => this.Then(SqQueryBuilder.LiteralCast(value));
        public CaseThenNext Then(DateTime? value) => this.Then(SqQueryBuilder.LiteralCast(value));
        public CaseThenNext Then(DateTime value) => this.Then(SqQueryBuilder.LiteralCast(value));
        public CaseThenNext Then(bool? value) => this.Then(SqQueryBuilder.LiteralCast(value));
        public CaseThenNext Then(bool value) => this.Then(SqQueryBuilder.LiteralCast(value));
        public CaseThenNext Then(byte? value) => this.Then(SqQueryBuilder.LiteralCast(value));
        public CaseThenNext Then(byte value) => this.Then(SqQueryBuilder.LiteralCast(value));
        public CaseThenNext Then(short? value) => this.Then(SqQueryBuilder.LiteralCast(value));
        public CaseThenNext Then(short value) => this.Then(SqQueryBuilder.LiteralCast(value));
        public CaseThenNext Then(long? value) => this.Then(SqQueryBuilder.LiteralCast(value));
        public CaseThenNext Then(long value) => this.Then(SqQueryBuilder.LiteralCast(value));
        public CaseThenNext Then(decimal? value) => this.Then(SqQueryBuilder.LiteralCast(value));
        public CaseThenNext Then(decimal value) => this.Then(SqQueryBuilder.LiteralCast(value));
        public CaseThenNext Then(double? value) => this.Then(SqQueryBuilder.LiteralCast(value));
        public CaseThenNext Then(double value) => this.Then(SqQueryBuilder.LiteralCast(value));
        public CaseThenNext Then(IReadOnlyList<byte> value) => this.Then(SqQueryBuilder.Literal(value));

        public CaseThenNext Then(ExprValue value)
        {
            this._cases.AssertFatalNotNull($"You cannot use {nameof(CaseThen)} structure directly");
            this._cases.Add(new ExprCaseWhenThen(this._nextCondition, value));
            return new CaseThenNext(this._cases);
        }
    }
}