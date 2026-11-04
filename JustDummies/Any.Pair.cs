namespace JustDummies;

public static partial class Any {

    /// <summary>
    ///     Draws two values of one ordered type, each from a base of its own — a span's start and end, a range's
    ///     minimum and maximum. Declared alone, the two are drawn independently; <see cref="AnyPair{T}.Having" />
    ///     declares the order between them, which is then held by construction rather than by redraw (ADR-0131).
    /// </summary>
    /// <remarks>
    ///     A side the test fixes needs no pair: keep it a literal and bound the other side on its own builder.
    ///     <c>TimeOnly</c> has no pair: its order is cyclic once a window crosses midnight (ADR-0125).
    /// </remarks>
    /// <param name="item1">The base the first item is drawn from.</param>
    /// <param name="item2">The base the second item is drawn from.</param>
    /// <returns>A generator of pairs, declaring no relation yet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    public static AnyPair<int> Pair(AnyInt32 item1, AnyInt32 item2) {
        if (item1 is null) { throw new ArgumentNullException(nameof(item1)); }
        if (item2 is null) { throw new ArgumentNullException(nameof(item2)); }

        return AnyPair<int>.Create(PairSideOf(item1), PairSideOf(item2));
    }

    /// <summary>The <c>byte</c> form of <see cref="Pair(AnyInt32, AnyInt32)" />, with the same contract.</summary>
    /// <param name="item1">The base the first item is drawn from.</param>
    /// <param name="item2">The base the second item is drawn from.</param>
    /// <returns>A generator of pairs, declaring no relation yet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    public static AnyPair<byte> Pair(AnyByte item1, AnyByte item2) {
        if (item1 is null) { throw new ArgumentNullException(nameof(item1)); }
        if (item2 is null) { throw new ArgumentNullException(nameof(item2)); }

        return AnyPair<byte>.Create(PairSideOf(item1), PairSideOf(item2));
    }

    /// <summary>The <c>sbyte</c> form of <see cref="Pair(AnyInt32, AnyInt32)" />, with the same contract.</summary>
    /// <param name="item1">The base the first item is drawn from.</param>
    /// <param name="item2">The base the second item is drawn from.</param>
    /// <returns>A generator of pairs, declaring no relation yet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    public static AnyPair<sbyte> Pair(AnySByte item1, AnySByte item2) {
        if (item1 is null) { throw new ArgumentNullException(nameof(item1)); }
        if (item2 is null) { throw new ArgumentNullException(nameof(item2)); }

        return AnyPair<sbyte>.Create(PairSideOf(item1), PairSideOf(item2));
    }

    /// <summary>The <c>short</c> form of <see cref="Pair(AnyInt32, AnyInt32)" />, with the same contract.</summary>
    /// <param name="item1">The base the first item is drawn from.</param>
    /// <param name="item2">The base the second item is drawn from.</param>
    /// <returns>A generator of pairs, declaring no relation yet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    public static AnyPair<short> Pair(AnyInt16 item1, AnyInt16 item2) {
        if (item1 is null) { throw new ArgumentNullException(nameof(item1)); }
        if (item2 is null) { throw new ArgumentNullException(nameof(item2)); }

        return AnyPair<short>.Create(PairSideOf(item1), PairSideOf(item2));
    }

    /// <summary>The <c>ushort</c> form of <see cref="Pair(AnyInt32, AnyInt32)" />, with the same contract.</summary>
    /// <param name="item1">The base the first item is drawn from.</param>
    /// <param name="item2">The base the second item is drawn from.</param>
    /// <returns>A generator of pairs, declaring no relation yet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    public static AnyPair<ushort> Pair(AnyUInt16 item1, AnyUInt16 item2) {
        if (item1 is null) { throw new ArgumentNullException(nameof(item1)); }
        if (item2 is null) { throw new ArgumentNullException(nameof(item2)); }

        return AnyPair<ushort>.Create(PairSideOf(item1), PairSideOf(item2));
    }

    /// <summary>The <c>uint</c> form of <see cref="Pair(AnyInt32, AnyInt32)" />, with the same contract.</summary>
    /// <param name="item1">The base the first item is drawn from.</param>
    /// <param name="item2">The base the second item is drawn from.</param>
    /// <returns>A generator of pairs, declaring no relation yet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    public static AnyPair<uint> Pair(AnyUInt32 item1, AnyUInt32 item2) {
        if (item1 is null) { throw new ArgumentNullException(nameof(item1)); }
        if (item2 is null) { throw new ArgumentNullException(nameof(item2)); }

        return AnyPair<uint>.Create(PairSideOf(item1), PairSideOf(item2));
    }

    /// <summary>The <c>long</c> form of <see cref="Pair(AnyInt32, AnyInt32)" />, with the same contract.</summary>
    /// <param name="item1">The base the first item is drawn from.</param>
    /// <param name="item2">The base the second item is drawn from.</param>
    /// <returns>A generator of pairs, declaring no relation yet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    public static AnyPair<long> Pair(AnyInt64 item1, AnyInt64 item2) {
        if (item1 is null) { throw new ArgumentNullException(nameof(item1)); }
        if (item2 is null) { throw new ArgumentNullException(nameof(item2)); }

        return AnyPair<long>.Create(PairSideOf(item1), PairSideOf(item2));
    }

    /// <summary>The <c>ulong</c> form of <see cref="Pair(AnyInt32, AnyInt32)" />, with the same contract.</summary>
    /// <param name="item1">The base the first item is drawn from.</param>
    /// <param name="item2">The base the second item is drawn from.</param>
    /// <returns>A generator of pairs, declaring no relation yet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    public static AnyPair<ulong> Pair(AnyUInt64 item1, AnyUInt64 item2) {
        if (item1 is null) { throw new ArgumentNullException(nameof(item1)); }
        if (item2 is null) { throw new ArgumentNullException(nameof(item2)); }

        return AnyPair<ulong>.Create(PairSideOf(item1), PairSideOf(item2));
    }

    /// <summary>The <c>float</c> form of <see cref="Pair(AnyInt32, AnyInt32)" />, with the same contract.</summary>
    /// <param name="item1">The base the first item is drawn from.</param>
    /// <param name="item2">The base the second item is drawn from.</param>
    /// <returns>A generator of pairs, declaring no relation yet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    public static AnyPair<float> Pair(AnySingle item1, AnySingle item2) {
        if (item1 is null) { throw new ArgumentNullException(nameof(item1)); }
        if (item2 is null) { throw new ArgumentNullException(nameof(item2)); }

        return AnyPair<float>.Create(PairSideOf(item1), PairSideOf(item2));
    }

    /// <summary>The <c>double</c> form of <see cref="Pair(AnyInt32, AnyInt32)" />, with the same contract.</summary>
    /// <param name="item1">The base the first item is drawn from.</param>
    /// <param name="item2">The base the second item is drawn from.</param>
    /// <returns>A generator of pairs, declaring no relation yet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    public static AnyPair<double> Pair(AnyDouble item1, AnyDouble item2) {
        if (item1 is null) { throw new ArgumentNullException(nameof(item1)); }
        if (item2 is null) { throw new ArgumentNullException(nameof(item2)); }

        return AnyPair<double>.Create(PairSideOf(item1), PairSideOf(item2));
    }

    /// <summary>The <c>decimal</c> form of <see cref="Pair(AnyInt32, AnyInt32)" />, with the same contract.</summary>
    /// <param name="item1">The base the first item is drawn from.</param>
    /// <param name="item2">The base the second item is drawn from.</param>
    /// <returns>A generator of pairs, declaring no relation yet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    public static AnyPair<decimal> Pair(AnyDecimal item1, AnyDecimal item2) {
        if (item1 is null) { throw new ArgumentNullException(nameof(item1)); }
        if (item2 is null) { throw new ArgumentNullException(nameof(item2)); }

        return AnyPair<decimal>.Create(PairSideOf(item1), PairSideOf(item2));
    }

    /// <summary>The <c>TimeSpan</c> form of <see cref="Pair(AnyInt32, AnyInt32)" />, with the same contract.</summary>
    /// <param name="item1">The base the first item is drawn from.</param>
    /// <param name="item2">The base the second item is drawn from.</param>
    /// <returns>A generator of pairs, declaring no relation yet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    public static AnyPair<TimeSpan> Pair(AnyTimeSpan item1, AnyTimeSpan item2) {
        if (item1 is null) { throw new ArgumentNullException(nameof(item1)); }
        if (item2 is null) { throw new ArgumentNullException(nameof(item2)); }

        return AnyPair<TimeSpan>.Create(PairSideOf(item1), PairSideOf(item2));
    }

    /// <summary>The <c>DateTime</c> form of <see cref="Pair(AnyInt32, AnyInt32)" />, with the same contract.</summary>
    /// <param name="item1">The base the first item is drawn from.</param>
    /// <param name="item2">The base the second item is drawn from.</param>
    /// <returns>A generator of pairs, declaring no relation yet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    public static AnyPair<DateTime> Pair(AnyDateTime item1, AnyDateTime item2) {
        if (item1 is null) { throw new ArgumentNullException(nameof(item1)); }
        if (item2 is null) { throw new ArgumentNullException(nameof(item2)); }

        return AnyPair<DateTime>.Create(PairSideOf(item1), PairSideOf(item2));
    }

    /// <summary>The <c>DateTimeOffset</c> form of <see cref="Pair(AnyInt32, AnyInt32)" />, with the same contract.</summary>
    /// <param name="item1">The base the first item is drawn from.</param>
    /// <param name="item2">The base the second item is drawn from.</param>
    /// <returns>A generator of pairs, declaring no relation yet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    public static AnyPair<DateTimeOffset> Pair(AnyDateTimeOffset item1, AnyDateTimeOffset item2) {
        if (item1 is null) { throw new ArgumentNullException(nameof(item1)); }
        if (item2 is null) { throw new ArgumentNullException(nameof(item2)); }

        return AnyPair<DateTimeOffset>.Create(PairSideOf(item1), PairSideOf(item2));
    }

#if NET8_0_OR_GREATER
    /// <summary>The <c>Int128</c> form of <see cref="Pair(AnyInt32, AnyInt32)" />, with the same contract.</summary>
    /// <param name="item1">The base the first item is drawn from.</param>
    /// <param name="item2">The base the second item is drawn from.</param>
    /// <returns>A generator of pairs, declaring no relation yet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    public static AnyPair<Int128> Pair(AnyInt128 item1, AnyInt128 item2) {
        if (item1 is null) { throw new ArgumentNullException(nameof(item1)); }
        if (item2 is null) { throw new ArgumentNullException(nameof(item2)); }

        return AnyPair<Int128>.Create(PairSideOf(item1), PairSideOf(item2));
    }

#endif

#if NET8_0_OR_GREATER
    /// <summary>The <c>UInt128</c> form of <see cref="Pair(AnyInt32, AnyInt32)" />, with the same contract.</summary>
    /// <param name="item1">The base the first item is drawn from.</param>
    /// <param name="item2">The base the second item is drawn from.</param>
    /// <returns>A generator of pairs, declaring no relation yet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    public static AnyPair<UInt128> Pair(AnyUInt128 item1, AnyUInt128 item2) {
        if (item1 is null) { throw new ArgumentNullException(nameof(item1)); }
        if (item2 is null) { throw new ArgumentNullException(nameof(item2)); }

        return AnyPair<UInt128>.Create(PairSideOf(item1), PairSideOf(item2));
    }

#endif

#if NET8_0_OR_GREATER
    /// <summary>The <c>Half</c> form of <see cref="Pair(AnyInt32, AnyInt32)" />, with the same contract.</summary>
    /// <param name="item1">The base the first item is drawn from.</param>
    /// <param name="item2">The base the second item is drawn from.</param>
    /// <returns>A generator of pairs, declaring no relation yet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    public static AnyPair<Half> Pair(AnyHalf item1, AnyHalf item2) {
        if (item1 is null) { throw new ArgumentNullException(nameof(item1)); }
        if (item2 is null) { throw new ArgumentNullException(nameof(item2)); }

        return AnyPair<Half>.Create(PairSideOf(item1), PairSideOf(item2));
    }

#endif

#if NET8_0_OR_GREATER
    /// <summary>The <c>DateOnly</c> form of <see cref="Pair(AnyInt32, AnyInt32)" />, with the same contract.</summary>
    /// <param name="item1">The base the first item is drawn from.</param>
    /// <param name="item2">The base the second item is drawn from.</param>
    /// <returns>A generator of pairs, declaring no relation yet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a base is <c>null</c>.</exception>
    public static AnyPair<DateOnly> Pair(AnyDateOnly item1, AnyDateOnly item2) {
        if (item1 is null) { throw new ArgumentNullException(nameof(item1)); }
        if (item2 is null) { throw new ArgumentNullException(nameof(item2)); }

        return AnyPair<DateOnly>.Create(PairSideOf(item1), PairSideOf(item2));
    }

#endif

    // One side of a pair over a builder: its four bounds in the builder's own vocabulary, and its largest value.

    private static PairSide<int> PairSideOf(AnyInt32 @base) {
        return new PairSide<int>(@base, value => @base.GreaterThanOrEqualTo(value), value => @base.GreaterThan(value), value => @base.LessThanOrEqualTo(value), value => @base.LessThan(value), @base.Largest);
    }

    private static PairSide<byte> PairSideOf(AnyByte @base) {
        return new PairSide<byte>(@base, value => @base.GreaterThanOrEqualTo(value), value => @base.GreaterThan(value), value => @base.LessThanOrEqualTo(value), value => @base.LessThan(value), @base.Largest);
    }

    private static PairSide<sbyte> PairSideOf(AnySByte @base) {
        return new PairSide<sbyte>(@base, value => @base.GreaterThanOrEqualTo(value), value => @base.GreaterThan(value), value => @base.LessThanOrEqualTo(value), value => @base.LessThan(value), @base.Largest);
    }

    private static PairSide<short> PairSideOf(AnyInt16 @base) {
        return new PairSide<short>(@base, value => @base.GreaterThanOrEqualTo(value), value => @base.GreaterThan(value), value => @base.LessThanOrEqualTo(value), value => @base.LessThan(value), @base.Largest);
    }

    private static PairSide<ushort> PairSideOf(AnyUInt16 @base) {
        return new PairSide<ushort>(@base, value => @base.GreaterThanOrEqualTo(value), value => @base.GreaterThan(value), value => @base.LessThanOrEqualTo(value), value => @base.LessThan(value), @base.Largest);
    }

    private static PairSide<uint> PairSideOf(AnyUInt32 @base) {
        return new PairSide<uint>(@base, value => @base.GreaterThanOrEqualTo(value), value => @base.GreaterThan(value), value => @base.LessThanOrEqualTo(value), value => @base.LessThan(value), @base.Largest);
    }

    private static PairSide<long> PairSideOf(AnyInt64 @base) {
        return new PairSide<long>(@base, value => @base.GreaterThanOrEqualTo(value), value => @base.GreaterThan(value), value => @base.LessThanOrEqualTo(value), value => @base.LessThan(value), @base.Largest);
    }

    private static PairSide<ulong> PairSideOf(AnyUInt64 @base) {
        return new PairSide<ulong>(@base, value => @base.GreaterThanOrEqualTo(value), value => @base.GreaterThan(value), value => @base.LessThanOrEqualTo(value), value => @base.LessThan(value), @base.Largest);
    }

    private static PairSide<float> PairSideOf(AnySingle @base) {
        return new PairSide<float>(@base, value => @base.GreaterThanOrEqualTo(value), value => @base.GreaterThan(value), value => @base.LessThanOrEqualTo(value), value => @base.LessThan(value), @base.Largest);
    }

    private static PairSide<double> PairSideOf(AnyDouble @base) {
        return new PairSide<double>(@base, value => @base.GreaterThanOrEqualTo(value), value => @base.GreaterThan(value), value => @base.LessThanOrEqualTo(value), value => @base.LessThan(value), @base.Largest);
    }

    private static PairSide<decimal> PairSideOf(AnyDecimal @base) {
        return new PairSide<decimal>(@base, value => @base.GreaterThanOrEqualTo(value), value => @base.GreaterThan(value), value => @base.LessThanOrEqualTo(value), value => @base.LessThan(value), @base.Largest);
    }

    private static PairSide<TimeSpan> PairSideOf(AnyTimeSpan @base) {
        return new PairSide<TimeSpan>(@base, value => @base.GreaterThanOrEqualTo(value), value => @base.GreaterThan(value), value => @base.LessThanOrEqualTo(value), value => @base.LessThan(value), @base.Largest);
    }

    private static PairSide<DateTime> PairSideOf(AnyDateTime @base) {
        return new PairSide<DateTime>(@base, value => @base.AfterOrEqualTo(value), value => @base.After(value), value => @base.BeforeOrEqualTo(value), value => @base.Before(value), @base.Largest, @base.SuppliedLocalValue);
    }

    private static PairSide<DateTimeOffset> PairSideOf(AnyDateTimeOffset @base) {
        return new PairSide<DateTimeOffset>(@base, value => @base.AfterOrEqualTo(value), value => @base.After(value), value => @base.BeforeOrEqualTo(value), value => @base.Before(value), @base.Largest);
    }

#if NET8_0_OR_GREATER
    private static PairSide<Int128> PairSideOf(AnyInt128 @base) {
        return new PairSide<Int128>(@base, value => @base.GreaterThanOrEqualTo(value), value => @base.GreaterThan(value), value => @base.LessThanOrEqualTo(value), value => @base.LessThan(value), @base.Largest);
    }

#endif

#if NET8_0_OR_GREATER
    private static PairSide<UInt128> PairSideOf(AnyUInt128 @base) {
        return new PairSide<UInt128>(@base, value => @base.GreaterThanOrEqualTo(value), value => @base.GreaterThan(value), value => @base.LessThanOrEqualTo(value), value => @base.LessThan(value), @base.Largest);
    }

#endif

#if NET8_0_OR_GREATER
    private static PairSide<Half> PairSideOf(AnyHalf @base) {
        return new PairSide<Half>(@base, value => @base.GreaterThanOrEqualTo(value), value => @base.GreaterThan(value), value => @base.LessThanOrEqualTo(value), value => @base.LessThan(value), @base.Largest);
    }

#endif

#if NET8_0_OR_GREATER
    private static PairSide<DateOnly> PairSideOf(AnyDateOnly @base) {
        return new PairSide<DateOnly>(@base, value => @base.AfterOrEqualTo(value), value => @base.After(value), value => @base.BeforeOrEqualTo(value), value => @base.Before(value), @base.Largest);
    }

#endif

}
