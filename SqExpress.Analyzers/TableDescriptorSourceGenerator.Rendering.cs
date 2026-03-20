using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using SqExpress.Analyzers.Diagnostics;
using SqExpress.CodeGen.Shared;

namespace SqExpress.Analyzers
{
    public sealed partial class TableDescriptorSourceGenerator
    {
        private static void Execute(SourceProductionContext context, ImmutableArray<TableDescriptorCandidate?> candidates)
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
                context.AddSource(CodeGenTableDescriptorSupport.GetHintName(candidate.Model), syntaxRoot.GetText(System.Text.Encoding.UTF8));
            }
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
            => locations.Select(location => CreateDiagnostic(descriptor, location, args)).ToImmutableArray();
    }
}
