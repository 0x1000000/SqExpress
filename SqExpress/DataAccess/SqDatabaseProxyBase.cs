using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using SqExpress.DbMetadata;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;

namespace SqExpress.DataAccess;

/// <summary>Forwards database operations to a target, allowing derived classes to override selected operations.</summary>
/// <param name="target">The database to which operations, including disposal, are forwarded.</param>
public abstract class SqDatabaseProxyBase(ISqDatabase target) : ISqDatabase
{
    /// <summary>Gets the database to which operations are forwarded.</summary>
    protected ISqDatabase Target => target;

    /// <inheritdoc />
    public virtual ISqTransaction BeginTransaction() => this.Target.BeginTransaction();

    /// <inheritdoc />
    public virtual ISqTransaction BeginTransactionOrUseExisting(out bool isNewTransaction)
        => this.Target.BeginTransactionOrUseExisting(out isNewTransaction);

    /// <inheritdoc />
    public virtual ISqTransaction BeginTransaction(IsolationLevel isolationLevel)
        => this.Target.BeginTransaction(isolationLevel);

    /// <inheritdoc />
    public virtual ISqTransaction BeginTransactionOrUseExisting(IsolationLevel isolationLevel, out bool isNewTransaction)
        => this.Target.BeginTransactionOrUseExisting(isolationLevel, out isNewTransaction);

    /// <inheritdoc />
    public virtual Task<TAgg> Query<TAgg>(IExprQuery query, TAgg seed, Func<TAgg, ISqDataRecordReader, TAgg> aggregator, CancellationToken cancellationToken = default)
        => this.Target.Query(query, seed, aggregator, cancellationToken);

    /// <inheritdoc />
    public virtual Task<TAgg> Query<TAgg>(IExprQuery query, TAgg seed, Func<TAgg, ISqDataRecordReader, Task<TAgg>> aggregator, CancellationToken cancellationToken = default)
        => this.Target.Query(query, seed, aggregator, cancellationToken);

#if !NETSTANDARD
    /// <inheritdoc />
    public virtual ValueTask<(ISqTransaction transaction, bool isNewTransaction)> BeginTransactionOrUseExistingAsync()
        => this.Target.BeginTransactionOrUseExistingAsync();

    /// <inheritdoc />
    public virtual ValueTask<(ISqTransaction transaction, bool isNewTransaction)> BeginTransactionOrUseExistingAsync(IsolationLevel isolationLevel)
        => this.Target.BeginTransactionOrUseExistingAsync(isolationLevel);

    /// <inheritdoc />
    public virtual ValueTask<ISqTransaction> BeginTransactionAsync() => this.Target.BeginTransactionAsync();

    /// <inheritdoc />
    public virtual ValueTask<ISqTransaction> BeginTransactionAsync(IsolationLevel isolationLevel)
        => this.Target.BeginTransactionAsync(isolationLevel);

    /// <inheritdoc />
    public virtual IAsyncEnumerable<ISqDataRecordReader> Query(IExprQuery query, CancellationToken cancellationToken = default)
        => this.Target.Query(query, cancellationToken);

    /// <inheritdoc />
    public virtual ValueTask DisposeAsync() => this.Target.DisposeAsync();
#endif

    /// <inheritdoc />
    public virtual Task<object?> QueryScalar(IExprQuery query, CancellationToken cancellationToken = default)
        => this.Target.QueryScalar(query, cancellationToken);

    /// <inheritdoc />
    public virtual Task Exec(IExprExec statement, CancellationToken cancellationToken = default)
        => this.Target.Exec(statement, cancellationToken);

    /// <inheritdoc />
    public virtual Task Statement(IStatement statement, CancellationToken cancellationToken = default)
        => this.Target.Statement(statement, cancellationToken);

    /// <inheritdoc />
    public virtual Task<IReadOnlyList<SqTable>> GetTables(CancellationToken cancellationToken = default)
        => this.Target.GetTables(cancellationToken);

    /// <inheritdoc />
    public virtual Task<IReadOnlyList<SqTable>> GetTables(bool skipUnknownColumnTypes, CancellationToken cancellationToken = default)
        => this.Target.GetTables(skipUnknownColumnTypes, cancellationToken);

    /// <inheritdoc />
    public virtual Task<IReadOnlyList<SqTable>> GetTables(SqGetTablesOptions options, CancellationToken cancellationToken = default)
        => this.Target.GetTables(options, cancellationToken);

    /// <inheritdoc />
    public virtual void Dispose() => this.Target.Dispose();
}
