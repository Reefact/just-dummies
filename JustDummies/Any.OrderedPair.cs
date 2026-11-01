namespace JustDummies;

public static partial class Any {

    /// <summary>
    ///     Draws two values tied by an order — a span's start and end, a range's minimum and maximum — with the first
    ///     at or below the second, or strictly below it when <paramref name="strict" /> is set. By construction, never
    ///     by redraw: the first is drawn from <paramref name="first" /> kept at or below the largest value
    ///     <paramref name="second" /> can produce, then the second from <paramref name="second" /> kept at or above it.
    ///     <see cref="AnyOrderedPair{T}.WithFirst" /> and <see cref="AnyOrderedPair{T}.WithSecond" /> pin one side and
    ///     bound the other the same way, so pinning the end constrains the start (ADR-0126).
    /// </summary>
    /// <remarks>
    ///     <c>TimeOnly</c> has no pair: its order is cyclic once a window crosses midnight (ADR-0125), so neither
    ///     reading of "first before second" is the right one everywhere. Three ordered values are a pair and a
    ///     <c>Then</c> drawing the middle one between them.
    /// </remarks>
    /// <param name="first">The base the first value is drawn from.</param>
    /// <param name="second">The base the second value is drawn from.</param>
    /// <param name="strict">Whether the first must be strictly below the second.</param>
    /// <returns>A generator of ordered pairs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no first value precedes the largest second.</exception>
    public static AnyOrderedPair<int> OrderedPair(AnyInt32 first, AnyInt32 second, bool strict = false) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }

        OrderedPairBounds<int> bounds = new(value => second.GreaterThanOrEqualTo(value), value => second.GreaterThan(value), value => first.LessThanOrEqualTo(value), value => first.LessThan(value), second.Largest());

        return AnyOrderedPair<int>.Create(first, second, strict, bounds);
    }

    /// <summary>The <c>byte</c> form of <see cref="OrderedPair(AnyInt32, AnyInt32, bool)" />, with the same contract.</summary>
    /// <param name="first">The base the first value is drawn from.</param>
    /// <param name="second">The base the second value is drawn from.</param>
    /// <param name="strict">Whether the first must be strictly below the second.</param>
    /// <returns>A generator of ordered pairs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no first value precedes the largest second.</exception>
    public static AnyOrderedPair<byte> OrderedPair(AnyByte first, AnyByte second, bool strict = false) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }

        OrderedPairBounds<byte> bounds = new(value => second.GreaterThanOrEqualTo(value), value => second.GreaterThan(value), value => first.LessThanOrEqualTo(value), value => first.LessThan(value), second.Largest());

        return AnyOrderedPair<byte>.Create(first, second, strict, bounds);
    }

    /// <summary>The <c>sbyte</c> form of <see cref="OrderedPair(AnyInt32, AnyInt32, bool)" />, with the same contract.</summary>
    /// <param name="first">The base the first value is drawn from.</param>
    /// <param name="second">The base the second value is drawn from.</param>
    /// <param name="strict">Whether the first must be strictly below the second.</param>
    /// <returns>A generator of ordered pairs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no first value precedes the largest second.</exception>
    public static AnyOrderedPair<sbyte> OrderedPair(AnySByte first, AnySByte second, bool strict = false) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }

        OrderedPairBounds<sbyte> bounds = new(value => second.GreaterThanOrEqualTo(value), value => second.GreaterThan(value), value => first.LessThanOrEqualTo(value), value => first.LessThan(value), second.Largest());

        return AnyOrderedPair<sbyte>.Create(first, second, strict, bounds);
    }

    /// <summary>The <c>short</c> form of <see cref="OrderedPair(AnyInt32, AnyInt32, bool)" />, with the same contract.</summary>
    /// <param name="first">The base the first value is drawn from.</param>
    /// <param name="second">The base the second value is drawn from.</param>
    /// <param name="strict">Whether the first must be strictly below the second.</param>
    /// <returns>A generator of ordered pairs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no first value precedes the largest second.</exception>
    public static AnyOrderedPair<short> OrderedPair(AnyInt16 first, AnyInt16 second, bool strict = false) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }

        OrderedPairBounds<short> bounds = new(value => second.GreaterThanOrEqualTo(value), value => second.GreaterThan(value), value => first.LessThanOrEqualTo(value), value => first.LessThan(value), second.Largest());

        return AnyOrderedPair<short>.Create(first, second, strict, bounds);
    }

    /// <summary>The <c>ushort</c> form of <see cref="OrderedPair(AnyInt32, AnyInt32, bool)" />, with the same contract.</summary>
    /// <param name="first">The base the first value is drawn from.</param>
    /// <param name="second">The base the second value is drawn from.</param>
    /// <param name="strict">Whether the first must be strictly below the second.</param>
    /// <returns>A generator of ordered pairs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no first value precedes the largest second.</exception>
    public static AnyOrderedPair<ushort> OrderedPair(AnyUInt16 first, AnyUInt16 second, bool strict = false) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }

        OrderedPairBounds<ushort> bounds = new(value => second.GreaterThanOrEqualTo(value), value => second.GreaterThan(value), value => first.LessThanOrEqualTo(value), value => first.LessThan(value), second.Largest());

        return AnyOrderedPair<ushort>.Create(first, second, strict, bounds);
    }

    /// <summary>The <c>uint</c> form of <see cref="OrderedPair(AnyInt32, AnyInt32, bool)" />, with the same contract.</summary>
    /// <param name="first">The base the first value is drawn from.</param>
    /// <param name="second">The base the second value is drawn from.</param>
    /// <param name="strict">Whether the first must be strictly below the second.</param>
    /// <returns>A generator of ordered pairs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no first value precedes the largest second.</exception>
    public static AnyOrderedPair<uint> OrderedPair(AnyUInt32 first, AnyUInt32 second, bool strict = false) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }

        OrderedPairBounds<uint> bounds = new(value => second.GreaterThanOrEqualTo(value), value => second.GreaterThan(value), value => first.LessThanOrEqualTo(value), value => first.LessThan(value), second.Largest());

        return AnyOrderedPair<uint>.Create(first, second, strict, bounds);
    }

    /// <summary>The <c>long</c> form of <see cref="OrderedPair(AnyInt32, AnyInt32, bool)" />, with the same contract.</summary>
    /// <param name="first">The base the first value is drawn from.</param>
    /// <param name="second">The base the second value is drawn from.</param>
    /// <param name="strict">Whether the first must be strictly below the second.</param>
    /// <returns>A generator of ordered pairs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no first value precedes the largest second.</exception>
    public static AnyOrderedPair<long> OrderedPair(AnyInt64 first, AnyInt64 second, bool strict = false) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }

        OrderedPairBounds<long> bounds = new(value => second.GreaterThanOrEqualTo(value), value => second.GreaterThan(value), value => first.LessThanOrEqualTo(value), value => first.LessThan(value), second.Largest());

        return AnyOrderedPair<long>.Create(first, second, strict, bounds);
    }

    /// <summary>The <c>ulong</c> form of <see cref="OrderedPair(AnyInt32, AnyInt32, bool)" />, with the same contract.</summary>
    /// <param name="first">The base the first value is drawn from.</param>
    /// <param name="second">The base the second value is drawn from.</param>
    /// <param name="strict">Whether the first must be strictly below the second.</param>
    /// <returns>A generator of ordered pairs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no first value precedes the largest second.</exception>
    public static AnyOrderedPair<ulong> OrderedPair(AnyUInt64 first, AnyUInt64 second, bool strict = false) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }

        OrderedPairBounds<ulong> bounds = new(value => second.GreaterThanOrEqualTo(value), value => second.GreaterThan(value), value => first.LessThanOrEqualTo(value), value => first.LessThan(value), second.Largest());

        return AnyOrderedPair<ulong>.Create(first, second, strict, bounds);
    }

    /// <summary>The <c>float</c> form of <see cref="OrderedPair(AnyInt32, AnyInt32, bool)" />, with the same contract.</summary>
    /// <param name="first">The base the first value is drawn from.</param>
    /// <param name="second">The base the second value is drawn from.</param>
    /// <param name="strict">Whether the first must be strictly below the second.</param>
    /// <returns>A generator of ordered pairs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no first value precedes the largest second.</exception>
    public static AnyOrderedPair<float> OrderedPair(AnySingle first, AnySingle second, bool strict = false) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }

        OrderedPairBounds<float> bounds = new(value => second.GreaterThanOrEqualTo(value), value => second.GreaterThan(value), value => first.LessThanOrEqualTo(value), value => first.LessThan(value), second.Largest());

        return AnyOrderedPair<float>.Create(first, second, strict, bounds);
    }

    /// <summary>The <c>double</c> form of <see cref="OrderedPair(AnyInt32, AnyInt32, bool)" />, with the same contract.</summary>
    /// <param name="first">The base the first value is drawn from.</param>
    /// <param name="second">The base the second value is drawn from.</param>
    /// <param name="strict">Whether the first must be strictly below the second.</param>
    /// <returns>A generator of ordered pairs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no first value precedes the largest second.</exception>
    public static AnyOrderedPair<double> OrderedPair(AnyDouble first, AnyDouble second, bool strict = false) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }

        OrderedPairBounds<double> bounds = new(value => second.GreaterThanOrEqualTo(value), value => second.GreaterThan(value), value => first.LessThanOrEqualTo(value), value => first.LessThan(value), second.Largest());

        return AnyOrderedPair<double>.Create(first, second, strict, bounds);
    }

    /// <summary>The <c>decimal</c> form of <see cref="OrderedPair(AnyInt32, AnyInt32, bool)" />, with the same contract.</summary>
    /// <param name="first">The base the first value is drawn from.</param>
    /// <param name="second">The base the second value is drawn from.</param>
    /// <param name="strict">Whether the first must be strictly below the second.</param>
    /// <returns>A generator of ordered pairs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no first value precedes the largest second.</exception>
    public static AnyOrderedPair<decimal> OrderedPair(AnyDecimal first, AnyDecimal second, bool strict = false) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }

        OrderedPairBounds<decimal> bounds = new(value => second.GreaterThanOrEqualTo(value), value => second.GreaterThan(value), value => first.LessThanOrEqualTo(value), value => first.LessThan(value), second.Largest());

        return AnyOrderedPair<decimal>.Create(first, second, strict, bounds);
    }

    /// <summary>The <c>TimeSpan</c> form of <see cref="OrderedPair(AnyInt32, AnyInt32, bool)" />, with the same contract.</summary>
    /// <param name="first">The base the first value is drawn from.</param>
    /// <param name="second">The base the second value is drawn from.</param>
    /// <param name="strict">Whether the first must be strictly below the second.</param>
    /// <returns>A generator of ordered pairs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no first value precedes the largest second.</exception>
    public static AnyOrderedPair<TimeSpan> OrderedPair(AnyTimeSpan first, AnyTimeSpan second, bool strict = false) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }

        OrderedPairBounds<TimeSpan> bounds = new(value => second.GreaterThanOrEqualTo(value), value => second.GreaterThan(value), value => first.LessThanOrEqualTo(value), value => first.LessThan(value), second.Largest());

        return AnyOrderedPair<TimeSpan>.Create(first, second, strict, bounds);
    }

    /// <summary>The <c>DateTime</c> form of <see cref="OrderedPair(AnyInt32, AnyInt32, bool)" />, with the same contract.</summary>
    /// <param name="first">The base the first value is drawn from.</param>
    /// <param name="second">The base the second value is drawn from.</param>
    /// <param name="strict">Whether the first must be strictly below the second.</param>
    /// <returns>A generator of ordered pairs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no first value precedes the largest second.</exception>
    public static AnyOrderedPair<DateTime> OrderedPair(AnyDateTime first, AnyDateTime second, bool strict = false) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }

        OrderedPairBounds<DateTime> bounds = new(value => second.AfterOrEqualTo(value), value => second.After(value), value => first.BeforeOrEqualTo(value), value => first.Before(value), second.Largest());

        return AnyOrderedPair<DateTime>.Create(first, second, strict, bounds);
    }

    /// <summary>The <c>DateTimeOffset</c> form of <see cref="OrderedPair(AnyInt32, AnyInt32, bool)" />, with the same contract.</summary>
    /// <param name="first">The base the first value is drawn from.</param>
    /// <param name="second">The base the second value is drawn from.</param>
    /// <param name="strict">Whether the first must be strictly below the second.</param>
    /// <returns>A generator of ordered pairs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no first value precedes the largest second.</exception>
    public static AnyOrderedPair<DateTimeOffset> OrderedPair(AnyDateTimeOffset first, AnyDateTimeOffset second, bool strict = false) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }

        OrderedPairBounds<DateTimeOffset> bounds = new(value => second.AfterOrEqualTo(value), value => second.After(value), value => first.BeforeOrEqualTo(value), value => first.Before(value), second.Largest());

        return AnyOrderedPair<DateTimeOffset>.Create(first, second, strict, bounds);
    }

#if NET8_0_OR_GREATER
    /// <summary>The <c>Int128</c> form of <see cref="OrderedPair(AnyInt32, AnyInt32, bool)" />, with the same contract.</summary>
    /// <param name="first">The base the first value is drawn from.</param>
    /// <param name="second">The base the second value is drawn from.</param>
    /// <param name="strict">Whether the first must be strictly below the second.</param>
    /// <returns>A generator of ordered pairs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no first value precedes the largest second.</exception>
    public static AnyOrderedPair<Int128> OrderedPair(AnyInt128 first, AnyInt128 second, bool strict = false) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }

        OrderedPairBounds<Int128> bounds = new(value => second.GreaterThanOrEqualTo(value), value => second.GreaterThan(value), value => first.LessThanOrEqualTo(value), value => first.LessThan(value), second.Largest());

        return AnyOrderedPair<Int128>.Create(first, second, strict, bounds);
    }
#endif

#if NET8_0_OR_GREATER
    /// <summary>The <c>UInt128</c> form of <see cref="OrderedPair(AnyInt32, AnyInt32, bool)" />, with the same contract.</summary>
    /// <param name="first">The base the first value is drawn from.</param>
    /// <param name="second">The base the second value is drawn from.</param>
    /// <param name="strict">Whether the first must be strictly below the second.</param>
    /// <returns>A generator of ordered pairs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no first value precedes the largest second.</exception>
    public static AnyOrderedPair<UInt128> OrderedPair(AnyUInt128 first, AnyUInt128 second, bool strict = false) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }

        OrderedPairBounds<UInt128> bounds = new(value => second.GreaterThanOrEqualTo(value), value => second.GreaterThan(value), value => first.LessThanOrEqualTo(value), value => first.LessThan(value), second.Largest());

        return AnyOrderedPair<UInt128>.Create(first, second, strict, bounds);
    }
#endif

#if NET8_0_OR_GREATER
    /// <summary>The <c>Half</c> form of <see cref="OrderedPair(AnyInt32, AnyInt32, bool)" />, with the same contract.</summary>
    /// <param name="first">The base the first value is drawn from.</param>
    /// <param name="second">The base the second value is drawn from.</param>
    /// <param name="strict">Whether the first must be strictly below the second.</param>
    /// <returns>A generator of ordered pairs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no first value precedes the largest second.</exception>
    public static AnyOrderedPair<Half> OrderedPair(AnyHalf first, AnyHalf second, bool strict = false) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }

        OrderedPairBounds<Half> bounds = new(value => second.GreaterThanOrEqualTo(value), value => second.GreaterThan(value), value => first.LessThanOrEqualTo(value), value => first.LessThan(value), second.Largest());

        return AnyOrderedPair<Half>.Create(first, second, strict, bounds);
    }
#endif

#if NET8_0_OR_GREATER
    /// <summary>The <c>DateOnly</c> form of <see cref="OrderedPair(AnyInt32, AnyInt32, bool)" />, with the same contract.</summary>
    /// <param name="first">The base the first value is drawn from.</param>
    /// <param name="second">The base the second value is drawn from.</param>
    /// <param name="strict">Whether the first must be strictly below the second.</param>
    /// <returns>A generator of ordered pairs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no first value precedes the largest second.</exception>
    public static AnyOrderedPair<DateOnly> OrderedPair(AnyDateOnly first, AnyDateOnly second, bool strict = false) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }

        OrderedPairBounds<DateOnly> bounds = new(value => second.AfterOrEqualTo(value), value => second.After(value), value => first.BeforeOrEqualTo(value), value => first.Before(value), second.Largest());

        return AnyOrderedPair<DateOnly>.Create(first, second, strict, bounds);
    }
#endif

}
