using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SqExpress.DbMetadata;
using SqExpress.SqlParser.Internal.Dom;
using SqExpress.SqlParser.Internal.Parsing;

namespace SqExpress.SqlParser.Internal.Mapping
{
    internal static class SqlDomTableArtifactExtractor
    {
        public static IReadOnlyList<SqTable> ExtractTables(SqlDomStatement statement)
        {
            var cteNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (statement.WithClause != null)
            {
                for (var i = 0; i < statement.WithClause.Ctes.Count; i++)
                {
                    cteNames.Add(statement.WithClause.Ctes[i].Name);
                }
            }

            var aliasToTable = new Dictionary<string, TableIdentity>(StringComparer.OrdinalIgnoreCase);
            var byTable = new Dictionary<TableIdentity, TableColumnMap>(TableIdentityComparer.Instance);

            for (var i = 0; i < statement.TableReferences.Count; i++)
            {
                var tableRef = statement.TableReferences[i];
                if (cteNames.Contains(tableRef.Table))
                {
                    continue;
                }

                var schema = string.IsNullOrWhiteSpace(tableRef.Schema) ? "dbo" : tableRef.Schema!;
                var tableIdentity = new TableIdentity(schema, tableRef.Table);
                if (!byTable.TryGetValue(tableIdentity, out _))
                {
                    byTable[tableIdentity] = new TableColumnMap();
                }

                var aliasKey = string.IsNullOrWhiteSpace(tableRef.Alias) ? tableRef.Table : tableRef.Alias!;
                aliasToTable[aliasKey] = tableIdentity;
            }

            for (var i = 0; i < statement.ColumnReferences.Count; i++)
            {
                var columnRef = statement.ColumnReferences[i];
                if (aliasToTable.TryGetValue(columnRef.SourceAlias, out var tableIdentity))
                {
                    RegisterColumnHint(byTable[tableIdentity], columnRef.ColumnName, InferredColumnKind.Int32);
                }
            }

            ApplyTokenBasedHints(statement.RawSql, aliasToTable, byTable);
            return BuildSqTables(byTable);
        }

        private static void ApplyTokenBasedHints(
            string rawSql,
            IReadOnlyDictionary<string, TableIdentity> aliasToTable,
            IDictionary<TableIdentity, TableColumnMap> byTable)
        {
            var tokens = SqlLexer.Tokenize(rawSql);
            var meaningful = tokens.Where(i => i.Type != SqlTokenType.EndOfFile).ToList();
            if (meaningful.Count < 1)
            {
                return;
            }

            var singleTableIdentity = aliasToTable.Count == 1 ? aliasToTable.First().Value : null;

            for (var i = 0; i < meaningful.Count; i++)
            {
                if (!TryReadColumnReference(meaningful, i, aliasToTable, singleTableIdentity, out var identity, out var column, out var next, out var isQualified))
                {
                    continue;
                }

                if (!byTable.TryGetValue(identity!, out var map))
                {
                    continue;
                }

                if (next >= meaningful.Count)
                {
                    continue;
                }

                var token = meaningful[next];
                if (isQualified || IsLikelySelectProjection(meaningful, i, next))
                {
                    RegisterColumnHint(map, column!, InferredColumnKind.Int32);
                }

                if (token.IsKeyword("LIKE"))
                {
                    RegisterColumnHint(map, column!, InferredColumnKind.NVarChar, stringLength: 255);
                    continue;
                }

                if (token.IsKeyword("IS"))
                {
                    var j = next + 1;
                    if (j < meaningful.Count && meaningful[j].IsKeyword("NOT"))
                    {
                        j++;
                    }

                    if (j < meaningful.Count && meaningful[j].IsKeyword("NULL"))
                    {
                        RegisterColumnHint(map, column!, InferredColumnKind.Int32, nullable: true);
                    }

                    continue;
                }

                if (token.IsKeyword("IN") && next + 1 < meaningful.Count && meaningful[next + 1].Type == SqlTokenType.OpenParen)
                {
                    var close = FindMatchingCloseParen(meaningful, next + 1);
                    if (close > next + 1)
                    {
                        InferFromInList(meaningful, next + 2, close, map, column!);
                    }

                    continue;
                }

                if (token.Type != SqlTokenType.Operator || next + 1 >= meaningful.Count)
                {
                    continue;
                }

                RegisterColumnHint(map, column!, InferredColumnKind.Int32);
                InferFromRightOperand(meaningful[next + 1], map, column!);
            }
        }

        private static bool IsLikelySelectProjection(IReadOnlyList<SqlToken> tokens, int currentIndex, int nextTokenIndex)
        {
            if (nextTokenIndex >= tokens.Count)
            {
                return false;
            }

            var next = tokens[nextTokenIndex];
            if (next.Type != SqlTokenType.Comma && !next.IsKeyword("FROM"))
            {
                return false;
            }

            if (currentIndex < 1)
            {
                return false;
            }

            var previous = tokens[currentIndex - 1];
            if (previous.IsKeyword("AS"))
            {
                return false;
            }

            return previous.IsKeyword("SELECT") || previous.Type == SqlTokenType.Comma;
        }

        private static void InferFromInList(
            IReadOnlyList<SqlToken> tokens,
            int start,
            int end,
            TableColumnMap map,
            string column)
        {
            var hasString = false;
            var hasDecimal = false;
            var hasInteger = false;

            for (var i = start; i < end; i++)
            {
                var token = tokens[i];
                if (token.Type == SqlTokenType.StringLiteral)
                {
                    hasString = true;
                    continue;
                }

                if (token.Type == SqlTokenType.NumberLiteral)
                {
                    if (token.Text.IndexOf(".", StringComparison.Ordinal) >= 0)
                    {
                        hasDecimal = true;
                    }
                    else
                    {
                        hasInteger = true;
                    }
                }
            }

            if (hasString)
            {
                RegisterColumnHint(map, column, InferredColumnKind.NVarChar, stringLength: 255);
                return;
            }

            if (hasDecimal)
            {
                RegisterColumnHint(map, column, InferredColumnKind.Decimal);
                return;
            }

            if (hasInteger)
            {
                RegisterColumnHint(map, column, InferredColumnKind.Int32);
            }
        }

        private static void InferFromRightOperand(SqlToken rhs, TableColumnMap map, string column)
        {
            if (rhs.Type == SqlTokenType.StringLiteral)
            {
                var value = UnescapeSqlString(rhs.Text);
                if (TryParseDateTimeLiteral(value))
                {
                    RegisterColumnHint(map, column, InferredColumnKind.DateTime);
                    return;
                }

                if (TryParseDateTimeOffsetLiteral(value))
                {
                    RegisterColumnHint(map, column, InferredColumnKind.DateTimeOffset);
                    return;
                }

                if (TryParseGuidLiteral(value))
                {
                    RegisterColumnHint(map, column, InferredColumnKind.Guid);
                    return;
                }

                if (LooksLikeDateTimeColumnName(column))
                {
                    RegisterColumnHint(map, column, InferredColumnKind.DateTime);
                    return;
                }

                RegisterColumnHint(map, column, InferredColumnKind.NVarChar, stringLength: Math.Max(255, value.Length));
                return;
            }

            if (rhs.Type == SqlTokenType.NumberLiteral)
            {
                if (rhs.Text.IndexOf(".", StringComparison.Ordinal) >= 0)
                {
                    RegisterColumnHint(map, column, InferredColumnKind.Decimal);
                }
                else
                {
                    RegisterColumnHint(map, column, InferredColumnKind.Int32);
                }
            }
        }

        private static bool LooksLikeDateTimeColumnName(string columnName)
        {
            var normalized = new string(
                columnName
                    .Where(ch => ch != '_' && ch != '-' && !char.IsWhiteSpace(ch))
                    .ToArray())
                .ToUpperInvariant();

            return normalized.EndsWith("DATE", StringComparison.Ordinal)
                   || normalized.EndsWith("TIME", StringComparison.Ordinal)
                   || normalized.EndsWith("AT", StringComparison.Ordinal)
                   || normalized.IndexOf("UTC", StringComparison.Ordinal) >= 0
                   || normalized.IndexOf("TIMESTAMP", StringComparison.Ordinal) >= 0
                   || normalized.EndsWith("ON", StringComparison.Ordinal);
        }

        private static bool TryReadColumnReference(
            IReadOnlyList<SqlToken> tokens,
            int index,
            IReadOnlyDictionary<string, TableIdentity> aliasToTable,
            TableIdentity? singleTableIdentity,
            out TableIdentity? identity,
            out string? column,
            out int nextTokenIndex,
            out bool isQualified)
        {
            identity = null;
            column = null;
            nextTokenIndex = index;
            isQualified = false;

            if (index >= tokens.Count || !tokens[index].IsIdentifierLike || IsReservedWord(tokens[index]))
            {
                return false;
            }

            if (index + 2 < tokens.Count
                && tokens[index + 1].Type == SqlTokenType.Dot
                && tokens[index + 2].IsIdentifierLike
                && !IsReservedWord(tokens[index + 2]))
            {
                if (!aliasToTable.TryGetValue(tokens[index].IdentifierValue, out identity))
                {
                    return false;
                }

                isQualified = true;
                column = tokens[index + 2].IdentifierValue;
                nextTokenIndex = index + 3;
                return true;
            }

            if (singleTableIdentity == null)
            {
                return false;
            }

            var identifier = tokens[index].IdentifierValue;
            if (identifier.StartsWith("@", StringComparison.Ordinal))
            {
                return false;
            }

            if (aliasToTable.ContainsKey(identifier))
            {
                return false;
            }

            identity = singleTableIdentity;
            column = identifier;
            nextTokenIndex = index + 1;
            return true;
        }

        private static int FindMatchingCloseParen(IReadOnlyList<SqlToken> tokens, int openParenIndex)
        {
            var depth = 0;
            for (var i = openParenIndex; i < tokens.Count; i++)
            {
                if (tokens[i].Type == SqlTokenType.OpenParen)
                {
                    depth++;
                    continue;
                }

                if (tokens[i].Type == SqlTokenType.CloseParen)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private static string UnescapeSqlString(string tokenText)
        {
            if (tokenText.Length >= 2 && tokenText[0] == '\'' && tokenText[tokenText.Length - 1] == '\'')
            {
                return tokenText.Substring(1, tokenText.Length - 2).Replace("''", "'");
            }

            return tokenText;
        }

        private static bool TryParseGuidLiteral(string value)
            => Guid.TryParse(value, out _);

        private static bool TryParseDateTimeLiteral(string value)
            => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out _);

        private static bool TryParseDateTimeOffsetLiteral(string value)
            => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out _);

        private static void RegisterColumnHint(
            TableColumnMap tableColumns,
            string columnName,
            InferredColumnKind hint,
            bool nullable = false,
            int? stringLength = null)
        {
            var normalizedHint = ApplyNamingHeuristics(columnName, hint, out var namingLength);
            var mergedStringLength = MergeStringLength(stringLength, namingLength);

            if (tableColumns.Columns.TryGetValue(columnName, out var existing))
            {
                existing.Kind = MergeColumnKinds(existing.Kind, normalizedHint);
                existing.IsNullable = existing.IsNullable || nullable;
                if (existing.Kind == InferredColumnKind.NVarChar)
                {
                    existing.StringLength = MergeStringLength(existing.StringLength, mergedStringLength);
                }
                else
                {
                    existing.StringLength = null;
                }

                return;
            }

            tableColumns.Columns[columnName] = new InferredTableColumn(
                columnName,
                normalizedHint,
                nullable,
                normalizedHint == InferredColumnKind.NVarChar ? mergedStringLength : null,
                tableColumns.NextOrder++);
        }

        private static IReadOnlyList<SqTable> BuildSqTables(IReadOnlyDictionary<TableIdentity, TableColumnMap> tableColumns)
        {
            var result = new List<SqTable>(tableColumns.Count);
            foreach (var entry in tableColumns.OrderBy(i => i.Key.SchemaName, StringComparer.OrdinalIgnoreCase).ThenBy(i => i.Key.TableName, StringComparer.OrdinalIgnoreCase))
            {
                var columns = entry.Value.Columns.Values.OrderBy(i => i.Order).ToList();
                var table = SqTable.Create(
                    entry.Key.SchemaName,
                    entry.Key.TableName,
                    app =>
                    {
                        for (var i = 0; i < columns.Count; i++)
                        {
                            var column = columns[i];
                            switch (column.Kind)
                            {
                                case InferredColumnKind.NVarChar:
                                    if (column.IsNullable)
                                    {
                                        app.AppendNullableStringColumn(column.Name, column.StringLength ?? 255, isUnicode: true);
                                    }
                                    else
                                    {
                                        app.AppendStringColumn(column.Name, column.StringLength ?? 255, isUnicode: true);
                                    }

                                    break;
                                case InferredColumnKind.Boolean:
                                    if (column.IsNullable)
                                    {
                                        app.AppendNullableBooleanColumn(column.Name);
                                    }
                                    else
                                    {
                                        app.AppendBooleanColumn(column.Name);
                                    }

                                    break;
                                case InferredColumnKind.Decimal:
                                    if (column.IsNullable)
                                    {
                                        app.AppendNullableDecimalColumn(column.Name);
                                    }
                                    else
                                    {
                                        app.AppendDecimalColumn(column.Name);
                                    }

                                    break;
                                case InferredColumnKind.DateTime:
                                    if (column.IsNullable)
                                    {
                                        app.AppendNullableDateTimeColumn(column.Name);
                                    }
                                    else
                                    {
                                        app.AppendDateTimeColumn(column.Name);
                                    }

                                    break;
                                case InferredColumnKind.DateTimeOffset:
                                    if (column.IsNullable)
                                    {
                                        app.AppendNullableDateTimeOffsetColumn(column.Name);
                                    }
                                    else
                                    {
                                        app.AppendDateTimeOffsetColumn(column.Name);
                                    }

                                    break;
                                case InferredColumnKind.Guid:
                                    if (column.IsNullable)
                                    {
                                        app.AppendNullableGuidColumn(column.Name);
                                    }
                                    else
                                    {
                                        app.AppendGuidColumn(column.Name);
                                    }

                                    break;
                                case InferredColumnKind.ByteArray:
                                    if (column.IsNullable)
                                    {
                                        app.AppendNullableByteArrayColumn(column.Name, size: null);
                                    }
                                    else
                                    {
                                        app.AppendByteArrayColumn(column.Name, size: null);
                                    }

                                    break;
                                default:
                                    if (column.IsNullable)
                                    {
                                        app.AppendNullableInt32Column(column.Name);
                                    }
                                    else
                                    {
                                        app.AppendInt32Column(column.Name);
                                    }

                                    break;
                            }
                        }

                        return app;
                    });
                result.Add(table);
            }

            return result;
        }

        private static InferredColumnKind ApplyNamingHeuristics(string sqlName, InferredColumnKind hint, out int? stringLength)
        {
            stringLength = null;
            if (hint != InferredColumnKind.Int32)
            {
                return hint;
            }

            var normalized = new string(
                sqlName
                    .Where(ch => ch != '_' && ch != '-' && !char.IsWhiteSpace(ch))
                    .ToArray())
                .ToUpperInvariant();

            if (normalized.StartsWith("IS", StringComparison.Ordinal)
                || normalized.StartsWith("HAS", StringComparison.Ordinal)
                || normalized.StartsWith("CAN", StringComparison.Ordinal)
                || normalized.EndsWith("FLAG", StringComparison.Ordinal)
                || normalized.StartsWith("ENABLE", StringComparison.Ordinal)
                || normalized.StartsWith("DISABLE", StringComparison.Ordinal))
            {
                return InferredColumnKind.Boolean;
            }

            if (normalized.EndsWith("ADDRESS", StringComparison.Ordinal))
            {
                stringLength = 1000;
                return InferredColumnKind.NVarChar;
            }

            if (normalized.EndsWith("EMAIL", StringComparison.Ordinal)
                || normalized.EndsWith("NAME", StringComparison.Ordinal)
                || normalized.EndsWith("DESCRIPTION", StringComparison.Ordinal)
                || normalized.EndsWith("TITLE", StringComparison.Ordinal)
                || normalized.EndsWith("COMMENT", StringComparison.Ordinal)
                || normalized.EndsWith("NOTE", StringComparison.Ordinal)
                || normalized.EndsWith("TEXT", StringComparison.Ordinal))
            {
                stringLength = 255;
                return InferredColumnKind.NVarChar;
            }

            if (normalized.EndsWith("DATE", StringComparison.Ordinal)
                || normalized.EndsWith("TIME", StringComparison.Ordinal)
                || normalized.EndsWith("AT", StringComparison.Ordinal)
                || normalized.IndexOf("UTC", StringComparison.Ordinal) >= 0
                || normalized.IndexOf("TIMESTAMP", StringComparison.Ordinal) >= 0
                || normalized.EndsWith("ON", StringComparison.Ordinal))
            {
                return InferredColumnKind.DateTime;
            }

            if (normalized.EndsWith("GUID", StringComparison.Ordinal)
                || normalized.EndsWith("UUID", StringComparison.Ordinal)
                || normalized.EndsWith("UID", StringComparison.Ordinal))
            {
                return InferredColumnKind.Guid;
            }

            if (normalized.EndsWith("AMOUNT", StringComparison.Ordinal)
                || normalized.EndsWith("PRICE", StringComparison.Ordinal)
                || normalized.EndsWith("COST", StringComparison.Ordinal)
                || normalized.EndsWith("RATE", StringComparison.Ordinal)
                || normalized.EndsWith("PERCENT", StringComparison.Ordinal)
                || normalized.EndsWith("BALANCE", StringComparison.Ordinal))
            {
                return InferredColumnKind.Decimal;
            }

            return InferredColumnKind.Int32;
        }

        private static int? MergeStringLength(int? existing, int? hint)
        {
            if (existing.HasValue && hint.HasValue)
            {
                return Math.Max(existing.Value, hint.Value);
            }

            return existing ?? hint;
        }

        private static InferredColumnKind MergeColumnKinds(InferredColumnKind existing, InferredColumnKind hint)
        {
            if (existing == hint)
            {
                return existing;
            }

            if (existing == InferredColumnKind.Int32)
            {
                return hint;
            }

            if (hint == InferredColumnKind.Int32)
            {
                return existing;
            }

            return InferredColumnKind.NVarChar;
        }

        private static bool IsReservedWord(SqlToken token)
        {
            if (token.Type != SqlTokenType.Identifier)
            {
                return false;
            }

            switch (token.Text.ToUpperInvariant())
            {
                case "SELECT":
                case "FROM":
                case "JOIN":
                case "UPDATE":
                case "DELETE":
                case "INSERT":
                case "MERGE":
                case "WHERE":
                case "SET":
                case "VALUES":
                case "INTO":
                case "USING":
                case "ON":
                case "GROUP":
                case "BY":
                case "ORDER":
                case "OFFSET":
                case "FETCH":
                case "ROW":
                case "ROWS":
                case "UNION":
                case "INTERSECT":
                case "EXCEPT":
                case "OUTER":
                case "CROSS":
                case "APPLY":
                case "WHEN":
                case "THEN":
                case "ELSE":
                case "END":
                case "AS":
                case "WITH":
                case "AND":
                case "OR":
                case "LIKE":
                case "IN":
                case "NULL":
                case "IS":
                case "NOT":
                    return true;
                default:
                    return false;
            }
        }

        private sealed class TableIdentity
        {
            public TableIdentity(string schemaName, string tableName)
            {
                this.SchemaName = schemaName;
                this.TableName = tableName;
            }

            public string SchemaName { get; }

            public string TableName { get; }
        }

        private sealed class TableIdentityComparer : IEqualityComparer<TableIdentity>
        {
            public static readonly TableIdentityComparer Instance = new TableIdentityComparer();

            public bool Equals(TableIdentity? x, TableIdentity? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                return string.Equals(x.SchemaName, y.SchemaName, StringComparison.OrdinalIgnoreCase)
                       && string.Equals(x.TableName, y.TableName, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(TableIdentity obj)
            {
                unchecked
                {
                    var hashCode = 17;
                    hashCode = (hashCode * 31) + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.SchemaName);
                    hashCode = (hashCode * 31) + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.TableName);
                    return hashCode;
                }
            }
        }

        private sealed class TableColumnMap
        {
            public Dictionary<string, InferredTableColumn> Columns { get; } = new Dictionary<string, InferredTableColumn>(StringComparer.OrdinalIgnoreCase);

            public int NextOrder { get; set; }
        }

        private sealed class InferredTableColumn
        {
            public InferredTableColumn(string name, InferredColumnKind kind, bool isNullable, int? stringLength, int order)
            {
                this.Name = name;
                this.Kind = kind;
                this.IsNullable = isNullable;
                this.StringLength = stringLength;
                this.Order = order;
            }

            public string Name { get; }

            public InferredColumnKind Kind { get; set; }

            public bool IsNullable { get; set; }

            public int? StringLength { get; set; }

            public int Order { get; }
        }

        private enum InferredColumnKind
        {
            Int32 = 1,
            NVarChar,
            Boolean,
            Decimal,
            DateTime,
            DateTimeOffset,
            Guid,
            ByteArray
        }
    }
}
