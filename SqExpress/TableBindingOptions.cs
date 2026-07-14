using System;

namespace SqExpress;

public enum TableBindingSeverity
{
    Warning,
    Error
}

public enum TableBindingDiagnosticCode
{
    DuplicateCatalogTable,
    UnknownTable,
    AmbiguousTable,
    UnknownColumn,
    AmbiguousColumn,
    InvalidColumnSource,
    UnsupportedReference
}

public sealed class TableBindingDiagnostic
{
    public TableBindingDiagnostic(TableBindingDiagnosticCode code, TableBindingSeverity severity, string message)
    {
        this.Code = code;
        this.Severity = severity;
        this.Message = message;
    }

    public TableBindingDiagnosticCode Code { get; }

    public TableBindingSeverity Severity { get; }

    public string Message { get; }

    public override string ToString() => this.Message;
}

public sealed class TableBindingOptions
{
    public Func<TableBindingDiagnosticCode, TableBindingSeverity>? SeverityResolver { get; set; }

    internal TableBindingSeverity ResolveSeverity(TableBindingDiagnosticCode code)
        => this.SeverityResolver?.Invoke(code) ?? TableBindingSeverity.Error;
}
