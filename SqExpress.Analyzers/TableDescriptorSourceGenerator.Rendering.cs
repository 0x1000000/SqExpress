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
                    context.ReportDiagnostic(CreateDiagnostic(DiagnosticDescriptors.TableDescriptorDuplicateTable, candidate.Symbol, candidate.Model.TableDisplayName));
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
                    context.ReportDiagnostic(CreateValidationDiagnostic(issue, candidate.Symbol));
                }

                if (validation.Issues.Length > 0)
                {
                    continue;
                }

                var syntaxRoot = CodeGenTableDescriptorSupport.GenerateTableDescriptor(candidate.Model, validation, uniqueTableModels);
                context.AddSource(CodeGenTableDescriptorSupport.GetHintName(candidate.Model), syntaxRoot.GetText(System.Text.Encoding.UTF8));
            }
        }
    }
}
