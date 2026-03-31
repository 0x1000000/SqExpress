using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using SqExpress.Analyzers.Diagnostics;
using SqExpress.CodeGen.Shared;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.Analyzers
{
    public sealed partial class TableDescriptorSourceGenerator
    {
        private static void Execute(
            SourceProductionContext context,
            Compilation compilation,
            ImmutableArray<TableDescriptorCandidate?> candidates,
            GeneratorOptionValues rawOptions)
        {
            var materializedCandidates = candidates.Where(static c => c != null).Cast<TableDescriptorCandidate>().ToImmutableArray();
            if (materializedCandidates.IsDefaultOrEmpty)
            {
                return;
            }

            foreach (var candidate in materializedCandidates)
            {
                foreach (var diagnostic in candidate.Diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }

            var duplicatesByKey = materializedCandidates
                .GroupBy(static c => c.Model.TableKey, StringComparer.OrdinalIgnoreCase)
                .Where(static g => g.Count() > 1)
                .ToDictionary(static g => g.Key, static g => g.ToImmutableArray(), StringComparer.OrdinalIgnoreCase);

            foreach (var duplicate in duplicatesByKey.Values)
            {
                foreach (var candidate in duplicate)
                {
                    context.ReportDiagnostic(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorDuplicateTable, candidate.TableAttributeLocation, candidate.Model.TableDisplayName));
                }
            }

            var uniqueTables = materializedCandidates
                .Where(c => !duplicatesByKey.ContainsKey(c.Model.TableKey))
                .ToDictionary(static c => c.Model.TableKey, static c => c, StringComparer.OrdinalIgnoreCase);

            var uniqueTableModels = uniqueTables.ToDictionary(static p => p.Key, static p => p.Value.Model, StringComparer.OrdinalIgnoreCase);
            var validCandidates = ImmutableArray.CreateBuilder<TableDescriptorCandidate>();

            foreach (var candidate in materializedCandidates)
            {
                if (candidate.Diagnostics.Any() || duplicatesByKey.ContainsKey(candidate.Model.TableKey))
                {
                    continue;
                }

                var validation = CodeGenTableDescriptorSupport.Validate(candidate.Model, uniqueTableModels);
                foreach (var issue in validation.Issues)
                {
                    foreach (var diagnostic in CreateValidationDiagnostics(issue, candidate))
                    {
                        context.ReportDiagnostic(diagnostic);
                    }
                }

                if (validation.Issues.Length > 0)
                {
                    continue;
                }

                var syntaxRoot = CodeGenTableDescriptorSupport.GenerateTableDescriptor(candidate.Model, validation, uniqueTableModels);
                context.AddSource(CodeGenTableDescriptorSupport.GetHintName(candidate.Model), syntaxRoot.GetText(Encoding.UTF8));
                validCandidates.Add(candidate);
            }

            if (validCandidates.Count == 0)
            {
                return;
            }

            var options = NormalizeGeneratorOptions(rawOptions, compilation);
            var modelBuildResult = BuildSqModels(validCandidates.ToImmutable(), options);
            foreach (var diagnostic in modelBuildResult.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }

            foreach (var model in modelBuildResult.Models)
            {
                var syntaxRoot = CodeGenModelSupport.Generate(model.Meta, options.ModelNamespace, rwClasses: true, nullRefTypes: options.NullRefTypes, modelType: options.ModelType);
                context.AddSource(CodeGenModelSupport.GetHintName(options.ModelNamespace, model.Meta.Name), syntaxRoot.GetText(Encoding.UTF8));
            }
        }

        private static GeneratorOptionValues CreateGeneratorOptions(AnalyzerConfigOptionsProvider provider)
        {
            provider.GlobalOptions.TryGetValue("build_property.SqModelGenNamespace", out var modelNamespace);
            provider.GlobalOptions.TryGetValue("build_property.SqModelGenType", out var modelType);
            provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);

            return new GeneratorOptionValues(modelNamespace, modelType, rootNamespace);
        }

        private static GeneratorOptions NormalizeGeneratorOptions(GeneratorOptionValues rawOptions, Compilation compilation)
        {
            var rootNamespace = !string.IsNullOrWhiteSpace(rawOptions.RootNamespace)
                ? rawOptions.RootNamespace!
                : !string.IsNullOrWhiteSpace(compilation.AssemblyName)
                    ? compilation.AssemblyName!
                    : "SqExpress";

            var modelNamespace = !string.IsNullOrWhiteSpace(rawOptions.ModelNamespace)
                ? rawOptions.ModelNamespace!
                : rootNamespace + ".Models";

            var modelType = string.Equals(rawOptions.ModelType, nameof(CodeGenModelType.ImmutableClass), StringComparison.OrdinalIgnoreCase)
                ? CodeGenModelType.ImmutableClass
                : CodeGenModelType.Record;

            if (modelType == CodeGenModelType.Record && !SupportsRecords(compilation))
            {
                modelType = CodeGenModelType.ImmutableClass;
            }

            return new GeneratorOptions(
                modelNamespace,
                modelType,
                compilation.Options.NullableContextOptions != NullableContextOptions.Disable);
        }

        private static bool SupportsRecords(Compilation compilation)
        {
            var parseOptions = compilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions;
            return parseOptions == null || parseOptions.LanguageVersion >= LanguageVersion.CSharp9;
        }

        private static SqModelBuildResult BuildSqModels(
            ImmutableArray<TableDescriptorCandidate> validCandidates,
            GeneratorOptions options)
        {
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            var builders = new Dictionary<string, SqModelBuilder>(StringComparer.Ordinal);

            foreach (var candidate in validCandidates)
            {
                var tableLevelModelName = candidate.Model.SqModelName?.Trim();
                if (!string.IsNullOrWhiteSpace(tableLevelModelName) && !IsValidIdentifier(tableLevelModelName!))
                {
                    diagnostics.Add(CreateDiagnostic(
                        DiagnosticDescriptors.TableDescriptorInvalidSqModelName,
                        candidate.TableAttributeLocation,
                        tableLevelModelName!,
                        candidate.Symbol.Name));
                    tableLevelModelName = null;
                }

                var tableRef = new CodeGenSqModelTableRef(
                    candidate.Model.ClassName,
                    candidate.Model.Namespace ?? string.Empty,
                    candidate.Model.Kind == CodeGenTableKind.TempTable
                        ? BaseTypeKindTag.TempTableBase
                        : candidate.Model.Kind == CodeGenTableKind.DerivedTable
                            ? BaseTypeKindTag.DerivedTableBase
                            : BaseTypeKindTag.TableBase);

                foreach (var column in candidate.Model.Columns)
                {
                    var columnLocation = candidate.ColumnLocationsBySqlName.TryGetValue(column.SqlName, out var locations)
                        ? locations.FirstOrDefault() ?? candidate.TableAttributeLocation
                        : candidate.TableAttributeLocation;

                    var memberships = new Dictionary<string, ParsedSqModelsEntry>(StringComparer.Ordinal);
                    if (!string.IsNullOrWhiteSpace(tableLevelModelName))
                    {
                        memberships[tableLevelModelName!] = new ParsedSqModelsEntry(tableLevelModelName!, propertyNameOverride: null);
                    }

                    foreach (var parsedEntry in ParseSqModelEntries(candidate, column, columnLocation, diagnostics))
                    {
                        if (memberships.TryGetValue(parsedEntry.ModelName, out var existing) &&
                            !string.IsNullOrWhiteSpace(parsedEntry.PropertyNameOverride))
                        {
                            memberships[parsedEntry.ModelName] = new ParsedSqModelsEntry(parsedEntry.ModelName, parsedEntry.PropertyNameOverride);
                        }
                        else
                        {
                            if (!memberships.ContainsKey(parsedEntry.ModelName))
                            {
                                memberships.Add(parsedEntry.ModelName, parsedEntry);
                            }
                        }
                    }

                    if (memberships.Count == 0)
                    {
                        continue;
                    }

                    var tablePropertyName = ResolveTablePropertyName(column);
                    var clrType = CodeGenModelSupport.GetClrTypeName(column.Kind, options.NullRefTypes);

                    foreach (var membership in memberships.Values)
                    {
                        var modelPropertyName = membership.PropertyNameOverride ?? column.PropertyName ?? CodeGenTableDescriptorSupport.ToIdentifier(column.SqlName);
                        if (!IsValidIdentifier(modelPropertyName))
                        {
                            diagnostics.Add(CreateDiagnostic(
                                DiagnosticDescriptors.TableDescriptorInvalidSqModelPropertyName,
                                columnLocation,
                                modelPropertyName,
                                membership.ModelName,
                                column.SqlName,
                                candidate.Symbol.Name));
                            continue;
                        }

                        var builder = GetOrCreateSqModelBuilder(builders, membership.ModelName);
                        builder.Add(
                            candidate,
                            column,
                            columnLocation,
                            tableRef,
                            tablePropertyName,
                            modelPropertyName,
                            clrType,
                            column.SqModelCastTypeName,
                            diagnostics);
                    }
                }
            }

            var models = ImmutableArray.CreateBuilder<ModelEmissionCandidate>();
            foreach (var builder in builders.Values.OrderBy(static i => i.Meta.Name, StringComparer.Ordinal))
            {
                if (builder.IsInvalid)
                {
                    continue;
                }

                var distinctColumnCounts = builder.PropertyStates
                    .Select(static p => p.Property.Column.Count)
                    .Distinct()
                    .ToArray();

                if (distinctColumnCounts.Length > 1)
                {
                    foreach (var location in builder.AllLocations.DefaultIfEmpty(Location.None))
                    {
                        diagnostics.Add(CreateDiagnostic(
                            DiagnosticDescriptors.TableDescriptorInconsistentSqModelShape,
                            location,
                            builder.Meta.Name));
                    }

                    continue;
                }

                models.Add(new ModelEmissionCandidate(builder.Meta));
            }

            return new SqModelBuildResult(models.ToImmutable(), diagnostics.ToImmutable());
        }

        private static IEnumerable<ParsedSqModelsEntry> ParseSqModelEntries(
            TableDescriptorCandidate candidate,
            CodeGenColumnModel column,
            Location columnLocation,
            ImmutableArray<Diagnostic>.Builder diagnostics)
        {
            if (string.IsNullOrWhiteSpace(column.SqModels))
            {
                yield break;
            }

            foreach (var rawToken in column.SqModels!.Split(','))
            {
                var token = rawToken.Trim();
                if (token.Length == 0)
                {
                    diagnostics.Add(CreateDiagnostic(
                        DiagnosticDescriptors.TableDescriptorInvalidSqModelsEntry,
                        columnLocation,
                        rawToken,
                        column.SqlName,
                        candidate.Symbol.Name));
                    continue;
                }

                var firstDot = token.IndexOf('.');
                string modelName;
                string? propertyNameOverride;

                if (firstDot < 0)
                {
                    modelName = token;
                    propertyNameOverride = null;
                }
                else
                {
                    var lastDot = token.LastIndexOf('.');
                    if (firstDot != lastDot)
                    {
                        diagnostics.Add(CreateDiagnostic(
                            DiagnosticDescriptors.TableDescriptorInvalidSqModelsEntry,
                            columnLocation,
                            token,
                            column.SqlName,
                            candidate.Symbol.Name));
                        continue;
                    }

                    modelName = token.Substring(0, firstDot).Trim();
                    propertyNameOverride = token.Substring(firstDot + 1).Trim();
                    if (string.IsNullOrWhiteSpace(modelName) || string.IsNullOrWhiteSpace(propertyNameOverride))
                    {
                        diagnostics.Add(CreateDiagnostic(
                            DiagnosticDescriptors.TableDescriptorInvalidSqModelsEntry,
                            columnLocation,
                            token,
                            column.SqlName,
                            candidate.Symbol.Name));
                        continue;
                    }
                }

                if (!IsValidIdentifier(modelName))
                {
                    diagnostics.Add(CreateDiagnostic(
                        DiagnosticDescriptors.TableDescriptorInvalidSqModelsEntry,
                        columnLocation,
                        token,
                        column.SqlName,
                        candidate.Symbol.Name));
                    continue;
                }

                yield return new ParsedSqModelsEntry(modelName, propertyNameOverride);
            }
        }

        private static SqModelBuilder GetOrCreateSqModelBuilder(IDictionary<string, SqModelBuilder> builders, string modelName)
        {
            if (!builders.TryGetValue(modelName, out var builder))
            {
                builder = new SqModelBuilder(new CodeGenSqModelMeta(modelName));
                builders.Add(modelName, builder);
            }

            return builder;
        }

        private static string ResolveTablePropertyName(CodeGenColumnModel column)
        {
            return string.IsNullOrWhiteSpace(column.PropertyName)
                ? CodeGenTableDescriptorSupport.ToIdentifier(column.SqlName)
                : column.PropertyName!;
        }

        private static bool IsValidIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (!(char.IsLetter(value[0]) || value[0] == '_'))
            {
                return false;
            }

            for (var i = 1; i < value.Length; i++)
            {
                if (!(char.IsLetterOrDigit(value[i]) || value[i] == '_'))
                {
                    return false;
                }
            }

            return true;
        }

        private static ImmutableArray<Diagnostic> CreateValidationDiagnostics(CodeGenValidationIssue issue, TableDescriptorCandidate candidate)
        {
            switch (issue.Kind)
            {
                case CodeGenValidationIssueKind.DuplicateColumn:
                    return CreateDiagnosticsAtLocations(
                        candidate.ColumnLocationsBySqlName.TryGetValue(issue.Subject, out var duplicateColumnLocations) ? duplicateColumnLocations : ImmutableArray.Create(candidate.TableAttributeLocation),
                        DiagnosticDescriptors.TableDescriptorDuplicateColumn,
                        issue.Subject,
                        issue.TableDisplayName);
                case CodeGenValidationIssueKind.InvalidPropertyName:
                    return CreateDiagnosticsAtLocations(
                        issue.RelatedValue != null && candidate.ColumnLocationsBySqlName.TryGetValue(issue.RelatedValue, out var invalidPropertyLocations) ? invalidPropertyLocations : ImmutableArray.Create(candidate.TableAttributeLocation),
                        DiagnosticDescriptors.TableDescriptorHasInvalidPropertyName,
                        issue.Subject,
                        issue.RelatedValue ?? string.Empty,
                        candidate.Symbol.Name);
                case CodeGenValidationIssueKind.DuplicatePropertyName:
                    return CreateDiagnosticsAtLocations(
                        candidate.PropertyLocationsByName.TryGetValue(issue.Subject, out var duplicatePropertyLocations) ? duplicatePropertyLocations : ImmutableArray.Create(candidate.TableAttributeLocation),
                        DiagnosticDescriptors.TableDescriptorDuplicatePropertyName,
                        issue.Subject,
                        candidate.Symbol.Name);
                case CodeGenValidationIssueKind.UnknownIndexColumn:
                    return CreateDiagnosticsAtLocations(
                        candidate.IndexLocations.Where(i => i.Columns.Contains(issue.Subject)).Select(i => i.Location).DefaultIfEmpty(candidate.TableAttributeLocation).ToImmutableArray(),
                        DiagnosticDescriptors.TableDescriptorUnknownIndexColumn,
                        issue.Subject,
                        issue.TableDisplayName);
                case CodeGenValidationIssueKind.DescendingColumnMustBeIndexed:
                    return CreateDiagnosticsAtLocations(
                        candidate.IndexLocations.Where(i => i.DescendingColumns.Contains(issue.Subject)).Select(i => i.Location).DefaultIfEmpty(candidate.TableAttributeLocation).ToImmutableArray(),
                        DiagnosticDescriptors.TableDescriptorDescendingColumnMustBeIndexed,
                        issue.Subject,
                        issue.TableDisplayName);
                case CodeGenValidationIssueKind.ForeignKeyTableNotFound:
                    return CreateDiagnosticsAtLocations(
                        issue.RelatedValue != null && candidate.ColumnLocationsBySqlName.TryGetValue(issue.RelatedValue, out var foreignKeyTableLocations) ? foreignKeyTableLocations : ImmutableArray.Create(candidate.TableAttributeLocation),
                        DiagnosticDescriptors.TableDescriptorForeignKeyTableNotFound,
                        issue.Subject,
                        issue.TableDisplayName,
                        issue.RelatedValue ?? string.Empty);
                case CodeGenValidationIssueKind.ForeignKeyColumnNotFound:
                    return CreateDiagnosticsAtLocations(
                        issue.RelatedValue != null && candidate.ColumnLocationsBySqlName.TryGetValue(issue.RelatedValue, out var foreignKeyColumnLocations) ? foreignKeyColumnLocations : ImmutableArray.Create(candidate.TableAttributeLocation),
                        DiagnosticDescriptors.TableDescriptorForeignKeyColumnNotFound,
                        issue.Subject,
                        issue.TableDisplayName,
                        issue.RelatedValue ?? string.Empty);
                default:
                    throw new ArgumentOutOfRangeException(nameof(issue.Kind), issue.Kind, null);
            }
        }

        private static ImmutableArray<Diagnostic> CreateDiagnosticsAtLocations(
            ImmutableArray<Location> locations,
            DiagnosticDescriptor descriptor,
            params object[] args)
        {
            return locations.Select(location => CreateDiagnostic(descriptor, location, args)).ToImmutableArray();
        }

        private readonly struct GeneratorOptionValues
        {
            public GeneratorOptionValues(string? modelNamespace, string? modelType, string? rootNamespace)
            {
                this.ModelNamespace = modelNamespace;
                this.ModelType = modelType;
                this.RootNamespace = rootNamespace;
            }

            public string? ModelNamespace { get; }

            public string? ModelType { get; }

            public string? RootNamespace { get; }
        }

        private readonly struct GeneratorOptions
        {
            public GeneratorOptions(string modelNamespace, CodeGenModelType modelType, bool nullRefTypes)
            {
                this.ModelNamespace = modelNamespace;
                this.ModelType = modelType;
                this.NullRefTypes = nullRefTypes;
            }

            public string ModelNamespace { get; }

            public CodeGenModelType ModelType { get; }

            public bool NullRefTypes { get; }
        }

        private readonly struct ParsedSqModelsEntry
        {
            public ParsedSqModelsEntry(string modelName, string? propertyNameOverride)
            {
                this.ModelName = modelName;
                this.PropertyNameOverride = propertyNameOverride;
            }

            public string ModelName { get; }

            public string? PropertyNameOverride { get; }
        }

        private readonly struct ModelEmissionCandidate
        {
            public ModelEmissionCandidate(CodeGenSqModelMeta meta)
            {
                this.Meta = meta;
            }

            public CodeGenSqModelMeta Meta { get; }
        }

        private readonly struct SqModelBuildResult
        {
            public SqModelBuildResult(ImmutableArray<ModelEmissionCandidate> models, ImmutableArray<Diagnostic> diagnostics)
            {
                this.Models = models;
                this.Diagnostics = diagnostics;
            }

            public ImmutableArray<ModelEmissionCandidate> Models { get; }

            public ImmutableArray<Diagnostic> Diagnostics { get; }
        }

        private sealed class SqModelBuilder
        {
            private readonly Dictionary<string, SqModelPropertyState> _properties = new Dictionary<string, SqModelPropertyState>(StringComparer.Ordinal);
            private readonly List<Location> _allLocations = new List<Location>();

            public SqModelBuilder(CodeGenSqModelMeta meta)
            {
                this.Meta = meta;
            }

            public CodeGenSqModelMeta Meta { get; }

            public IEnumerable<SqModelPropertyState> PropertyStates => this._properties.Values;

            public IReadOnlyList<Location> AllLocations => this._allLocations;

            public bool IsInvalid { get; private set; }

            public void Add(
                TableDescriptorCandidate candidate,
                CodeGenColumnModel column,
                Location location,
                CodeGenSqModelTableRef tableRef,
                string tablePropertyName,
                string modelPropertyName,
                string clrType,
                string? castType,
                ImmutableArray<Diagnostic>.Builder diagnostics)
            {
                if (!this._properties.TryGetValue(modelPropertyName, out var state))
                {
                    var property = new CodeGenSqModelPropertyMeta(modelPropertyName, clrType, castType, column.IsPrimaryKey, column.IsIdentity);
                    property.AddColumnCheckExistence(this.Meta.Name, new CodeGenSqModelPropertyTableColMeta(tableRef, tablePropertyName));
                    this.Meta.AddPropertyCheckExistence(property);

                    state = new SqModelPropertyState(property);
                    this._properties.Add(modelPropertyName, state);
                    state.AddLocation(location);
                    this._allLocations.Add(location);
                    return;
                }

                if (state.Property.Type != clrType || state.Property.CastType != castType)
                {
                    this.IsInvalid = true;
                    foreach (var conflictLocation in state.Locations.Append(location))
                    {
                        diagnostics.Add(CreateDiagnostic(
                            DiagnosticDescriptors.TableDescriptorConflictingSqModelProperty,
                            conflictLocation,
                            modelPropertyName,
                            this.Meta.Name));
                    }

                    return;
                }

                var existingColumn = state.Property.Column.FirstOrDefault(c => c.TableRef.Equals(tableRef));
                if (existingColumn != null)
                {
                    if (string.Equals(existingColumn.ColumnName, tablePropertyName, StringComparison.Ordinal))
                    {
                        return;
                    }

                    this.IsInvalid = true;
                    foreach (var duplicateLocation in state.Locations.Append(location))
                    {
                        diagnostics.Add(CreateDiagnostic(
                            DiagnosticDescriptors.TableDescriptorDuplicateSqModelPropertyInTable,
                            duplicateLocation,
                            modelPropertyName,
                            this.Meta.Name,
                            candidate.Symbol.Name));
                    }

                    return;
                }

                state.Property.AddColumnCheckExistence(this.Meta.Name, new CodeGenSqModelPropertyTableColMeta(tableRef, tablePropertyName));
                state.AddLocation(location);
                this._allLocations.Add(location);
            }
        }

        private sealed class SqModelPropertyState
        {
            private readonly List<Location> _locations = new List<Location>();

            public SqModelPropertyState(CodeGenSqModelPropertyMeta property)
            {
                this.Property = property;
            }

            public CodeGenSqModelPropertyMeta Property { get; }

            public IReadOnlyList<Location> Locations => this._locations;

            public void AddLocation(Location location)
            {
                this._locations.Add(location);
            }
        }
    }
}
