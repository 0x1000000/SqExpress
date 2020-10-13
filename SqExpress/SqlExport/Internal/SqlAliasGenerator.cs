using System;
using System.Collections.Generic;
using SqExpress.Syntax.Names;

namespace SqExpress.SqlExport.Internal
{
    internal class SqlAliasGenerator
    {
        private int _counter;

        private readonly Dictionary<Guid, string> _dictionary = new Dictionary<Guid, string>();

        public string GetAlias(ExprAliasGuid alias)
        {
            if(this._dictionary.TryGetValue(alias.Id, out var result))
            {
                return result;
            }

            result = "A" + this._counter++;

            this._dictionary.Add(alias.Id, result);

            return result;
        }
    }
}