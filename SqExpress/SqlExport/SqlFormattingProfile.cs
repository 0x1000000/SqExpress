using System;

namespace SqExpress.SqlExport;

/// <summary>Immutable, fully resolved SQL whitespace settings. Profiles can be shared between exporters.</summary>
public sealed class SqlFormattingProfile
{
    /// <summary>Existing exporter output without additional formatting.</summary>
    public static readonly SqlFormattingProfile Unformatted = new SqlFormattingProfile();

    /// <summary>Generous multiline layout with separate clauses, list items, aliases, and Boolean operators.</summary>
    public static readonly SqlFormattingProfile Spacious = Unformatted.WithOptions(new SqlFormattingOptions
    {
        SelectBody = SqlClauseBodyPlacement.NextLineIndented,
        WhereBody = SqlClauseBodyPlacement.NextLineIndented,
        GroupByBody = SqlClauseBodyPlacement.NextLineIndented,
        OrderByBody = SqlClauseBodyPlacement.NextLineIndented,
        SetBody = SqlClauseBodyPlacement.NextLineIndented,
        JoinOnBody = SqlClauseBodyPlacement.NextLineIndented,
        OutputBody = SqlClauseBodyPlacement.NextLineIndented,
        ValuesBody = SqlClauseBodyPlacement.NextLineIndented,
        TableAliasPlacement = SqlTableAliasPlacement.NextLineIndented,
        BooleanOperatorPlacement = SqlBooleanOperatorPlacement.SeparateLine,
        SelectList = SqlListLayout.OneItemPerLine,
        GroupByList = SqlListLayout.OneItemPerLine,
        OrderByList = SqlListLayout.OneItemPerLine,
        SetList = SqlListLayout.OneItemPerLine,
        OutputList = SqlListLayout.OneItemPerLine,
        ValuesRows = SqlListLayout.OneItemPerLine,
        NewLineBeforeClauses = true,
        NewLineBeforeJoins = true,
        NewLineAroundSetOperators = true,
        MultilineSubqueries = true,
        MultilineBooleanParentheses = true,
        MultilineCteBodies = true,
        NewLineBetweenCtes = true,
        NewLineBetweenStatements = true,
    });

    /// <summary>Gets the resolved IndentationSize setting.</summary>
    public int IndentationSize { get; }

    /// <summary>Gets the resolved NewLineStyle setting.</summary>
    public SqlNewLineStyle NewLineStyle { get; }

    /// <summary>Gets the resolved SelectBody setting.</summary>
    public SqlClauseBodyPlacement SelectBody { get; }

    /// <summary>Gets the resolved WhereBody setting.</summary>
    public SqlClauseBodyPlacement WhereBody { get; }

    /// <summary>Gets the resolved GroupByBody setting.</summary>
    public SqlClauseBodyPlacement GroupByBody { get; }

    /// <summary>Gets the resolved OrderByBody setting.</summary>
    public SqlClauseBodyPlacement OrderByBody { get; }

    /// <summary>Gets the resolved SetBody setting.</summary>
    public SqlClauseBodyPlacement SetBody { get; }

    /// <summary>Gets the resolved JoinOnBody setting.</summary>
    public SqlClauseBodyPlacement JoinOnBody { get; }

    /// <summary>Gets the resolved OutputBody setting.</summary>
    public SqlClauseBodyPlacement OutputBody { get; }

    /// <summary>Gets the resolved ValuesBody setting.</summary>
    public SqlClauseBodyPlacement ValuesBody { get; }

    /// <summary>Gets the resolved TableAliasPlacement setting.</summary>
    public SqlTableAliasPlacement TableAliasPlacement { get; }

    /// <summary>Gets the resolved BooleanOperatorPlacement setting.</summary>
    public SqlBooleanOperatorPlacement BooleanOperatorPlacement { get; }

    /// <summary>Gets the resolved SelectList setting.</summary>
    public SqlListLayout SelectList { get; }

    /// <summary>Gets the resolved GroupByList setting.</summary>
    public SqlListLayout GroupByList { get; }

    /// <summary>Gets the resolved OrderByList setting.</summary>
    public SqlListLayout OrderByList { get; }

    /// <summary>Gets the resolved SetList setting.</summary>
    public SqlListLayout SetList { get; }

    /// <summary>Gets the resolved OutputList setting.</summary>
    public SqlListLayout OutputList { get; }

    /// <summary>Gets the resolved ValuesRows setting.</summary>
    public SqlListLayout ValuesRows { get; }

    /// <summary>Gets the resolved NewLineBeforeClauses setting.</summary>
    public bool NewLineBeforeClauses { get; }

    /// <summary>Gets the resolved NewLineBeforeJoins setting.</summary>
    public bool NewLineBeforeJoins { get; }

    /// <summary>Gets the resolved NewLineAroundSetOperators setting.</summary>
    public bool NewLineAroundSetOperators { get; }

    /// <summary>Gets the resolved MultilineSubqueries setting.</summary>
    public bool MultilineSubqueries { get; }

    /// <summary>Gets the resolved MultilineBooleanParentheses setting.</summary>
    public bool MultilineBooleanParentheses { get; }

    /// <summary>Gets the resolved MultilineCteBodies setting.</summary>
    public bool MultilineCteBodies { get; }

    /// <summary>Gets the resolved NewLineBetweenCtes setting.</summary>
    public bool NewLineBetweenCtes { get; }

    /// <summary>Gets the resolved NewLineBetweenStatements setting.</summary>
    public bool NewLineBetweenStatements { get; }

    internal string NewLine => this.NewLineStyle == SqlNewLineStyle.Lf ? "\n" : this.NewLineStyle == SqlNewLineStyle.CrLf ? "\r\n" : Environment.NewLine;

    private SqlFormattingProfile()
    {
        this.IndentationSize = 4;
        this.NewLineStyle = SqlNewLineStyle.Platform;
        this.SelectBody = SqlClauseBodyPlacement.Inline;
        this.WhereBody = SqlClauseBodyPlacement.Inline;
        this.GroupByBody = SqlClauseBodyPlacement.Inline;
        this.OrderByBody = SqlClauseBodyPlacement.Inline;
        this.SetBody = SqlClauseBodyPlacement.Inline;
        this.JoinOnBody = SqlClauseBodyPlacement.Inline;
        this.OutputBody = SqlClauseBodyPlacement.Inline;
        this.ValuesBody = SqlClauseBodyPlacement.Inline;
        this.TableAliasPlacement = SqlTableAliasPlacement.Inline;
        this.BooleanOperatorPlacement = SqlBooleanOperatorPlacement.Inline;
        this.SelectList = SqlListLayout.Inline;
        this.GroupByList = SqlListLayout.Inline;
        this.OrderByList = SqlListLayout.Inline;
        this.SetList = SqlListLayout.Inline;
        this.OutputList = SqlListLayout.Inline;
        this.ValuesRows = SqlListLayout.Inline;
        this.NewLineBeforeClauses = false;
        this.NewLineBeforeJoins = false;
        this.NewLineAroundSetOperators = false;
        this.MultilineSubqueries = false;
        this.MultilineBooleanParentheses = false;
        this.MultilineCteBodies = false;
        this.NewLineBetweenCtes = false;
        this.NewLineBetweenStatements = false;
    }

    private SqlFormattingProfile(SqlFormattingProfile source, SqlFormattingOptions options)
    {
        this.IndentationSize = options.IndentationSize ?? source.IndentationSize;
        this.NewLineStyle = options.NewLineStyle ?? source.NewLineStyle;
        this.SelectBody = options.SelectBody ?? source.SelectBody;
        this.WhereBody = options.WhereBody ?? source.WhereBody;
        this.GroupByBody = options.GroupByBody ?? source.GroupByBody;
        this.OrderByBody = options.OrderByBody ?? source.OrderByBody;
        this.SetBody = options.SetBody ?? source.SetBody;
        this.JoinOnBody = options.JoinOnBody ?? source.JoinOnBody;
        this.OutputBody = options.OutputBody ?? source.OutputBody;
        this.ValuesBody = options.ValuesBody ?? source.ValuesBody;
        this.TableAliasPlacement = options.TableAliasPlacement ?? source.TableAliasPlacement;
        this.BooleanOperatorPlacement = options.BooleanOperatorPlacement ?? source.BooleanOperatorPlacement;
        this.SelectList = options.SelectList ?? source.SelectList;
        this.GroupByList = options.GroupByList ?? source.GroupByList;
        this.OrderByList = options.OrderByList ?? source.OrderByList;
        this.SetList = options.SetList ?? source.SetList;
        this.OutputList = options.OutputList ?? source.OutputList;
        this.ValuesRows = options.ValuesRows ?? source.ValuesRows;
        this.NewLineBeforeClauses = options.NewLineBeforeClauses ?? source.NewLineBeforeClauses;
        this.NewLineBeforeJoins = options.NewLineBeforeJoins ?? source.NewLineBeforeJoins;
        this.NewLineAroundSetOperators = options.NewLineAroundSetOperators ?? source.NewLineAroundSetOperators;
        this.MultilineSubqueries = options.MultilineSubqueries ?? source.MultilineSubqueries;
        this.MultilineBooleanParentheses = options.MultilineBooleanParentheses ?? source.MultilineBooleanParentheses;
        this.MultilineCteBodies = options.MultilineCteBodies ?? source.MultilineCteBodies;
        this.NewLineBetweenCtes = options.NewLineBetweenCtes ?? source.NewLineBetweenCtes;
        this.NewLineBetweenStatements = options.NewLineBetweenStatements ?? source.NewLineBetweenStatements;
        if (this.IndentationSize < 0) throw new ArgumentOutOfRangeException(nameof(options.IndentationSize));
        if (!Enum.IsDefined(typeof(SqlNewLineStyle), this.NewLineStyle)) throw new ArgumentOutOfRangeException(nameof(options.NewLineStyle));
        if (!Enum.IsDefined(typeof(SqlClauseBodyPlacement), this.SelectBody)) throw new ArgumentOutOfRangeException(nameof(options.SelectBody));
        if (!Enum.IsDefined(typeof(SqlClauseBodyPlacement), this.WhereBody)) throw new ArgumentOutOfRangeException(nameof(options.WhereBody));
        if (!Enum.IsDefined(typeof(SqlClauseBodyPlacement), this.GroupByBody)) throw new ArgumentOutOfRangeException(nameof(options.GroupByBody));
        if (!Enum.IsDefined(typeof(SqlClauseBodyPlacement), this.OrderByBody)) throw new ArgumentOutOfRangeException(nameof(options.OrderByBody));
        if (!Enum.IsDefined(typeof(SqlClauseBodyPlacement), this.SetBody)) throw new ArgumentOutOfRangeException(nameof(options.SetBody));
        if (!Enum.IsDefined(typeof(SqlClauseBodyPlacement), this.JoinOnBody)) throw new ArgumentOutOfRangeException(nameof(options.JoinOnBody));
        if (!Enum.IsDefined(typeof(SqlClauseBodyPlacement), this.OutputBody)) throw new ArgumentOutOfRangeException(nameof(options.OutputBody));
        if (!Enum.IsDefined(typeof(SqlClauseBodyPlacement), this.ValuesBody)) throw new ArgumentOutOfRangeException(nameof(options.ValuesBody));
        if (!Enum.IsDefined(typeof(SqlTableAliasPlacement), this.TableAliasPlacement)) throw new ArgumentOutOfRangeException(nameof(options.TableAliasPlacement));
        if (!Enum.IsDefined(typeof(SqlBooleanOperatorPlacement), this.BooleanOperatorPlacement)) throw new ArgumentOutOfRangeException(nameof(options.BooleanOperatorPlacement));
        if (!Enum.IsDefined(typeof(SqlListLayout), this.SelectList)) throw new ArgumentOutOfRangeException(nameof(options.SelectList));
        if (!Enum.IsDefined(typeof(SqlListLayout), this.GroupByList)) throw new ArgumentOutOfRangeException(nameof(options.GroupByList));
        if (!Enum.IsDefined(typeof(SqlListLayout), this.OrderByList)) throw new ArgumentOutOfRangeException(nameof(options.OrderByList));
        if (!Enum.IsDefined(typeof(SqlListLayout), this.SetList)) throw new ArgumentOutOfRangeException(nameof(options.SetList));
        if (!Enum.IsDefined(typeof(SqlListLayout), this.OutputList)) throw new ArgumentOutOfRangeException(nameof(options.OutputList));
        if (!Enum.IsDefined(typeof(SqlListLayout), this.ValuesRows)) throw new ArgumentOutOfRangeException(nameof(options.ValuesRows));
    }

    /// <summary>Returns an immutable snapshot with the supplied non-null overrides applied.</summary>
    /// <param name="options">Settings to override; omitted settings are inherited.</param>
    /// <returns>A new resolved profile.</returns>
    public SqlFormattingProfile WithOptions(SqlFormattingOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        return new SqlFormattingProfile(this, options);
    }
}