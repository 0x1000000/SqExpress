using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.DbMetadata;
using SqExpress.SqlParser;
using SqExpress.Syntax;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Update;

namespace SqExpress.Analyzers
{
    internal static class SqTSqlParserParseDiagnosticHelper
    {
        public static bool TryGetSqlParseFailureMessage(
            SqTSqlParserInvocation match,
            out string failureMessage)
        {
            failureMessage = string.Empty;
            if (!TryParseExpectedTables(match.SqlText, out var _, out var _, out var _, out failureMessage))
            {
                return true;
            }

            return false;
        }

        public static bool TryGetDiscoveredTablesFailureMessage(
            SemanticModel semanticModel,
            SqTSqlParserInvocation match,
            CancellationToken cancellationToken,
            out string failureMessage)
        {
            failureMessage = string.Empty;

            if (!TryParseExpectedTables(match.SqlText, out var _, out var _, out var expectedTables, out var _))
            {
                return false;
            }

            var expectedKeys = expectedTables
                .Select(i => i.TableKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (expectedKeys.Count < 1)
            {
                return false;
            }

            var sourceCatalog = SqTSqlParserSourceTableCatalogHelper.BuildSourceTableCatalog(semanticModel.Compilation, cancellationToken);
            var missing = new List<string>();
            var ambiguous = new List<string>();

            foreach (var expectedKey in expectedKeys)
            {
                if (!sourceCatalog.TryGetValue(expectedKey, out var candidates) || candidates.Count < 1)
                {
                    missing.Add(FormatTableKey(expectedKey));
                    continue;
                }

                if (candidates.Count > 1)
                {
                    ambiguous.Add(
                        FormatTableKey(expectedKey)
                        + ": "
                        + string.Join(", ", candidates.Select(i => i.SimpleTypeName).OrderBy(i => i, StringComparer.OrdinalIgnoreCase)));
                }
            }

            if (missing.Count < 1 && ambiguous.Count < 1)
            {
                return false;
            }

            var parts = new List<string>();
            if (missing.Count > 0)
            {
                parts.Add("No SqExpress table class found for: " + string.Join(", ", missing) + ".");
            }

            if (ambiguous.Count > 0)
            {
                parts.Add("Multiple SqExpress table classes found for: " + string.Join("; ", ambiguous) + ".");
            }

            failureMessage = string.Join(" ", parts);
            return true;
        }

        public static bool TryGetDiscoveredColumnsFailureMessage(
            SemanticModel semanticModel,
            SqTSqlParserInvocation match,
            CancellationToken cancellationToken,
            out string failureMessage)
        {
            failureMessage = string.Empty;

            if (!TryParseExpectedTables(match.SqlText, out var _, out var parsedTables, out var expectedTables, out var _))
            {
                return false;
            }

            var resolvableTables = expectedTables
                .GroupBy(i => i.TableKey, StringComparer.OrdinalIgnoreCase)
                .Where(i => i.Count() == 1)
                .ToDictionary(i => i.Key, i => i.First(), StringComparer.OrdinalIgnoreCase);
            if (resolvableTables.Count < 1)
            {
                return false;
            }

            var sourceCatalog = SqTSqlParserSourceTableCatalogHelper.BuildSourceTableCatalog(semanticModel.Compilation, cancellationToken);
            var resolvedTableClasses = new Dictionary<string, SourceTableInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var tableKey in resolvableTables.Keys)
            {
                if (!sourceCatalog.TryGetValue(tableKey, out var candidates) || candidates.Count != 1)
                {
                    return false;
                }

                resolvedTableClasses[tableKey] = candidates[0];
            }

            if (parsedTables == null || parsedTables.Count < 1)
            {
                return false;
            }

            var missingMessages = new List<string>();
            foreach (var parsedTable in parsedTables)
            {
                var tableKey = GetTableKey(parsedTable.FullName.AsExprTableFullName());
                if (!resolvedTableClasses.TryGetValue(tableKey, out var tableInfo)
                    || tableInfo.ColumnsByName.Count < 1
                    || parsedTable.Columns.Count < 1)
                {
                    continue;
                }

                foreach (var parsedColumn in parsedTable.Columns)
                {
                    if (tableInfo.ColumnsByName.ContainsKey(parsedColumn.ColumnName.Name))
                    {
                        continue;
                    }

                    var message = "Could not find Column [" + parsedColumn.ColumnName.Name + "] in table " + FormatTableKey(tableKey) + ".";
                    if (!missingMessages.Contains(message, StringComparer.Ordinal))
                    {
                        missingMessages.Add(message);
                    }
                }
            }

            if (missingMessages.Count < 1)
            {
                return false;
            }

            failureMessage = string.Join(" ", missingMessages.OrderBy(i => i, StringComparer.OrdinalIgnoreCase));
            return true;
        }

        private static bool TryParseExpectedTables(
            string sqlText,
            out IExpr? parsedExpr,
            out IReadOnlyList<SqTable>? parsedTables,
            out IReadOnlyList<ExpectedTableInfo> expectedTables,
            out string failureMessage)
        {
            parsedExpr = null;
            parsedTables = null;
            expectedTables = Array.Empty<ExpectedTableInfo>();
            failureMessage = string.Empty;

            if (!SqTSqlParser.TryParse(sqlText, out parsedExpr, out parsedTables, out string? parseError))
            {
                failureMessage = "Could not parse SQL. " + (parseError ?? "Unknown parser error.");
                return false;
            }

            expectedTables = CollectExpectedTables(parsedExpr!);
            return true;
        }

        private static IReadOnlyList<ExpectedTableInfo> CollectExpectedTables(IExpr expr)
        {
            var result = new List<ExpectedTableInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var table in expr.SyntaxTree().DescendantsAndSelf().OfType<ExprTable>())
            {
                var hasExplicitAlias = table.Alias != null;
                var alias = hasExplicitAlias
                    ? GetAliasName(table.Alias!.Alias)
                    : ToCamelCaseIdentifier(table.FullName.AsExprTableFullName().TableName.Name, "t");
                if (string.IsNullOrWhiteSpace(alias))
                {
                    continue;
                }

                var tableKey = GetTableKey(table.FullName.AsExprTableFullName());
                if (seen.Add(alias + "|" + tableKey))
                {
                    result.Add(new ExpectedTableInfo(alias, tableKey));
                }
            }

            switch (expr)
            {
                case ExprInsert insert:
                    AddSyntheticExpectedTable(result, seen, insert.Target);
                    break;
                case ExprIdentityInsert identityInsert:
                    AddSyntheticExpectedTable(result, seen, identityInsert.Insert.Target);
                    break;
                case ExprDelete delete:
                    AddSyntheticExpectedTable(result, seen, delete.Target.FullName);
                    break;
                case ExprDeleteOutput deleteOutput:
                    AddSyntheticExpectedTable(result, seen, deleteOutput.Delete.Target.FullName);
                    break;
                case ExprUpdate update:
                    AddSyntheticExpectedTable(result, seen, update.Target.FullName);
                    break;
            }

            return result;
        }

        private static void AddSyntheticExpectedTable(
            List<ExpectedTableInfo> result,
            HashSet<string> seen,
            IExprTableFullName fullName)
        {
            var exprFullName = fullName.AsExprTableFullName();
            var alias = ToCamelCaseIdentifier(exprFullName.TableName.Name, "t");
            if (string.IsNullOrWhiteSpace(alias))
            {
                return;
            }

            var tableKey = GetTableKey(exprFullName);
            if (seen.Add(alias + "|" + tableKey))
            {
                result.Add(new ExpectedTableInfo(alias, tableKey));
            }
        }

        private static SemanticModel GetSemanticModelForNode(SemanticModel semanticModel, SyntaxNode node)
        {
            if (node.SyntaxTree == semanticModel.SyntaxTree)
            {
                return semanticModel;
            }

            return semanticModel.Compilation.GetSemanticModel(node.SyntaxTree);
        }

        private static string GetAliasName(IExprAlias alias)
        {
            return alias switch
            {
                ExprAlias exprAlias => exprAlias.Name,
                ExprAliasGuid exprAliasGuid => "A" + Math.Abs(exprAliasGuid.Id.GetHashCode()).ToString(),
                _ => "A0"
            };
        }

        private static string GetTableKey(ExprTableFullName fullName)
            => BuildTableKey(fullName.DbSchema?.Schema.Name, fullName.TableName.Name);

        private static string BuildTableKey(string? schema, string tableName)
            => (schema ?? string.Empty) + "." + tableName;

        private static string FormatTableKey(string tableKey)
        {
            var parts = tableKey.Split(new[] { '.' }, 2);
            return parts.Length == 2
                ? "[" + parts[0] + "].[" + parts[1] + "]"
                : "[" + tableKey + "]";
        }

        private static string ToCamelCaseIdentifier(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            var parts = value
                .Split(new[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(i => i.Trim())
                .Where(i => i.Length > 0)
                .ToList();
            if (parts.Count < 1)
            {
                return fallback;
            }

            var first = parts[0];
            var result = char.ToLowerInvariant(first[0]) + first.Substring(1);
            for (var i = 1; i < parts.Count; i++)
            {
                result += char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            }

            return SyntaxFacts.IsValidIdentifier(result) ? result : fallback;
        }

        private readonly struct ExpectedTableInfo
        {
            public ExpectedTableInfo(string alias, string tableKey)
            {
                this.Alias = alias;
                this.TableKey = tableKey;
            }

            public string Alias { get; }

            public string TableKey { get; }
        }

    }
}
