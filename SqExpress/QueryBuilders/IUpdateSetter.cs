using System;
using SqExpress.Syntax.Value;

namespace SqExpress.QueryBuilders;

/// <summary>
/// Defines a fluent assignment step that accepts either SqExpress assignment expressions or CLR literal values.
/// </summary>
/// <typeparam name="TRes">The next fluent-builder stage returned after an assignment.</typeparam>
/// <typeparam name="TCol">The column-reference type accepted by the builder.</typeparam>
public interface IUpdateSetter<out TRes, in TCol> : IUpdateSetterLiteral<TRes, TCol>
{
    /// <summary>
    /// Assigns an expression, column, parameter, or SQL <c>DEFAULT</c> value to a column.
    /// </summary>
    /// <param name="col">The target column being assigned.</param>
    /// <param name="value">The SQL expression evaluated for the new column value.</param>
    /// <returns>The next fluent stage, retaining this and any earlier assignments.</returns>
    public TRes Set(TCol col, IExprAssigning value);
}

/// <summary>
/// Defines fluent column assignments for supported CLR literal types.
/// </summary>
/// <remarks>
/// Literal values are converted to typed SqExpress literal nodes. Their final SQL representation and
/// parameterization are controlled by the exporter and database configuration.
/// </remarks>
/// <typeparam name="TRes">The next fluent-builder stage returned after an assignment.</typeparam>
/// <typeparam name="TCol">The target column-reference type accepted by the builder.</typeparam>
public interface IUpdateSetterLiteral<out TRes, in TCol>
{
    /// <summary>Assigns a nullable 32-bit integer as a typed SQL value, preserving <see langword="null"/> as SQL <c>NULL</c>.</summary>
    /// <param name="col">The target column.</param>
    /// <param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, int? value);
    /// <summary>Assigns a 32-bit integer as a typed SQL value.</summary>
    /// <param name="col">The target column.</param><param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, int value);
    /// <summary>Assigns text through a typed value node that can be safely escaped or parameterized.</summary>
    /// <param name="col">The target column.</param><param name="value">The text to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, string value);
    /// <summary>Assigns a nullable GUID using the target dialect's compatible representation.</summary>
    /// <param name="col">The target column.</param><param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, Guid? value);
    /// <summary>Assigns a GUID using the target dialect's compatible representation.</summary>
    /// <param name="col">The target column.</param><param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, Guid value);
    /// <summary>Assigns a nullable date/time through a typed temporal value node.</summary>
    /// <param name="col">The target column.</param><param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, DateTime? value);
    /// <summary>Assigns a date/time through a typed temporal value node.</summary>
    /// <param name="col">The target column.</param><param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, DateTime value);
    /// <summary>Assigns a nullable offset-aware date/time using a dialect-compatible value representation.</summary>
    /// <param name="col">The target column.</param><param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, DateTimeOffset? value);
    /// <summary>Assigns an offset-aware date/time using a dialect-compatible value representation.</summary>
    /// <param name="col">The target column.</param><param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, DateTimeOffset value);
    /// <summary>Assigns a nullable Boolean using the selected database's Boolean-compatible form.</summary>
    /// <param name="col">The target column.</param><param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, bool? value);
    /// <summary>Assigns a Boolean using the selected database's Boolean-compatible form.</summary>
    /// <param name="col">The target column.</param><param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, bool value);
    /// <summary>Assigns a nullable byte using the target dialect's compatible numeric form.</summary>
    /// <param name="col">The target column.</param><param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, byte? value);
    /// <summary>Assigns a byte using the target dialect's compatible numeric form.</summary>
    /// <param name="col">The target column.</param><param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, byte value);
    /// <summary>Assigns a nullable 16-bit integer as a typed SQL value.</summary>
    /// <param name="col">The target column.</param><param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, short? value);
    /// <summary>Assigns a 16-bit integer as a typed SQL value.</summary>
    /// <param name="col">The target column.</param><param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, short value);
    /// <summary>Assigns a nullable 64-bit integer as a typed SQL value.</summary>
    /// <param name="col">The target column.</param><param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, long? value);
    /// <summary>Assigns a 64-bit integer as a typed SQL value.</summary>
    /// <param name="col">The target column.</param><param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, long value);
    /// <summary>Assigns a nullable decimal as an exact numeric SQL value.</summary>
    /// <param name="col">The target column.</param><param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, decimal? value);
    /// <summary>Assigns a decimal as an exact numeric SQL value.</summary>
    /// <param name="col">The target column.</param><param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, decimal value);
    /// <summary>Assigns a nullable double as an approximate numeric SQL value.</summary>
    /// <param name="col">The target column.</param><param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, double? value);
    /// <summary>Assigns a double as an approximate numeric SQL value.</summary>
    /// <param name="col">The target column.</param><param name="value">The value to assign.</param>
    /// <returns>The next fluent stage with this assignment appended.</returns>
    public TRes Set(TCol col, double value);
}
