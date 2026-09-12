using System;
using SqExpress.Internal;

namespace SqExpress;

/// <summary>Represents a validated portable SQL/JSON path.</summary>
public readonly struct SqJsonPath : IEquatable<SqJsonPath>
{
    internal SqJsonPath(string value)
    {
        SqJsonPathParser.Parse(value);
        this.Value = value;
    }

    /// <summary>Gets the original JSON path text.</summary>
    public string Value { get; }

    /// <summary>Creates a JSON path from its textual representation.</summary>
    /// <param name="value">The portable JSON path text.</param>
    /// <returns>A validated JSON path.</returns>
    public static implicit operator SqJsonPath(string value) => new(value);

    /// <summary>Extracts the original path text.</summary>
    /// <param name="path">The JSON path.</param>
    /// <returns>The original path text.</returns>
    public static explicit operator string(SqJsonPath path) => path.Value;

    /// <inheritdoc />
    public bool Equals(SqJsonPath other) => string.Equals(this.Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SqJsonPath other && this.Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => this.Value == null ? 0 : StringComparer.Ordinal.GetHashCode(this.Value);

    /// <inheritdoc />
    public override string ToString() => this.Value ?? string.Empty;
}
