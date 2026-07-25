using System;

namespace SqExpress;

/// <summary>Specifies whether a table-binding diagnostic is recoverable or fatal.</summary>
public enum TableBindingSeverity
{
    /// <summary>Reports the condition while allowing binding to succeed.</summary>
    Warning,
    /// <summary>Prevents binding from succeeding.</summary>
    Error
}

/// <summary>Identifies a table- or column-binding condition.</summary>
public enum TableBindingDiagnosticCode
{
    /// <summary>Multiple catalog descriptors have the same full table name.</summary>
    DuplicateCatalogTable,
    /// <summary>A referenced table is absent from the supplied catalog.</summary>
    UnknownTable,
    /// <summary>A table reference matches multiple visible candidates.</summary>
    AmbiguousTable,
    /// <summary>A referenced column is absent from its resolved source.</summary>
    UnknownColumn,
    /// <summary>An unqualified column matches multiple visible sources.</summary>
    AmbiguousColumn,
    /// <summary>A column qualifier is not valid in the current scope.</summary>
    InvalidColumnSource,
    /// <summary>The syntax contains a reference shape the binder cannot resolve.</summary>
    UnsupportedReference
}

/// <summary>Describes one table-binding warning or error.</summary>
public sealed class TableBindingDiagnostic
{
    /// <summary>Initializes a binding diagnostic.</summary>
    public TableBindingDiagnostic(TableBindingDiagnosticCode code, TableBindingSeverity severity, string message)
    {
        this.Code = code;
        this.Severity = severity;
        this.Message = message;
    }

    /// <summary>Gets the machine-readable diagnostic category.</summary>
    public TableBindingDiagnosticCode Code { get; }

    /// <summary>Gets whether the diagnostic is a warning or error.</summary>
    public TableBindingSeverity Severity { get; }

    /// <summary>Gets the human-readable diagnostic message.</summary>
    public string Message { get; }

    /// <summary>Returns <see cref="Message"/>.</summary>
    public override string ToString() => this.Message;
}

/// <summary>Configures severity policy for syntax-tree table and column binding.</summary>
public sealed class TableBindingOptions
{
    /// <summary>Gets or sets a callback that maps diagnostic categories to warning or error severity.</summary>
    /// <remarks>When null, every binding diagnostic is treated as an error.</remarks>
    public Func<TableBindingDiagnosticCode, TableBindingSeverity>? SeverityResolver { get; set; }

    internal TableBindingSeverity ResolveSeverity(TableBindingDiagnosticCode code)
        => this.SeverityResolver?.Invoke(code) ?? TableBindingSeverity.Error;
}
