using System.Collections.Generic;

namespace SqExpress.SqlTranspiler
{
    public sealed class SqExpressSqlInlineTranspileResult
    {
        public SqExpressSqlInlineTranspileResult(
            string statementKind,
            string expressionVariableName,
            IReadOnlyList<SqExpressSqlInlineParameter> parameters,
            IReadOnlyList<string> localDeclarations,
            IReadOnlyList<string> nestedTypeDeclarations)
        {
            this.StatementKind = statementKind;
            this.ExpressionVariableName = expressionVariableName;
            this.Parameters = parameters;
            this.LocalDeclarations = localDeclarations;
            this.NestedTypeDeclarations = nestedTypeDeclarations;
        }

        public string StatementKind { get; }

        public string ExpressionVariableName { get; }

        public IReadOnlyList<SqExpressSqlInlineParameter> Parameters { get; }

        public IReadOnlyList<string> LocalDeclarations { get; }

        public IReadOnlyList<string> NestedTypeDeclarations { get; }
    }

    public sealed class SqExpressSqlInlineParameter
    {
        public SqExpressSqlInlineParameter(string parameterName, string variableName, string defaultDeclaration, bool isList)
        {
            this.ParameterName = parameterName;
            this.VariableName = variableName;
            this.DefaultDeclaration = defaultDeclaration;
            this.IsList = isList;
        }

        public string ParameterName { get; }

        public string VariableName { get; }

        public string DefaultDeclaration { get; }

        public bool IsList { get; }
    }

    public sealed class SqExpressSqlInlineTableBinding
    {
        public SqExpressSqlInlineTableBinding(string tableKey, string alias, string variableName, string typeName)
        {
            this.TableKey = tableKey;
            this.Alias = alias;
            this.VariableName = variableName;
            this.TypeName = typeName;
        }

        public string TableKey { get; }

        public string Alias { get; }

        public string VariableName { get; }

        public string TypeName { get; }
    }
}
