namespace JustDummies;

/// <summary>
///     A fluent generator of arbitrary array (<c>T[]</c>) values over an element generator. Shares the collection
///     constraint surface (<see cref="AnyCollection{TItem,TResult,TSelf}" />) — count bounds and contained values —
///     and adds <see cref="Distinct()" /> to require pairwise-distinct elements.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public sealed class AnyArray<T> : AnyCollection<T, T[], AnyArray<T>> {

    private readonly ElementOrder<T>? _order;

    internal AnyArray(RandomSource? source, CollectionState<T> state, ElementOrder<T>? order = null) : base(source, state) {
        _order = order;
    }

    /// <summary>Requires the elements to be pairwise distinct (default equality).</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint cannot be satisfied by the element generator's domain.</exception>
    public AnyArray<T> Distinct() {
        return With(State.AsDistinct(null, ConstraintCall.Of(nameof(Distinct))));
    }

    /// <summary>Requires the elements to be pairwise distinct under <paramref name="comparer" />.</summary>
    /// <param name="comparer">The equality comparer deciding whether two elements are the same.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="comparer" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint cannot be satisfied by the element generator's domain.</exception>
    public AnyArray<T> Distinct(IEqualityComparer<T> comparer) {
        if (comparer is null) { throw new ArgumentNullException(nameof(comparer)); }

        return With(State.AsDistinct(comparer, ConstraintCall.Of(nameof(Distinct), "comparer")));
    }

    /// <summary>
    ///     Requires the elements to be pairwise distinct by the key <paramref name="keySelector" /> returns — exactly
    ///     <see cref="Distinct(IEqualityComparer{T})" /> with a comparer by that key, under the key type's default
    ///     equality: the same bounded redraw, and the same loud failure with its seed when the key runs out of values.
    /// </summary>
    /// <remarks>
    ///     The key's domain is not checked up front: it comes from an opaque function, so it is known only by drawing.
    ///     A collection is distinct under one equality, so a different comparer declared before or after is refused.
    /// </remarks>
    /// <param name="keySelector">The key two elements must not share.</param>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="keySelector" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when a different equality is already declared.</exception>
    public AnyArray<T> DistinctBy<TKey>(Func<T, TKey> keySelector) {
        if (keySelector is null) { throw new ArgumentNullException(nameof(keySelector)); }

        return With(State.AsDistinct(new KeyEqualityComparer<T, TKey>(keySelector), ConstraintCall.Of(nameof(DistinctBy), "keySelector")));
    }

    /// <summary>
    ///     Arranges the elements in ascending, non-decreasing order of the key <paramref name="keySelector" /> returns —
    ///     <c>OrderedBy(p =&gt; p.At)</c> for points in time. The elements are drawn, then sorted, so every other
    ///     constraint keeps applying: the order changes the arrangement, never the content. Strictly increasing is
    ///     <c>OrderedBy(key).DistinctBy(key)</c>. Declared once per generator (ADR-0124).
    /// </summary>
    /// <remarks>
    ///     Two key orders are the same declaration only through the same key selector instance; a different one, or
    ///     <c>Ordered()</c>, declared before or after, is refused.
    /// </remarks>
    /// <param name="keySelector">The key the elements are arranged by.</param>
    /// <typeparam name="TKey">The type of the key, which has a natural order.</typeparam>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="keySelector" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when a different order is already declared.</exception>
    public AnyArray<T> OrderedBy<TKey>(Func<T, TKey> keySelector) where TKey : IComparable<TKey> {
        if (keySelector is null) { throw new ArgumentNullException(nameof(keySelector)); }

        return WithOrder(ElementOrder<T>.ByKey(keySelector, ConstraintCall.Of(nameof(OrderedBy), "keySelector")));
    }

    /// <summary>The same generator with <paramref name="order" /> declared — the seam <c>Ordered()</c> goes through.</summary>
    internal AnyArray<T> WithOrder(ElementOrder<T> order) {
        if (order is null) { throw new ArgumentNullException(nameof(order)); }

        return new AnyArray<T>(SourceOrNull, State, ElementOrder<T>.Declare(_order, order));
    }

    private protected override AnyArray<T> With(CollectionState<T> state) {
        if (state is null) { throw new ArgumentNullException(nameof(state)); }

        return new AnyArray<T>(SourceOrNull, state, _order);
    }

    private protected override T[] Build(List<T> items) {
        if (items is null) { throw new ArgumentNullException(nameof(items)); }

        _order?.Arrange(items);

        return items.ToArray();
    }

}
