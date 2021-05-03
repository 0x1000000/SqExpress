using System;

namespace SqExpress.CodeGenUtil.CodeGen
{
    internal enum BaseTypeKindTag
    {
        TableBase,
        TempTableBase,
        DerivedTableBase
    }

    internal static class BaseTypeKindTagExtensions
    {
        public static TRes Switch<TRes>(this BaseTypeKindTag tag,TRes tableBaseRes, TRes tempTableBaseRes, TRes derivedTableBaseRes)
        {
            switch (tag)
            {
                case BaseTypeKindTag.TableBase:
                    return tableBaseRes;
                case BaseTypeKindTag.TempTableBase:
                    return tempTableBaseRes;
                case BaseTypeKindTag.DerivedTableBase:
                    return derivedTableBaseRes;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tag), tag, null);
            }
        }
    }
}