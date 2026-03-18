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

        public static readonly DiagnosticDescriptor TableDescriptorMustBeClass = new DiagnosticDescriptor(
            id: "SQEX100",
            title: "Table descriptor target must be a class",
            messageFormat: "Table descriptor target '{0}' must be a class",
            category: "SourceGeneration",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Reports unsupported table descriptor attribute targets.");

        public static readonly DiagnosticDescriptor TableDescriptorMustBePartial = new DiagnosticDescriptor(
            id: "SQEX101",
            title: "Table descriptor class must be partial",
            messageFormat: "Table descriptor class '{0}' must be declared partial",
            category: "SourceGeneration",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Reports table descriptor classes that cannot be completed by the source generator.");

        public static readonly DiagnosticDescriptor TableDescriptorMustBeTopLevel = new DiagnosticDescriptor(
            id: "SQEX102",
            title: "Table descriptor class must be top-level",
            messageFormat: "Table descriptor class '{0}' must be top-level",
            category: "SourceGeneration",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Reports nested table descriptor classes, which are not supported by the generator.");

        public static readonly DiagnosticDescriptor TableDescriptorMustBeNonGeneric = new DiagnosticDescriptor(
            id: "SQEX103",
            title: "Table descriptor class must be non-generic",
            messageFormat: "Table descriptor class '{0}' must be non-generic",
            category: "SourceGeneration",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Reports generic table descriptor classes, which are not supported by the generator.");

        public static readonly DiagnosticDescriptor TableDescriptorMustNotSpecifyBaseType = new DiagnosticDescriptor(
            id: "SQEX104",
            title: "Table descriptor class must not specify a custom base type",
            messageFormat: "Table descriptor class '{0}' must not specify a custom base type",
            category: "SourceGeneration",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Reports table descriptor classes whose base type would conflict with generated TableBase inheritance.");

        public static readonly DiagnosticDescriptor TableDescriptorHasInvalidDeclaration = new DiagnosticDescriptor(
            id: "SQEX105",
            title: "Table descriptor declaration is invalid",
            messageFormat: "Table descriptor declaration for '{0}' is invalid",
            category: "SourceGeneration",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Reports table descriptor declarations with unsupported constructor arguments.");

        public static readonly DiagnosticDescriptor TableDescriptorDuplicateTable = new DiagnosticDescriptor(
            id: "SQEX106",
            title: "Table descriptor table mapping is duplicated",
            messageFormat: "Table descriptor for table {0} is declared more than once",
            category: "SourceGeneration",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Reports multiple generated descriptor classes that target the same SQL table.");

        public static readonly DiagnosticDescriptor TableDescriptorDuplicateColumn = new DiagnosticDescriptor(
            id: "SQEX107",
            title: "Table descriptor column mapping is duplicated",
            messageFormat: "Column '{0}' is declared more than once for table {1}",
            category: "SourceGeneration",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Reports duplicate SQL column declarations on a generated table descriptor.");

        public static readonly DiagnosticDescriptor TableDescriptorHasInvalidPropertyName = new DiagnosticDescriptor(
            id: "SQEX108",
            title: "Table descriptor property name is invalid",
            messageFormat: "Property name '{0}' for SQL column '{1}' in table descriptor '{2}' is not a valid C# identifier",
            category: "SourceGeneration",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Reports invalid generated or explicit property names for generated table descriptors.");

        public static readonly DiagnosticDescriptor TableDescriptorDuplicatePropertyName = new DiagnosticDescriptor(
            id: "SQEX109",
            title: "Table descriptor property mapping is duplicated",
            messageFormat: "Property name '{0}' is generated more than once for table descriptor '{1}'",
            category: "SourceGeneration",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Reports duplicate C# property names after column-name normalization.");

        public static readonly DiagnosticDescriptor TableDescriptorUnknownIndexColumn = new DiagnosticDescriptor(
            id: "SQEX110",
            title: "Index references an unknown table descriptor column",
            messageFormat: "Could not find index column '{0}' in table {1}",
            category: "SourceGeneration",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Reports index definitions that reference unknown SQL columns.");

        public static readonly DiagnosticDescriptor TableDescriptorDescendingColumnMustBeIndexed = new DiagnosticDescriptor(
            id: "SQEX111",
            title: "Descending index column must belong to the index",
            messageFormat: "Descending index column '{0}' is not part of the index for table {1}",
            category: "SourceGeneration",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Reports descending index metadata that references a column not present in the index column list.");

        public static readonly DiagnosticDescriptor TableDescriptorForeignKeyTableNotFound = new DiagnosticDescriptor(
            id: "SQEX112",
            title: "Foreign key table could not be resolved",
            messageFormat: "Could not resolve foreign key target table '{0}' for column '{2}' in table {1}",
            category: "SourceGeneration",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Reports foreign key attributes that point to an unknown generated table descriptor.");

        public static readonly DiagnosticDescriptor TableDescriptorForeignKeyColumnNotFound = new DiagnosticDescriptor(
            id: "SQEX113",
            title: "Foreign key column could not be resolved",
            messageFormat: "Could not resolve foreign key target column '{0}' in table {1} for column '{2}'",
            category: "SourceGeneration",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Reports foreign key attributes that point to an unknown target column.");
    }
}
