namespace SqExpress.SqlExport.Internal
{
    internal enum SqlRenderSite
    {
        Clause,
        Join,
        SetOperator,
        BooleanOperator,
        BooleanRight,
        TableAlias,
        Select,
        Where,
        GroupBy,
        OrderBy,
        Set,
        JoinOn,
        Output,
        Values,
        Subquery,
        BooleanParentheses,
        CteBody,
        CteSeparator,
        Statement,
        Inline
    }

    internal readonly struct SqlWhitespace
    {
        public readonly bool NewLine;
        public readonly int Spaces;

        public SqlWhitespace(bool newLine, int spaces = 0)
        {
            this.NewLine = newLine;
            this.Spaces = spaces;
        }
    }

    internal readonly struct SqlTrivia
    {
        public readonly SqlWhitespace Leading;
        public readonly SqlWhitespace Trailing;

        public SqlTrivia(SqlWhitespace leading, SqlWhitespace trailing = default)
        {
            this.Leading = leading;
            this.Trailing = trailing;
        }
    }

    // Pure rules: no expression traversal, node cache, or mutable rendering state.
    internal sealed class SqlFormattingRules
    {
        public SqlFormattingProfile Profile { get; }

        public SqlFormattingRules(SqlFormattingProfile profile)
        {
            this.Profile = profile;
        }

        public SqlWhitespace Boundary(SqlRenderSite site, int fallbackSpaces)
        {
            bool newline = site switch
            {
                SqlRenderSite.Clause => this.Profile.NewLineBeforeClauses,
                SqlRenderSite.Join => this.Profile.NewLineBeforeJoins,
                SqlRenderSite.SetOperator => this.Profile.NewLineAroundSetOperators,
                SqlRenderSite.BooleanOperator => this.Profile.BooleanOperatorPlacement !=
                                                 SqlBooleanOperatorPlacement.Inline,
                SqlRenderSite.BooleanRight => this.Profile.BooleanOperatorPlacement ==
                                              SqlBooleanOperatorPlacement.SeparateLine,
                SqlRenderSite.CteSeparator => this.Profile.NewLineBetweenCtes,
                SqlRenderSite.Statement => this.Profile.NewLineBetweenStatements,
                _ => false
            };
            if (site == SqlRenderSite.BooleanRight &&
                this.Profile.BooleanOperatorPlacement == SqlBooleanOperatorPlacement.LineStart)
            {
                fallbackSpaces = 1;
            }

            return new SqlWhitespace(newline, newline ? 0 : fallbackSpaces);
        }

        public bool Body(SqlRenderSite site) => site switch
        {
            SqlRenderSite.Select => this.Profile.SelectBody == SqlClauseBodyPlacement.NextLineIndented,
            SqlRenderSite.Where => this.Profile.WhereBody == SqlClauseBodyPlacement.NextLineIndented,
            SqlRenderSite.GroupBy => this.Profile.GroupByBody == SqlClauseBodyPlacement.NextLineIndented,
            SqlRenderSite.OrderBy => this.Profile.OrderByBody == SqlClauseBodyPlacement.NextLineIndented,
            SqlRenderSite.Set => this.Profile.SetBody == SqlClauseBodyPlacement.NextLineIndented,
            SqlRenderSite.JoinOn => this.Profile.JoinOnBody == SqlClauseBodyPlacement.NextLineIndented,
            SqlRenderSite.Output => this.Profile.OutputBody == SqlClauseBodyPlacement.NextLineIndented,
            SqlRenderSite.Values => this.Profile.ValuesBody == SqlClauseBodyPlacement.NextLineIndented,
            SqlRenderSite.TableAlias => this.Profile.TableAliasPlacement == SqlTableAliasPlacement.NextLineIndented,
            SqlRenderSite.Subquery => this.Profile.MultilineSubqueries,
            SqlRenderSite.BooleanParentheses => this.Profile.MultilineBooleanParentheses,
            SqlRenderSite.CteBody => this.Profile.MultilineCteBodies,
            _ => false
        };

        public bool List(SqlRenderSite site) => site switch
        {
            SqlRenderSite.Select => this.Profile.SelectList == SqlListLayout.OneItemPerLine,
            SqlRenderSite.GroupBy => this.Profile.GroupByList == SqlListLayout.OneItemPerLine,
            SqlRenderSite.OrderBy => this.Profile.OrderByList == SqlListLayout.OneItemPerLine,
            SqlRenderSite.Set => this.Profile.SetList == SqlListLayout.OneItemPerLine,
            SqlRenderSite.Output => this.Profile.OutputList == SqlListLayout.OneItemPerLine,
            SqlRenderSite.Values => this.Profile.ValuesRows == SqlListLayout.OneItemPerLine,
            _ => false
        };
    }
}
