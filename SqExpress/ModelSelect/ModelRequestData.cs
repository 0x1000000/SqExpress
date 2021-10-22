using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.Syntax.Select;

namespace SqExpress.ModelSelect
{
    public readonly struct ModelRequestData<TRes>
    {
        public readonly IExprQuery Expr;

        public readonly Func<ISqDataRecordReader, TRes> Mapper;

        internal ModelRequestData(IExprQuery expr, Func<ISqDataRecordReader, TRes> mapper)
        {
            this.Expr = expr;
            this.Mapper = mapper;
        }
    }

    public readonly struct ModelRangeRequestData<TRes>
    {
        public readonly ExprSelectOffsetFetch Expr;

        public readonly Func<ISqDataRecordReader, TRes> Mapper;

        internal ModelRangeRequestData(ExprSelectOffsetFetch expr, Func<ISqDataRecordReader, TRes> mapper)
        {
            this.Expr = expr;
            this.Mapper = mapper;
        }
    }
}