namespace JustDummies;

/// <summary>
///     What a pair needs from one of its bases and cannot ask of an <see cref="IAny{T}" />: the base bounded on
///     either side, the largest value it can produce, and — for <c>DateTime</c> — a <c>Local</c> value it was
///     supplied. Each <c>Any.Pair</c> overload builds two from its own builder type, which is how the pair stays
///     constructive on every ordered type without a shared bound interface the builders do not have (ADR-0131).
/// </summary>
/// <typeparam name="T">The type the base draws.</typeparam>
internal sealed class PairSide<T> {

    private readonly Func<T, IAny<T>> _above;
    private readonly Func<T, IAny<T>> _atLeast;
    private readonly Func<T, IAny<T>> _atMost;
    private readonly Func<T, IAny<T>> _below;
    private readonly Func<T>          _largest;
    private readonly Func<string?>    _localValue;

    internal PairSide(IAny<T> @base, Func<T, IAny<T>> atLeast, Func<T, IAny<T>> above, Func<T, IAny<T>> atMost, Func<T, IAny<T>> below, Func<T> largest, Func<string?>? localValue = null) {
        Base        = @base ?? throw new ArgumentNullException(nameof(@base));
        _atLeast    = atLeast ?? throw new ArgumentNullException(nameof(atLeast));
        _above      = above ?? throw new ArgumentNullException(nameof(above));
        _atMost     = atMost ?? throw new ArgumentNullException(nameof(atMost));
        _below      = below ?? throw new ArgumentNullException(nameof(below));
        _largest    = largest ?? throw new ArgumentNullException(nameof(largest));
        _localValue = localValue ?? NoLocalValue;
    }

    /// <summary>The base itself, drawn as it is when the pair declares no relation.</summary>
    internal IAny<T> Base { get; }

    /// <summary>The largest value the base can produce, which the other side is kept at or below.</summary>
    internal T Largest() {
        return _largest();
    }

    /// <summary>The base kept at or above <paramref name="value" />, or above it when <paramref name="strict" />.</summary>
    internal IAny<T> From(T value, bool strict) {
        return strict ? _above(value) : _atLeast(value);
    }

    /// <summary>The base kept at or below <paramref name="value" />, or below it when <paramref name="strict" />.</summary>
    internal IAny<T> UpTo(T value, bool strict) {
        return strict ? _below(value) : _atMost(value);
    }

    /// <summary>
    ///     A <c>Local</c> value the base was supplied and still allows, rendered, or <c>null</c>. Only a
    ///     <c>DateTime</c> base can hold one; a pair refuses it once an order makes a drawn value a bound (ADR-0131,
    ///     ADR-0114).
    /// </summary>
    internal string? LocalValue() {
        return _localValue();
    }

    private static string? NoLocalValue() {
        return null;
    }

}
