namespace SqExpress.DataAccess;

/// <summary>Controls how database execution converts literals into ADO.NET command parameters.</summary>
public enum ParametrizationMode
{
    /// <summary>Exports literals directly into SQL without automatic parameters.</summary>
    None,

    /// <summary>Parameterizes supported literals and throws when the provider parameter limit is exceeded.</summary>
    ThrowOnLimit,

    /// <summary>Parameterizes supported literals up to the provider limit and emits remaining literals in SQL.</summary>
    LiteralFallback,
}
