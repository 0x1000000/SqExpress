using Microsoft.CodeAnalysis;

namespace SqExpress.Analyzers.Diagnostics
{
    internal static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor ConvertSqTSqlParserParseCall = new DiagnosticDescriptor(
            id: "SQEX001",
            title: "SqTSqlParser call can be converted to SqExpress",
            messageFormat: "Call to '{0}' uses compile-time SQL and can be converted to SqExpress code",
            category: "Migration",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Identifies SqTSqlParser.Parse calls with compile-time SQL strings as candidates for generated SqExpress code.");

        public static readonly DiagnosticDescriptor SqTSqlParserParseCannotResolveTables = new DiagnosticDescriptor(
            id: "SQEX002",
            title: "SqTSqlParser call cannot be converted because table classes are missing",
            messageFormat: "{0}",
            category: "Migration",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Reports SqTSqlParser.Parse calls whose referenced SQL tables cannot be mapped uniquely to source-visible SqExpress TableBase classes.");
    }
}
