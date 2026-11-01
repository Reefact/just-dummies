namespace JustDummies;

/// <summary>
///     What an ordered pair needs from its two bases and cannot ask of an <see cref="IAny{T}" />: each base bounded
///     on the side the order requires, and the largest value the second can produce. Each <c>Any.OrderedPair</c>
///     overload builds one from its own builder type, which is how the pair stays constructive on every ordered
///     type without a shared bound interface (ADR-0126).
/// </summary>
/// <typeparam name="T">The type of both values.</typeparam>
internal sealed class OrderedPairBounds<T> {

    private readonly Func<T, IAny<T>> _firstAtOrBelow;
    private readonly Func<T, IAny<T>> _firstBelow;
    private readonly Func<T, IAny<T>> _secondAbove;
    private readonly Func<T, IAny<T>> _secondAtOrAbove;

    internal OrderedPairBounds(Func<T, IAny<T>> secondAtOrAbove, Func<T, IAny<T>> secondAbove, Func<T, IAny<T>> firstAtOrBelow, Func<T, IAny<T>> firstBelow, T largestSecond) {
        _secondAtOrAbove = secondAtOrAbove ?? throw new ArgumentNullException(nameof(secondAtOrAbove));
        _secondAbove     = secondAbove ?? throw new ArgumentNullException(nameof(secondAbove));
        _firstAtOrBelow  = firstAtOrBelow ?? throw new ArgumentNullException(nameof(firstAtOrBelow));
        _firstBelow      = firstBelow ?? throw new ArgumentNullException(nameof(firstBelow));
        LargestSecond    = largestSecond;
    }

    /// <summary>The largest value the second base can produce, which the first is kept at or below.</summary>
    internal T LargestSecond { get; }

    /// <summary>The second base, bounded to follow <paramref name="first" />.</summary>
    internal IAny<T> SecondFrom(T first, bool strict) {
        return strict ? _secondAbove(first) : _secondAtOrAbove(first);
    }

    /// <summary>The first base, bounded to precede <paramref name="second" />.</summary>
    internal IAny<T> FirstUpTo(T second, bool strict) {
        return strict ? _firstBelow(second) : _firstAtOrBelow(second);
    }

}
