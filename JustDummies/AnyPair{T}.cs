namespace JustDummies;

/// <summary>
///     A generator of two values of one ordered type, each drawn from a base of its own — a span's start and end, a
///     range's minimum and maximum. Declared without a relation, the two are drawn independently.
///     <see cref="Having" /> declares the order between them, which is then held by construction, never by redraw: the
///     side the order puts below is drawn from its base kept within what the other base can follow, then the other
///     side from its base kept on the right side of it (ADR-0131).
/// </summary>
/// <remarks>
///     <para>
///         The pair has no notion of a fixed side. A side the test fixes is not drawn, so it needs no pair: keep it a
///         literal and bound the other side on its own builder — <c>Any.DateTime().AfterOrEqualTo(start)</c>.
///     </para>
///     <para>
///         The pair draws from the source its bases share and replays with it; two bases drawing from different
///         explicit contexts do not replay from either seed alone, as with <c>Any.Combine</c>.
///     </para>
/// </remarks>
/// <typeparam name="T">The type of both values.</typeparam>
public sealed class AnyPair<T> : IAny<(T Item1, T Item2)>, IHasRandomSource, IReproducibilityHint {

    private const string Item1Name = "Item1";
    private const string Item2Name = "Item2";

    private readonly PairSide<T>   _item1;
    private readonly PairSide<T>   _item2;
    private readonly Order?        _order;
    private readonly bool          _reproducible;
    private readonly RandomSource? _source;

    private AnyPair(PairSide<T> item1, PairSide<T> item2, Order? order, RandomSource? source, bool reproducible) {
        _item1        = item1;
        _item2        = item2;
        _order        = order;
        _source       = source;
        _reproducible = reproducible;
        Having        = new AnyPairRelation<T>(this);
    }

    RandomSource? IHasRandomSource.Source => _source;

    bool IReproducibilityHint.DrawsOnlyFromSource => _reproducible;

    /// <summary>
    ///     Introduces the relation between the two items, spelled in the vocabulary of the pair's builder:
    ///     <c>Having.Item1LessThanItem2()</c> and its siblings on the numbers and durations,
    ///     <c>Having.Item1BeforeItem2()</c> and its siblings on the dates. A pair declares at most one relation: the
    ///     same one again changes nothing, another is refused as already defined.
    /// </summary>
    public AnyPairRelation<T> Having { get; }

    /// <summary>The pair over two bases, declaring no relation yet.</summary>
    internal static AnyPair<T> Create(PairSide<T> item1, PairSide<T> item2) {
        if (item1 is null) { throw new ArgumentNullException(nameof(item1)); }
        if (item2 is null) { throw new ArgumentNullException(nameof(item2)); }

        RandomSource? source       = AnyDerivation.SourceOf(item1.Base) ?? AnyDerivation.SourceOf(item2.Base);
        bool          reproducible = AnyDerivation.DrawsOnlyFrom(item1.Base, source) && AnyDerivation.DrawsOnlyFrom(item2.Base, source);

        return new AnyPair<T>(item1, item2, null, source, reproducible);
    }

    /// <inheritdoc />
    public (T Item1, T Item2) Generate() {
        if (_order is not null) { return _order.Generate(); }

        T item1 = _item1.Base.Generate();

        return (item1, _item2.Base.Generate());
    }

    /// <summary>
    ///     The pair with an order declared: <paramref name="item1Below" /> says which item the order puts below,
    ///     <paramref name="strict" /> whether strictly. The side below is narrowed once, here, to what the other base can
    ///     follow, which is what makes every later draw succeed.
    /// </summary>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when another relation is already declared, when a <c>DateTime</c> base holds a <c>Local</c> value, or
    ///     when no value of the side below lies within what the other base can follow.
    /// </exception>
    internal AnyPair<T> Ordered(string relation, bool item1Below, bool strict) {
        if (relation is null) { throw new ArgumentNullException(nameof(relation)); }

        ConstraintCall call = ConstraintCall.Of(nameof(Having) + "." + relation);
        if (_order is { } declared) { return declared.Call == call ? this : throw ConflictingAnyConstraintException.AlreadyDefined(call, declared.Call); }

        RefuseLocal(_item1, Item1Name, call);
        RefuseLocal(_item2, Item2Name, call);

        PairSide<T> lower     = item1Below ? _item1 : _item2;
        PairSide<T> upper     = item1Below ? _item2 : _item1;
        string      lowerName = item1Below ? Item1Name : Item2Name;
        string      upperName = item1Below ? Item2Name : Item1Name;
        T           largest   = upper.Largest();
        IAny<T>     narrowed;
        try {
            narrowed = lower.UpTo(largest, strict);
        } catch (ConflictingAnyConstraintException refusal) {
            throw ConflictingAnyConstraintException.PairHasNoValue(call, $"no {lowerName} value lies {(strict ? "below" : "at or below")} {AnyDerivation.Display(largest)}, the largest value of {upperName}'s base", refusal);
        }

        return new AnyPair<T>(_item1, _item2, new Order(narrowed, upper, strict, item1Below, call), _source, _reproducible);
    }

    private static void RefuseLocal(PairSide<T> side, string name, ConstraintCall call) {
        if (side.LocalValue() is { } local) { throw ConflictingAnyConstraintException.LocalValueInAPair(call, name, local); }
    }

    /// <summary>A declared order: the side below, already narrowed, and the side above it is drawn from.</summary>
    private sealed class Order {

        private readonly bool        _item1Below;
        private readonly IAny<T>     _lower;
        private readonly bool        _strict;
        private readonly PairSide<T> _upper;

        internal Order(IAny<T> lower, PairSide<T> upper, bool strict, bool item1Below, ConstraintCall call) {
            _lower      = lower ?? throw new ArgumentNullException(nameof(lower));
            _upper      = upper ?? throw new ArgumentNullException(nameof(upper));
            _strict     = strict;
            _item1Below = item1Below;
            Call        = call ?? throw new ArgumentNullException(nameof(call));
        }

        internal ConstraintCall Call { get; }

        internal (T Item1, T Item2) Generate() {
            T below = _lower.Generate();
            T above = _upper.From(below, _strict).Generate();

            return _item1Below ? (below, above) : (above, below);
        }

    }

}
