using System;
using System.Collections.Generic;
using System.Text;

namespace SqExpress.Internal;

internal enum SqJsonPathSegmentKind
{
    Property,
    Index
}

internal readonly struct SqJsonPathSegment
{
    private SqJsonPathSegment(SqJsonPathSegmentKind kind, string? property, int index)
    {
        this.Kind = kind;
        this.Property = property;
        this.Index = index;
    }

    public SqJsonPathSegmentKind Kind { get; }
    public string? Property { get; }
    public int Index { get; }

    public static SqJsonPathSegment ForProperty(string property) => new(SqJsonPathSegmentKind.Property, property, 0);
    public static SqJsonPathSegment ForIndex(int index) => new(SqJsonPathSegmentKind.Index, null, index);
}

internal sealed class SqJsonPathModel
{
    public SqJsonPathModel(string value, IReadOnlyList<SqJsonPathSegment> segments)
    {
        this.Value = value;
        this.Segments = segments;
    }

    public string Value { get; }
    public IReadOnlyList<SqJsonPathSegment> Segments { get; }
}

internal static class SqJsonPathParser
{
    public static SqJsonPathModel Parse(string? path)
    {
        if (path == null)
        {
            throw Error(0, "expected '$'");
        }

        if (path.Length == 0 || path[0] != '$')
        {
            throw Error(0, "expected '$'");
        }

        var result = new List<SqJsonPathSegment>();
        var position = 1;
        while (position < path.Length)
        {
            if (path[position] == '.')
            {
                position++;
                if (position >= path.Length)
                {
                    throw Error(position, "expected a property name");
                }

                if (path[position] == '"')
                {
                    result.Add(SqJsonPathSegment.ForProperty(ParseQuotedProperty(path, ref position)));
                }
                else
                {
                    var start = position;
                    if (!IsIdentifierStart(path[position]))
                    {
                        throw Error(position, "expected a property name");
                    }

                    position++;
                    while (position < path.Length && IsIdentifierPart(path[position]))
                    {
                        position++;
                    }

                    result.Add(SqJsonPathSegment.ForProperty(path.Substring(start, position - start)));
                }

                continue;
            }

            if (path[position] == '[')
            {
                var start = position;
                position++;
                if (position >= path.Length || !char.IsDigit(path[position]))
                {
                    throw Error(position, "expected a non-negative array index");
                }

                var index = 0;
                while (position < path.Length && char.IsDigit(path[position]))
                {
                    try
                    {
                        index = checked(index * 10 + (path[position] - '0'));
                    }
                    catch (OverflowException)
                    {
                        throw Error(start + 1, "array index is too large");
                    }
                    position++;
                }

                if (position >= path.Length || path[position] != ']')
                {
                    throw Error(position, "expected ']'");
                }

                position++;
                result.Add(SqJsonPathSegment.ForIndex(index));
                continue;
            }

            throw Error(position, "expected '.' or '['");
        }

        return new SqJsonPathModel(path, result);
    }

    public static void ValidateOutputPath(string? path)
    {
        var model = Parse(path);
        if (model.Segments.Count == 0)
        {
            throw new SqExpressException("A JSON output path must contain at least one property.");
        }

        foreach (var segment in model.Segments)
        {
            if (segment.Kind != SqJsonPathSegmentKind.Property)
            {
                throw new SqExpressException("A JSON output path may contain object properties only.");
            }
        }
    }

    private static string ParseQuotedProperty(string path, ref int position)
    {
        position++;
        var result = new StringBuilder();
        while (position < path.Length)
        {
            var ch = path[position++];
            if (ch == '"')
            {
                if (result.Length == 0)
                {
                    throw Error(position - 1, "property name cannot be empty");
                }
                return result.ToString();
            }

            if (ch == '\\')
            {
                if (position >= path.Length)
                {
                    throw Error(position, "incomplete escape sequence");
                }

                var escaped = path[position++];
                switch (escaped)
                {
                    case '"': result.Append('"'); break;
                    case '\\': result.Append('\\'); break;
                    case '/': result.Append('/'); break;
                    case 'b': result.Append('\b'); break;
                    case 'f': result.Append('\f'); break;
                    case 'n': result.Append('\n'); break;
                    case 'r': result.Append('\r'); break;
                    case 't': result.Append('\t'); break;
                    default: throw Error(position - 1, "unsupported escape sequence");
                }
            }
            else
            {
                result.Append(ch);
            }
        }

        throw Error(position, "expected closing quote");
    }

    private static bool IsIdentifierStart(char ch) => ch == '_' || char.IsLetter(ch);
    private static bool IsIdentifierPart(char ch) => ch == '_' || char.IsLetterOrDigit(ch);
    private static SqExpressException Error(int position, string message)
        => new($"Invalid JSON path at position {position}: {message}.");
}
