using System;

namespace SqExpress.SqlParser.Internal.Parsing
{
    internal enum SqlTokenType
    {
        EndOfFile,
        Identifier,
        BracketIdentifier,
        StringLiteral,
        NumberLiteral,
        Comma,
        Dot,
        OpenParen,
        CloseParen,
        Semicolon,
        Operator,
        Symbol,
    }

    internal readonly struct SqlToken
    {
        public SqlToken(SqlTokenType type, string text, int start, int length)
        {
            this.Type = type;
            this.Text = text;
            this.Start = start;
            this.Length = length;
        }

        public SqlTokenType Type { get; }

        public string Text { get; }

        public int Start { get; }

        public int Length { get; }

        public int End => this.Start + this.Length;

        public bool IsKeyword(string keyword)
            => this.Type == SqlTokenType.Identifier
               && string.Equals(this.Text, keyword, StringComparison.OrdinalIgnoreCase);

        public bool IsIdentifierLike
            => this.Type == SqlTokenType.Identifier || this.Type == SqlTokenType.BracketIdentifier;

        public string IdentifierValue
        {
            get
            {
                if (this.Type == SqlTokenType.BracketIdentifier
                    && this.Text.Length >= 2
                    && this.Text[0] == '['
                    && this.Text[this.Text.Length - 1] == ']')
                {
                    return this.Text.Substring(1, this.Text.Length - 2).Replace("]]", "]");
                }

                return this.Text;
            }
        }

        public override string ToString()
            => this.Text;
    }
}
