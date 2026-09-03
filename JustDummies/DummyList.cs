namespace JustDummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="List{T}" /> values over an element generator. Shares the collection
///     constraint surface (<see cref="DummyCollection{TItem,TResult,TSelf}" />) — count bounds and contained values —
///     and adds <see cref="Distinct()" /> to require pairwise-distinct elements.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public sealed class DummyList<T> : DummyCollection<T, List<T>, DummyList<T>> {

    internal DummyList(RandomSource? source, CollectionState<T> state) : base(source, state) { }

    /// <summary>Requires the elements to be pairwise distinct (default equality).</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint cannot be satisfied by the element generator's domain.</exception>
    public DummyList<T> Distinct() {
        return With(State.AsDistinct(null, ConstraintCall.Of(nameof(Distinct))));
    }

    /// <summary>Requires the elements to be pairwise distinct under <paramref name="comparer" />.</summary>
    /// <param name="comparer">The equality comparer deciding whether two elements are the same.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="comparer" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint cannot be satisfied by the element generator's domain.</exception>
    public DummyList<T> Distinct(IEqualityComparer<T> comparer) {
        if (comparer is null) { throw new ArgumentNullException(nameof(comparer)); }

        return With(State.AsDistinct(comparer, ConstraintCall.Of(nameof(Distinct), "comparer")));
    }

    private protected override DummyList<T> With(CollectionState<T> state) {
        if (state is null) { throw new ArgumentNullException(nameof(state)); }

        return new DummyList<T>(SourceOrNull, state);
    }

    private protected override List<T> Build(List<T> items) {
        if (items is null) { throw new ArgumentNullException(nameof(items)); }

        return items;
    }

}
