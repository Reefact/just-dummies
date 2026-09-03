namespace JustDummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="HashSet{T}" /> values over an element generator. A set is
///     distinct by nature, so it carries the collection constraint surface
///     (<see cref="DummyCollection{TItem,TResult,TSelf}" />) — count bounds and contained values — without a
///     <c>Distinct()</c> toggle. When the element generator advertises fewer distinct values than the requested
///     count, the contradiction is caught eagerly with a <see cref="ConflictingDummyConstraintException" />; otherwise a
///     genuine shortfall surfaces at generation as an <see cref="DummyGenerationException" />.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public sealed class DummySet<T> : DummyCollection<T, HashSet<T>, DummySet<T>> {

    internal DummySet(RandomSource? source, CollectionState<T> state) : base(source, state) { }

    private protected override DummySet<T> With(CollectionState<T> state) {
        if (state is null) { throw new ArgumentNullException(nameof(state)); }

        return new DummySet<T>(SourceOrNull, state);
    }

    private protected override HashSet<T> Build(List<T> items) {
        if (items is null) { throw new ArgumentNullException(nameof(items)); }

        // The state already deduplicated under the comparer; the set carries the same comparer so later lookups
        // by the caller behave identically.
        return new HashSet<T>(items, State.Comparer);
    }

}
