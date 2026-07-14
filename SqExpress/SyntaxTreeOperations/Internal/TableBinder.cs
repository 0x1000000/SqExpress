using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SqExpress.DbMetadata;
using SqExpress.Syntax;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Update;

namespace SqExpress.SyntaxTreeOperations.Internal;

internal sealed class TableBinder : ExprVisitorBase
{
    private readonly IReadOnlyList<TableBase> _catalog;
    private readonly TableBindingOptions _options;
    private readonly Dictionary<IExpr, IExpr> _replacements = new Dictionary<IExpr, IExpr>(ReferenceComparer.Instance);
    private readonly List<TableBindingDiagnostic> _warnings = new List<TableBindingDiagnostic>();
    private readonly List<TableBindingDiagnostic> _errors = new List<TableBindingDiagnostic>();
    private Scope? _scope;
    private bool _suppressOuterScope;

    private TableBinder(IReadOnlyList<TableBase> catalog, TableBindingOptions options)
    {
        this._catalog = catalog;
        this._options = options;
        this.ValidateCatalog();
    }

    public static T Bind<T>(
        T expression,
        IReadOnlyList<TableBase> tables,
        TableBindingOptions? options,
        out IReadOnlyList<TableBindingDiagnostic> warnings,
        out IReadOnlyList<TableBindingDiagnostic> errors)
        where T : IExpr
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));
        if (tables == null) throw new ArgumentNullException(nameof(tables));

        var binder = new TableBinder(tables, options ?? new TableBindingOptions());
        binder.AcceptRoot(expression);
        var result = (T)expression.SyntaxTree().Modify(node =>
            binder._replacements.TryGetValue(node, out var replacement) ? replacement : node)!;
        warnings = binder._warnings;
        errors = binder._errors;
        return result;
    }

    private void AcceptRoot(IExpr expression) => this.Accept(expression);

    public override void VisitExprQuerySpecification(ExprQuerySpecification expr)
    {
        var previous = this._scope;
        var parent = this._suppressOuterScope ? null : previous;
        this._suppressOuterScope = false;
        this._scope = new Scope(parent);

        if (expr.From is not null)
        {
            this.VisitSource(expr.From, false);
        }

        this.Accept(expr.SelectList);
        this.Accept(expr.Top);
        this.Accept(expr.Where);
        this.Accept(expr.GroupBy);
        this._scope = previous;
    }

    public override void VisitExprUpdate(ExprUpdate expr)
    {
        var previous = this._scope;
        this._scope = new Scope(null);
        var target = this.BindTable(expr.Target);
        if (target != null) this._scope.Physical.Add(target);
        if (expr.Source != null && !ReferenceEquals(expr.Source, expr.Target)) this.VisitSource(expr.Source, false);
        this.Accept(expr.SetClause);
        this.Accept(expr.Filter);
        this._scope = previous;
    }

    public override void VisitExprDelete(ExprDelete expr)
    {
        var previous = this._scope;
        this._scope = new Scope(null);
        var target = this.BindTable(expr.Target);
        if (target != null) this._scope.Physical.Add(target);
        if (expr.Source != null && !ReferenceEquals(expr.Source, expr.Target)) this.VisitSource(expr.Source, false);
        this.Accept(expr.Filter);
        this._scope = previous;
    }

    public override void VisitExprColumn(ExprColumn expr)
    {
        if (expr is TableColumn || this._scope == null)
        {
            return;
        }

        var resolution = this.ResolveColumn(expr);
        if (resolution.Column is { } column)
        {
            this._replacements[expr] = column;
        }
        else if (resolution.DiagnosticCode.HasValue)
        {
            this.Report(resolution.DiagnosticCode.Value, resolution.Message!);
        }
    }

    private void VisitSource(IExprTableSource source, bool lateral)
    {
        switch (source)
        {
            case ExprTable table:
                var bound = this.BindTable(table);
                if (bound != null)
                {
                    this._scope!.Physical.Add(bound);
                }
                break;
            case ExprJoinedTable joined:
                this.VisitSource(joined.Left, false);
                this.VisitSource(joined.Right, false);
                this.Accept(joined.SearchCondition);
                break;
            case ExprCrossedTable crossed:
                this.VisitSource(crossed.Left, false);
                this.VisitSource(crossed.Right, false);
                break;
            case ExprLateralCrossedTable lateralCrossed:
                this.VisitSource(lateralCrossed.Left, false);
                this.VisitSource(lateralCrossed.Right, true);
                break;
            case ExprDerivedTableQuery derived:
                this.AddDerived(derived);
                this._suppressOuterScope = !lateral;
                this.Accept(derived.Query);
                break;
            case ExprDerivedTableValues values:
                this.AddDerived(values);
                this.Accept(values.Values);
                break;
            default:
                if (source.Alias?.Alias is { } alias)
                {
                    this._scope!.DerivedAliases.Add(alias);
                }
                this.Accept(source);
                break;
        }
    }

    private void AddDerived(ExprDerivedTable derived)
    {
        this._scope!.DerivedAliases.Add(derived.Alias.Alias);

        foreach (var selecting in derived.ExtractSelecting())
        {
            if (selecting is IExprNamedSelecting named && named.OutputName != null)
            {
                this._scope!.DerivedColumns.Add(named.OutputName.ToLowerInvariant());
            }
        }
    }

    private SqTable? BindTable(ExprTable table)
    {
        var matches = this.FindTables(table.FullName);
        if (matches.Count == 0)
        {
            this.Report(TableBindingDiagnosticCode.UnknownTable, $"Could not bind table '{Format(table.FullName)}'.");
            return null;
        }
        if (matches.Count > 1)
        {
            this.Report(TableBindingDiagnosticCode.AmbiguousTable, $"Table reference '{Format(table.FullName)}' is ambiguous.");
            return null;
        }

        if (table is SqTable alreadyBound)
        {
            return alreadyBound;
        }

        var result = SqTable.Clone(matches[0], table.Alias);
        this._replacements[table] = result;
        return result;
    }

    private IReadOnlyList<TableBase> FindTables(IExprTableFullName name)
    {
        var input = name.AsExprTableFullName();
        return this._catalog.Where(candidate =>
        {
            var canonical = candidate.FullName.AsExprTableFullName();
            if (!ExprNameEqualityComparer.CaseInsensitive.Equals(input.TableName, canonical.TableName)) return false;
            if (input.DbSchema?.Schema is { } inputSchema
                && (canonical.DbSchema?.Schema is not { } canonicalSchema
                    || !ExprNameEqualityComparer.CaseInsensitive.Equals(inputSchema, canonicalSchema))) return false;
            if (input.DbSchema?.Database is { } inputDatabase
                && (canonical.DbSchema?.Database is not { } canonicalDatabase
                    || !ExprNameEqualityComparer.CaseInsensitive.Equals(inputDatabase, canonicalDatabase))) return false;
            return true;
        }).ToArray();
    }

    private ColumnResolution ResolveColumn(ExprColumn expr)
    {
        if (expr.Source is ExprTableAlias tableAlias)
        {
            var alias = tableAlias.Alias;
            for (var scope = this._scope; scope != null; scope = scope.Parent)
            {
                if (scope.DerivedAliases.Any(a => AliasEquals(a, alias))) return default;
                var tables = scope.Physical.Where(t => t.Alias != null && AliasEquals(t.Alias.Alias, alias)).ToArray();
                if (tables.Length == 1) return ResolveInTable(tables[0], expr);
                if (tables.Length > 1) return ColumnResolution.Error(TableBindingDiagnosticCode.AmbiguousColumn, $"Column source alias '{AliasDisplay(alias)}' is ambiguous.");
            }
            return ColumnResolution.Error(TableBindingDiagnosticCode.InvalidColumnSource, $"Unknown column source alias '{AliasDisplay(alias)}'.");
        }

        if (expr.Source is IExprTableFullName fullName)
        {
            for (var scope = this._scope; scope != null; scope = scope.Parent)
            {
                var tables = scope.Physical.Where(t => FullNameMatches(fullName, t.FullName)).ToArray();
                if (tables.Length == 1) return ResolveInTable(tables[0], expr);
                if (tables.Length > 1) return ColumnResolution.Error(TableBindingDiagnosticCode.AmbiguousColumn, $"Column source '{Format(fullName)}' is ambiguous.");
            }
            return ColumnResolution.Error(TableBindingDiagnosticCode.InvalidColumnSource, $"Unknown column source '{Format(fullName)}'.");
        }

        if (expr.Source != null)
        {
            return default;
        }

        for (var scope = this._scope; scope != null; scope = scope.Parent)
        {
            var matches = scope.Physical
                .SelectMany(t => t.Columns.Where(c => ExprNameEqualityComparer.CaseInsensitive.Equals(c.ColumnName, expr.ColumnName)))
                .ToArray();
            var derivedMatch = scope.DerivedColumns.Contains(expr.ColumnName.LowerInvariantName);
            if (matches.Length + (derivedMatch ? 1 : 0) > 1)
            {
                return ColumnResolution.Error(TableBindingDiagnosticCode.AmbiguousColumn, $"Unqualified column '{expr.ColumnName.Name}' is ambiguous.");
            }
            if (matches.Length == 1) return new ColumnResolution(matches[0]);
            if (derivedMatch) return default;
            if (scope.Physical.Count > 0 || scope.DerivedAliases.Count > 0) break;
        }
        return ColumnResolution.Error(TableBindingDiagnosticCode.UnknownColumn, $"Could not bind column '{expr.ColumnName.Name}'.");
    }

    private static ColumnResolution ResolveInTable(SqTable table, ExprColumn expr)
    {
        var matches = table.Columns.Where(c => ExprNameEqualityComparer.CaseInsensitive.Equals(c.ColumnName, expr.ColumnName)).ToArray();
        return matches.Length == 1
            ? new ColumnResolution(matches[0])
            : ColumnResolution.Error(TableBindingDiagnosticCode.UnknownColumn, $"Could not bind column '{expr.ColumnName.Name}' in table '{Format(table.FullName)}'.");
    }

    private static bool AliasEquals(IExprAlias left, IExprAlias right)
        => left is IExprName leftName && right is IExprName rightName
            ? ExprNameEqualityComparer.CaseInsensitive.Equals(leftName, rightName)
            : left.Equals(right);

    private static string AliasDisplay(IExprAlias alias)
        => alias is IExprName name ? name.Name : alias.ToString() ?? alias.GetType().Name;

    private static bool FullNameMatches(IExprTableFullName requested, IExprTableFullName actual)
    {
        var left = requested.AsExprTableFullName();
        var right = actual.AsExprTableFullName();
        return ExprNameEqualityComparer.CaseInsensitive.Equals(left.TableName, right.TableName)
               && (left.DbSchema?.Schema is not { } leftSchema
                   || right.DbSchema?.Schema is { } rightSchema && ExprNameEqualityComparer.CaseInsensitive.Equals(leftSchema, rightSchema))
               && (left.DbSchema?.Database is not { } leftDatabase
                   || right.DbSchema?.Database is { } rightDatabase && ExprNameEqualityComparer.CaseInsensitive.Equals(leftDatabase, rightDatabase));
    }

    private void ValidateCatalog()
    {
        foreach (var duplicate in this._catalog.GroupBy(t => CatalogKey(t.FullName)).Where(g => g.Count() > 1))
        {
            this.Report(TableBindingDiagnosticCode.DuplicateCatalogTable, $"Duplicate table '{Format(duplicate.First().FullName)}' in binding catalog.");
        }
    }

    private void Report(TableBindingDiagnosticCode code, string message)
    {
        var severity = this._options.ResolveSeverity(code);
        var diagnostic = new TableBindingDiagnostic(code, severity, message);
        (severity == TableBindingSeverity.Warning ? this._warnings : this._errors).Add(diagnostic);
    }

    private static string CatalogKey(IExprTableFullName name)
    {
        var full = name.AsExprTableFullName();
        return $"{full.DbSchema?.Database?.LowerInvariantName}|{full.DbSchema?.Schema.LowerInvariantName}|{full.TableName.LowerInvariantName}";
    }

    private static string Format(IExprTableFullName name)
    {
        var full = name.AsExprTableFullName();
        return string.Join(".", new[] { full.DbSchema?.Database?.Name, full.DbSchema?.Schema.Name, full.TableName.Name }.Where(s => s != null));
    }

    private sealed class Scope
    {
        public Scope(Scope? parent) => this.Parent = parent;
        public Scope? Parent { get; }
        public List<SqTable> Physical { get; } = new List<SqTable>();
        public List<IExprAlias> DerivedAliases { get; } = new List<IExprAlias>();
        public HashSet<string> DerivedColumns { get; } = new HashSet<string>(StringComparer.Ordinal);
    }

    private readonly struct ColumnResolution
    {
        public ColumnResolution(TableColumn column) { this.Column = column; this.DiagnosticCode = null; this.Message = null; }
        private ColumnResolution(TableBindingDiagnosticCode code, string message) { this.Column = null; this.DiagnosticCode = code; this.Message = message; }
        public TableColumn? Column { get; }
        public TableBindingDiagnosticCode? DiagnosticCode { get; }
        public string? Message { get; }
        public static ColumnResolution Error(TableBindingDiagnosticCode code, string message) => new ColumnResolution(code, message);
    }

    private sealed class ReferenceComparer : IEqualityComparer<IExpr>
    {
        public static readonly ReferenceComparer Instance = new ReferenceComparer();
        public bool Equals(IExpr? x, IExpr? y) => ReferenceEquals(x, y);
        public int GetHashCode(IExpr obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
