using System.Collections.Generic;

namespace SqExpress.Syntax.Names
{
    public class ExprNameEqualityComparer
    {
        public static readonly IEqualityComparer<IExprName> CaseSensitive = new CaseSensitiveComparer();

        public static readonly IEqualityComparer<IExprName> CaseInsensitive = new CaseInsensitiveComparer();

        private class CaseSensitiveComparer : IEqualityComparer<IExprName>
        {
            public bool Equals(IExprName? x, IExprName? y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Name == y.Name;
            }

            public int GetHashCode(IExprName obj)
            {
                return obj.Name.GetHashCode();
            }
        }

        private class CaseInsensitiveComparer : IEqualityComparer<IExprName>
        {
            public bool Equals(IExprName? x, IExprName? y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.LowerInvariantName == y.LowerInvariantName;
            }

            public int GetHashCode(IExprName obj)
            {
                return obj.LowerInvariantName.GetHashCode();
            }
        }
    }
}