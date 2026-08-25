// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.Pagination.Abstractions;

/// <summary>
/// Represents an unencoded cursor value for forward or backward pagination.
/// </summary>
public readonly struct RawCursorValue : System.IEquatable<RawCursorValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RawCursorValue"/> struct with the specified raw cursor string.
    /// </summary>
    /// <param name="value">The raw cursor value string.</param>
    public RawCursorValue(string value) => Value = value;

    /// <summary>
    /// Gets the raw cursor value string.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public bool Equals(RawCursorValue other) => Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is RawCursorValue other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Value != null ? System.StringComparer.Ordinal.GetHashCode(Value) : 0;

    /// <summary>
    /// Determines whether two <see cref="RawCursorValue"/> instances are equal.
    /// </summary>
    /// <param name="left">The first instance to compare.</param>
    /// <param name="right">The second instance to compare.</param>
    /// <returns><see langword="true"/> if both instances are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(RawCursorValue left, RawCursorValue right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="RawCursorValue"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first instance to compare.</param>
    /// <param name="right">The second instance to compare.</param>
    /// <returns><see langword="true"/> if the instances are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(RawCursorValue left, RawCursorValue right) => !left.Equals(right);
}

