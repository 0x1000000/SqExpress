namespace SqExpress.DataAccess;

/// <summary>Controls discovery of database tables and views.</summary>
public class SqGetTablesOptions
{
    /// <summary>Gets or sets whether ordinary views are included alongside tables. Defaults to false.</summary>
    public bool IncludeViews { get; set; }

    /// <summary>Gets or sets whether unsupported column types are omitted instead of rejected. Defaults to false.</summary>
    public bool SkipUnknownColumnTypes { get; set; }
}
