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
    public TRes Set(TCol col, IExprAssigning value);
}

/// <summary>
/// Defines fluent column assignments for supported CLR literal types.
/// </summary>
/// <remarks>
/// Literal values are converted to typed SqExpress literal nodes. Their final SQL representation and
/// parameterization are controlled by the exporter and database configuration.
/// </remarks>
public interface IUpdateSetterLiteral<out TRes, in TCol>
{
    /// <summary>Assigns a nullable 32-bit integer literal to a column.</summary>
    public TRes Set(TCol col, int? value);
    /// <summary>Assigns a 32-bit integer literal to a column.</summary>
    public TRes Set(TCol col, int value);
    /// <summary>Assigns a string literal to a column.</summary>
    public TRes Set(TCol col, string value);
    /// <summary>Assigns a nullable GUID literal to a column.</summary>
    public TRes Set(TCol col, Guid? value);
    /// <summary>Assigns a GUID literal to a column.</summary>
    public TRes Set(TCol col, Guid value);
    /// <summary>Assigns a nullable date and time literal to a column.</summary>
    public TRes Set(TCol col, DateTime? value);
    /// <summary>Assigns a date and time literal to a column.</summary>
    public TRes Set(TCol col, DateTime value);
    /// <summary>Assigns a nullable date, time, and offset literal to a column.</summary>
    public TRes Set(TCol col, DateTimeOffset? value);
    /// <summary>Assigns a date, time, and offset literal to a column.</summary>
    public TRes Set(TCol col, DateTimeOffset value);
    /// <summary>Assigns a nullable Boolean literal to a column.</summary>
    public TRes Set(TCol col, bool? value);
    /// <summary>Assigns a Boolean literal to a column.</summary>
    public TRes Set(TCol col, bool value);
    /// <summary>Assigns a nullable byte literal to a column.</summary>
    public TRes Set(TCol col, byte? value);
    /// <summary>Assigns a byte literal to a column.</summary>
    public TRes Set(TCol col, byte value);
    /// <summary>Assigns a nullable 16-bit integer literal to a column.</summary>
    public TRes Set(TCol col, short? value);
    /// <summary>Assigns a 16-bit integer literal to a column.</summary>
    public TRes Set(TCol col, short value);
    /// <summary>Assigns a nullable 64-bit integer literal to a column.</summary>
    public TRes Set(TCol col, long? value);
    /// <summary>Assigns a 64-bit integer literal to a column.</summary>
    public TRes Set(TCol col, long value);
    /// <summary>Assigns a nullable decimal literal to a column.</summary>
    public TRes Set(TCol col, decimal? value);
    /// <summary>Assigns a decimal literal to a column.</summary>
    public TRes Set(TCol col, decimal value);
    /// <summary>Assigns a nullable double-precision literal to a column.</summary>
    public TRes Set(TCol col, double? value);
    /// <summary>Assigns a double-precision literal to a column.</summary>
    public TRes Set(TCol col, double value);
}
