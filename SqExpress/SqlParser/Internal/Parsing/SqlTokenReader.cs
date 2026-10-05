using System;
using System.Collections.Generic;

namespace SqExpress.SqlParser.Internal.Parsing;

internal static class SqlTokenReader
{
    public readonly struct TokenRange
    {
        public TokenRange(int start, int end)
        {
            this.Start = start;
            this.End = end;
        }

        public int Start { get; }

        public int End { get; }
    }

    public static int FindMatchingCloseParen(IReadOnlyList<SqlToken> tokens, int openIndex)
    {
        if (openIndex < 0 || openIndex >= tokens.Count || tokens[openIndex].Type != SqlTokenType.OpenParen)
        {
            return -1;
        }

        var depth = 0;
        for (var i = openIndex; i < tokens.Count; i++)
        {
            switch (tokens[i].Type)
            {
                case SqlTokenType.OpenParen:
                    depth++;
                    break;
                case SqlTokenType.CloseParen:
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                    break;
                case SqlTokenType.EndOfFile:
                    return -1;
            }
        }

        return -1;
    }

    public static int FindFirstTopLevelKeyword(
        IReadOnlyList<SqlToken> tokens,
        int startIndex,
        int endExclusive,
        string keyword)
    {
        if (startIndex < 0 || startIndex > tokens.Count || endExclusive < startIndex || endExclusive > tokens.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex), "The token range is outside the token list.");
        }

        var depth = 0;
        for (var i = startIndex; i < endExclusive; i++)
        {
            switch (tokens[i].Type)
            {
                case SqlTokenType.OpenParen:
                    depth++;
                    continue;
                case SqlTokenType.CloseParen:
                    if (depth > 0)
                    {
                        depth--;
                    }
                    continue;
                case SqlTokenType.EndOfFile:
                    return -1;
            }

            if (depth == 0 && tokens[i].IsKeyword(keyword))
            {
                return i;
            }
        }

        return -1;
    }

    public static List<string> ReadMultipartIdentifier(
        IReadOnlyList<SqlToken> tokens,
        ref int index,
        int endExclusive)
    {
        if (index < 0 || index > tokens.Count || endExclusive < index || endExclusive > tokens.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "The token range is outside the token list.");
        }

        var result = new List<string>();
        if (index >= endExclusive || !tokens[index].IsIdentifierLike)
        {
            return result;
        }

        result.Add(tokens[index].IdentifierValue);
        index++;

        while (index + 1 < endExclusive
               && tokens[index].Type == SqlTokenType.Dot
               && tokens[index + 1].IsIdentifierLike)
        {
            index++;
            result.Add(tokens[index].IdentifierValue);
            index++;
        }

        return result;
    }

    public static bool TrySplitTopLevelCommaRanges(
        IReadOnlyList<SqlToken> tokens,
        int startInclusive,
        int endExclusive,
        out IReadOnlyList<TokenRange> ranges)
    {
        if (startInclusive < 0 || startInclusive > tokens.Count || endExclusive < startInclusive || endExclusive > tokens.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(startInclusive), "The token range is outside the token list.");
        }

        var result = new List<TokenRange>();
        var segmentStart = startInclusive;
        var depth = 0;
        for (var i = startInclusive; i < endExclusive; i++)
        {
            switch (tokens[i].Type)
            {
                case SqlTokenType.OpenParen:
                    depth++;
                    break;
                case SqlTokenType.CloseParen:
                    if (depth == 0)
                    {
                        ranges = Array.Empty<TokenRange>();
                        return false;
                    }
                    depth--;
                    break;
                case SqlTokenType.Comma when depth == 0:
                    if (i == segmentStart)
                    {
                        ranges = Array.Empty<TokenRange>();
                        return false;
                    }
                    result.Add(new TokenRange(segmentStart, i));
                    segmentStart = i + 1;
                    break;
                case SqlTokenType.EndOfFile:
                    endExclusive = i;
                    i = endExclusive;
                    break;
            }
        }

        if (depth != 0 || segmentStart >= endExclusive)
        {
            ranges = Array.Empty<TokenRange>();
            return false;
        }

        result.Add(new TokenRange(segmentStart, endExclusive));
        ranges = result;
        return true;
    }

    public static string SliceSql(
        string sql,
        IReadOnlyList<SqlToken> tokens,
        int startInclusive,
        int endExclusive)
    {
        if (startInclusive < 0 || startInclusive > tokens.Count || endExclusive < startInclusive || endExclusive > tokens.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(startInclusive), "The token range is outside the token list.");
        }

        if (startInclusive == endExclusive)
        {
            return string.Empty;
        }

        var start = tokens[startInclusive].Start;
        var end = tokens[endExclusive - 1].End;
        if (start < 0 || end < start || end > sql.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(sql), "The token range is outside the source SQL.");
        }

        return sql.Substring(start, end - start).Trim();
    }
}

internal sealed class SqlTokenCursor
{
    public SqlTokenCursor(IReadOnlyList<SqlToken> tokens, string sql)
    {
        this.Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        this.Sql = sql ?? throw new ArgumentNullException(nameof(sql));
    }

    public IReadOnlyList<SqlToken> Tokens { get; }

    public string Sql { get; }

    public int Index { get; private set; }

    public bool IsAtEnd => this.Index >= this.Tokens.Count || this.Tokens[this.Index].Type == SqlTokenType.EndOfFile;

    public bool TryGetCurrent(out SqlToken token)
    {
        if (this.Index >= 0 && this.Index < this.Tokens.Count)
        {
            token = this.Tokens[this.Index];
            return true;
        }

        token = default;
        return false;
    }

    public SqlToken Current
        => this.TryGetCurrent(out var token)
            ? token
            : throw new InvalidOperationException("The token cursor is past the end of input.");

    public bool MoveNext()
    {
        if (this.IsAtEnd)
        {
            return false;
        }

        this.Index++;
        return !this.IsAtEnd;
    }

    public bool TryMoveTo(int index)
    {
        if (index < 0 || index > this.Tokens.Count)
        {
            return false;
        }

        this.Index = index;
        return this.Index < this.Tokens.Count;
    }
}
