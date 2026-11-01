namespace JustDummies;

/// <summary>
///     A generator of two values tied by an order — a span's start and end, a range's minimum and maximum — drawn so
///     that the first never exceeds the second, by construction: the first is drawn from its base kept at or below the
///     largest value the second base can produce, then the second from its base kept at or above it. A pinned value
///     bounds the other draw the same way, in either direction, so pinning the end constrains the start
///     (ADR-0126).
/// </summary>
/// <remarks>
///     <para>
///         Each pin is declared once, like a value set: the same value again changes nothing, another is refused as
///         already defined. Two pins in the wrong order, or a pin no value of the other base can follow,
///         are refused at the call naming both sides. A pinned value is a literal, as with <c>Any.Fixed</c>: it is not
///         checked against its own base, only against the other one.
///     </para>
///     <para>
///         The pair draws from the source its bases share and replays with it; two bases drawing from different
///         explicit contexts do not replay from either seed alone, as with <c>Any.Combine</c>.
///     </para>
/// </remarks>
/// <typeparam name="T">The type of both values.</typeparam>
public sealed class AnyOrderedPair<T> : IAny<(T First, T Second)>, IHasRandomSource, IReproducibilityHint {

    private readonly OrderedPairBounds<T> _bounds;
    private readonly IAny<T>              _first;
    private readonly Pin?                 _firstPin;
    private readonly bool                 _reproducible;
    private readonly Pin?                 _secondPin;
    private readonly RandomSource?        _source;
    private readonly bool                 _strict;

    private AnyOrderedPair(IAny<T> first, OrderedPairBounds<T> bounds, bool strict, Pin? firstPin, Pin? secondPin, RandomSource? source, bool reproducible) {
        _first        = first;
        _bounds       = bounds;
        _strict       = strict;
        _firstPin     = firstPin;
        _secondPin    = secondPin;
        _source       = source;
        _reproducible = reproducible;
    }

    RandomSource? IHasRandomSource.Source => _source;

    bool IReproducibilityHint.DrawsOnlyFromSource => _reproducible;

    /// <summary>
    ///     The pair over <paramref name="first" /> and <paramref name="second" />, the first base already kept at or
    ///     below the largest second — the one narrowing that makes every unpinned draw succeed.
    /// </summary>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when no first value precedes the largest second.</exception>
    internal static AnyOrderedPair<T> Create(IAny<T> first, IAny<T> second, bool strict, OrderedPairBounds<T> bounds) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }
        if (bounds is null) { throw new ArgumentNullException(nameof(bounds)); }

        ConstraintCall call         = ConstraintCall.Of("OrderedPair", strict ? ["first", "second", "strict: true"] : ["first", "second"]);
        RandomSource?  source       = AnyDerivation.SourceOf(first) ?? AnyDerivation.SourceOf(second);
        bool           reproducible = AnyDerivation.DrawsOnlyFrom(first, source) && AnyDerivation.DrawsOnlyFrom(second, source);
        IAny<T>        narrowed     = Bounded(() => bounds.FirstUpTo(bounds.LargestSecond, strict), call, $"no first value lies {Below(strict)} {AnyDerivation.Display(bounds.LargestSecond)}, the largest value of the second base");

        return new AnyOrderedPair<T>(narrowed, bounds, strict, null, null, source, reproducible);
    }

    /// <summary>
    ///     Pins the first value: the second is then drawn from its base kept at or above <paramref name="value" />
    ///     — above it when the pair is strict. Declared once per generator.
    /// </summary>
    /// <param name="value">The first value.</param>
    /// <returns>A new generator carrying the pin.</returns>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the first is already pinned to another value, when the second is pinned before it, or when no
    ///     second value follows <paramref name="value" />.
    /// </exception>
    public AnyOrderedPair<T> WithFirst(T value) {
        ConstraintCall call = ConstraintCall.Of(nameof(WithFirst), AnyDerivation.Display(value));

        if (_firstPin is { } pinned) { return SamePin(pinned, value) ? this : throw ConflictingAnyConstraintException.AlreadyDefined(call, pinned.Call); }
        if (_secondPin is { } second) {
            if (!Follows(value, second.Value)) { throw ConflictingAnyConstraintException.AlreadyBoundedAbove(call, second.Call, second.Rendered, _strict); }
        } else {
            Bounded(() => _bounds.SecondFrom(value, _strict), call, $"no second value lies {Above(_strict)} {AnyDerivation.Display(value)}");
        }

        return new AnyOrderedPair<T>(_first, _bounds, _strict, new Pin(value, call), _secondPin, _source, _reproducible);
    }

    /// <summary>
    ///     Pins the second value: the first is then drawn from its base kept at or below <paramref name="value" />
    ///     — below it when the pair is strict. Declared once per generator.
    /// </summary>
    /// <param name="value">The second value.</param>
    /// <returns>A new generator carrying the pin.</returns>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the second is already pinned to another value, when the first is pinned after it, or when no
    ///     first value precedes <paramref name="value" />.
    /// </exception>
    public AnyOrderedPair<T> WithSecond(T value) {
        ConstraintCall call = ConstraintCall.Of(nameof(WithSecond), AnyDerivation.Display(value));

        if (_secondPin is { } pinned) { return SamePin(pinned, value) ? this : throw ConflictingAnyConstraintException.AlreadyDefined(call, pinned.Call); }
        if (_firstPin is { } first) {
            if (!Follows(first.Value, value)) { throw ConflictingAnyConstraintException.AlreadyBoundedBelow(call, first.Call, first.Rendered, _strict); }
        } else {
            Bounded(() => _bounds.FirstUpTo(value, _strict), call, $"no first value lies {Below(_strict)} {AnyDerivation.Display(value)}");
        }

        return new AnyOrderedPair<T>(_first, _bounds, _strict, _firstPin, new Pin(value, call), _source, _reproducible);
    }

    /// <inheritdoc />
    public (T First, T Second) Generate() {
        if (_firstPin is { } first) {
            return (first.Value, _secondPin is { } both ? both.Value : _bounds.SecondFrom(first.Value, _strict).Generate());
        }

        if (_secondPin is { } second) { return (_bounds.FirstUpTo(second.Value, _strict).Generate(), second.Value); }

        T drawn = _first.Generate();

        return (drawn, _bounds.SecondFrom(drawn, _strict).Generate());
    }

    private bool Follows(T first, T second) {
        int order = Comparer<T>.Default.Compare(first, second);

        return _strict ? order < 0 : order <= 0;
    }

    private static bool SamePin(Pin pinned, T value) {
        return EqualityComparer<T>.Default.Equals(pinned.Value, value);
    }

    private static string Above(bool strict) {
        return strict ? "above" : "at or above";
    }

    private static string Below(bool strict) {
        return strict ? "below" : "at or below";
    }

    /// <summary>
    ///     One base bounded by the other side of the order, or the refusal that base raised, requoted under the call
    ///     that asked for the bound so both sides are named.
    /// </summary>
    private static IAny<T> Bounded(Func<IAny<T>> bound, ConstraintCall applying, string missing) {
        try {
            return bound();
        } catch (ConflictingAnyConstraintException refusal) {
            throw ConflictingAnyConstraintException.OrderedPairHasNoValue(applying, missing, refusal);
        }
    }

    /// <summary>A pinned value and the call that pinned it.</summary>
    private sealed class Pin {

        internal Pin(T value, ConstraintCall call) {
            Value = value;
            Call  = call;
        }

        internal T Value { get; }

        internal ConstraintCall Call { get; }

        internal string Rendered => AnyDerivation.Display(Value);

    }

}
