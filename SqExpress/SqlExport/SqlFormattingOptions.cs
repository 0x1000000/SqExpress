namespace SqExpress.SqlExport;

/// <summary>Controls NewLineStyle in SQL output.</summary>
public enum SqlNewLineStyle
{
    /// <summary>Uses Platform layout.</summary>
    Platform,
    /// <summary>Uses Lf layout.</summary>
    Lf,
    /// <summary>Uses CrLf layout.</summary>
    CrLf
}

/// <summary>Controls ClauseBodyPlacement in SQL output.</summary>
public enum SqlClauseBodyPlacement
{
    /// <summary>Uses Inline layout.</summary>
    Inline,
    /// <summary>Uses NextLineIndented layout.</summary>
    NextLineIndented
}

/// <summary>Controls TableAliasPlacement in SQL output.</summary>
public enum SqlTableAliasPlacement
{
    /// <summary>Uses Inline layout.</summary>
    Inline,
    /// <summary>Uses NextLineIndented layout.</summary>
    NextLineIndented
}

/// <summary>Controls BooleanOperatorPlacement in SQL output.</summary>
public enum SqlBooleanOperatorPlacement
{
    /// <summary>Uses Inline layout.</summary>
    Inline,
    /// <summary>Uses LineStart layout.</summary>
    LineStart,
    /// <summary>Uses SeparateLine layout.</summary>
    SeparateLine
}

/// <summary>Controls ListLayout in SQL output.</summary>
public enum SqlListLayout
{
    /// <summary>Uses Inline layout.</summary>
    Inline,
    /// <summary>Uses OneItemPerLine layout.</summary>
    OneItemPerLine
}

/// <summary>A mutable patch of settings to apply to an immutable formatting profile. Null properties inherit the base profile.</summary>
public sealed class SqlFormattingOptions
{
    /// <summary>Gets or sets the optional IndentationSize override.</summary>
    public int? IndentationSize { get; set; }

    /// <summary>Gets or sets the optional NewLineStyle override.</summary>
    public SqlNewLineStyle? NewLineStyle { get; set; }

    /// <summary>Gets or sets the optional SelectBody override.</summary>
    public SqlClauseBodyPlacement? SelectBody { get; set; }

    /// <summary>Gets or sets the optional WhereBody override.</summary>
    public SqlClauseBodyPlacement? WhereBody { get; set; }

    /// <summary>Gets or sets the optional GroupByBody override.</summary>
    public SqlClauseBodyPlacement? GroupByBody { get; set; }

    /// <summary>Gets or sets the optional OrderByBody override.</summary>
    public SqlClauseBodyPlacement? OrderByBody { get; set; }

    /// <summary>Gets or sets the optional SetBody override.</summary>
    public SqlClauseBodyPlacement? SetBody { get; set; }

    /// <summary>Gets or sets the optional JoinOnBody override.</summary>
    public SqlClauseBodyPlacement? JoinOnBody { get; set; }

    /// <summary>Gets or sets the optional OutputBody override.</summary>
    public SqlClauseBodyPlacement? OutputBody { get; set; }

    /// <summary>Gets or sets the optional ValuesBody override.</summary>
    public SqlClauseBodyPlacement? ValuesBody { get; set; }

    /// <summary>Gets or sets the optional TableAliasPlacement override.</summary>
    public SqlTableAliasPlacement? TableAliasPlacement { get; set; }

    /// <summary>Gets or sets the optional BooleanOperatorPlacement override.</summary>
    public SqlBooleanOperatorPlacement? BooleanOperatorPlacement { get; set; }

    /// <summary>Gets or sets the optional SelectList override.</summary>
    public SqlListLayout? SelectList { get; set; }

    /// <summary>Gets or sets the optional GroupByList override.</summary>
    public SqlListLayout? GroupByList { get; set; }

    /// <summary>Gets or sets the optional OrderByList override.</summary>
    public SqlListLayout? OrderByList { get; set; }

    /// <summary>Gets or sets the optional SetList override.</summary>
    public SqlListLayout? SetList { get; set; }

    /// <summary>Gets or sets the optional OutputList override.</summary>
    public SqlListLayout? OutputList { get; set; }

    /// <summary>Gets or sets the optional ValuesRows override.</summary>
    public SqlListLayout? ValuesRows { get; set; }

    /// <summary>Gets or sets the optional NewLineBeforeClauses override.</summary>
    public bool? NewLineBeforeClauses { get; set; }

    /// <summary>Gets or sets the optional NewLineBeforeJoins override.</summary>
    public bool? NewLineBeforeJoins { get; set; }

    /// <summary>Gets or sets the optional NewLineAroundSetOperators override.</summary>
    public bool? NewLineAroundSetOperators { get; set; }

    /// <summary>Gets or sets the optional MultilineSubqueries override.</summary>
    public bool? MultilineSubqueries { get; set; }

    /// <summary>Gets or sets the optional MultilineBooleanParentheses override.</summary>
    public bool? MultilineBooleanParentheses { get; set; }

    /// <summary>Gets or sets the optional MultilineCteBodies override.</summary>
    public bool? MultilineCteBodies { get; set; }

    /// <summary>Gets or sets the optional NewLineBetweenCtes override.</summary>
    public bool? NewLineBetweenCtes { get; set; }

    /// <summary>Gets or sets the optional NewLineBetweenStatements override.</summary>
    public bool? NewLineBetweenStatements { get; set; }
}