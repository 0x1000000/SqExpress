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

        public static readonly DiagnosticDescriptor SqTSqlParserParseHasInvalidSql = new DiagnosticDescriptor(
            id: "SQEX010",
            title: "SqTSqlParser SQL cannot be parsed",
            messageFormat: "{0}",
            category: "Correctness",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Reports SqTSqlParser.Parse calls whose compile-time SQL text cannot be parsed by SqTSqlParser.");

        public static readonly DiagnosticDescriptor SqTSqlParserParseExistingTablesMismatch = new DiagnosticDescriptor(
            id: "SQEX011",
            title: "SqTSqlParser referenced SQL tables cannot be resolved to SqExpress table classes",
            messageFormat: "{0}",
            category: "Correctness",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Reports SqTSqlParser.Parse calls whose referenced SQL tables cannot be resolved uniquely to discovered SqExpress TableBase classes in the current compilation.");

        public static readonly DiagnosticDescriptor SqTSqlParserParseExistingColumnsMismatch = new DiagnosticDescriptor(
            id: "SQEX012",
            title: "SqTSqlParser referenced SQL columns cannot be resolved to SqExpress table members",
            messageFormat: "{0}",
            category: "Correctness",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Reports SqTSqlParser.Parse calls whose referenced SQL columns cannot be resolved to discovered SqExpress table descriptor members.");
    }
}
