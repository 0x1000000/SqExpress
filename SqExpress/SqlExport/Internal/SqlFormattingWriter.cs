using System;
using System.Text;

namespace SqExpress.SqlExport.Internal
{
    /// <summary>
    /// Owns formatting state for one SQL rendering occurrence. SQL token rendering remains in the expression builder.
    /// </summary>
    internal sealed class SqlFormattingWriter
    {
        private readonly StringBuilder _builder;
        private readonly SqlFormattingRules _rules;
        private int _indentLevel;

        public SqlFormattingWriter(SqlFormattingProfile profile)
            : this(new StringBuilder(), profile)
        {
        }

        private SqlFormattingWriter(StringBuilder builder, SqlFormattingProfile profile)
        {
            this._builder = builder;
            this._rules = new SqlFormattingRules(profile);
        }

        public SqlFormattingProfile Profile => this._rules.Profile;

        public int IndentLevel
        {
            get => this._indentLevel;
            set => this._indentLevel = value;
        }

        public int Length
        {
            get => this._builder.Length;
            set => this._builder.Length = value;
        }

        public void Append(char value) => this._builder.Append(value);

        public void Append(string? value) => this._builder.Append(value);

        public void Append(int value) => this._builder.Append(value);

        public void Append(long value) => this._builder.Append(value);

        public void Append(char value, int repeatCount) => this._builder.Append(value, repeatCount);

        public void AppendHexByte(byte value) => this._builder.AppendFormat("{0:x2}", value);

        public void AppendEscapedSingleQuote(string value)
            => SqlInjectionChecker.AppendStringEscapeSingleQuote(this._builder, value);

        public void AppendEscapedSingleQuoteAndBackslash(string value)
            => SqlInjectionChecker.AppendStringEscapeSingleQuoteAndBackslash(this._builder, value);

        public void AppendEscapedBacktick(string value)
            => SqlInjectionChecker.AppendStringEscapeBacktick(this._builder, value);

        public void AppendEscapedDoubleQuote(string value)
            => SqlInjectionChecker.AppendStringEscapeDoubleQuote(this._builder, value);

        public void AppendEscapedClosingSquare(string value)
            => SqlInjectionChecker.AppendStringEscapeClosingSquare(this._builder, value);

        public override string ToString() => this._builder.ToString();

        public void AppendWhitespace(SqlWhitespace whitespace)
        {
            if (!whitespace.NewLine)
            {
                this._builder.Append(' ', whitespace.Spaces);
                return;
            }

            this._builder.Append(this.Profile.NewLine);
            this._builder.Append(' ', checked(this._indentLevel * this.Profile.IndentationSize));
        }

        public void AppendBoundary(SqlRenderSite site, int compactSpaces)
            => this.AppendWhitespace(this._rules.Boundary(site, compactSpaces));

        public void AppendClause(
            string text,
            SqlRenderSite site = SqlRenderSite.Clause,
            int compactLeadingSpaces = 1,
            int compactTrailingSpaces = 0)
        {
            this.AppendBoundary(site, compactLeadingSpaces);
            this._builder.Append(text);
            this._builder.Append(' ', compactTrailingSpaces);
        }

        public Scope BeginBody(SqlRenderSite site, int compactSpaces)
            => new Scope(this, this._rules.Body(site), compactSpaces, null);

        public Scope BeginParentheses(SqlRenderSite site, char end)
            => new Scope(this, this._rules.Body(site), 0, end);

        public void AppendItems(int count, SqlRenderSite site, Action<int> render, int compactSpaces)
        {
            int saved = this._indentLevel;
            bool body = this._rules.Body(site);
            bool list = this._rules.List(site);
            try
            {
                for (int i = 0; i < count; i++)
                {
                    if (i > 0) this._builder.Append(',');
                    bool newline = i == 0 ? body : list;
                    this._indentLevel = saved + (body || (i > 0 && list) ? 1 : 0);
                    this.AppendWhitespace(new SqlWhitespace(newline, i == 0 && !newline ? compactSpaces : 0));
                    render(i);
                }
            }
            finally
            {
                this._indentLevel = saved;
            }
        }

        public readonly struct Scope : IDisposable
        {
            private readonly SqlFormattingWriter _writer;
            private readonly int _indent;
            private readonly SqlTrivia _trivia;
            private readonly char? _end;

            internal Scope(SqlFormattingWriter writer, bool multiline, int compactSpaces, char? end)
            {
                this._writer = writer;
                this._indent = writer._indentLevel;
                this._end = end;
                this._trivia = new SqlTrivia(
                    new SqlWhitespace(multiline, multiline ? 0 : compactSpaces),
                    new SqlWhitespace(multiline && end.HasValue));
                if (multiline) writer._indentLevel++;
                writer.AppendWhitespace(this._trivia.Leading);
            }

            public void Dispose()
            {
                this._writer._indentLevel = this._indent;
                this._writer.AppendWhitespace(this._trivia.Trailing);
                if (this._end.HasValue) this._writer._builder.Append(this._end.Value);
            }
        }
    }
}
