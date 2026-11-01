namespace JustDummies;

/// <summary>
///     What <see cref="Any.PermutationOf{T}" /> returns: a uniform shuffle of fixed positions, drawn from its own
///     source, so a replay reproduces the order.
/// </summary>
/// <typeparam name="T">The type of the items.</typeparam>
internal sealed class PermutationAny<T> : IAny<List<T>>, IHasRandomSource {

    #region Fields declarations

    private readonly T[]          _items;
    private readonly RandomSource _source;

    #endregion

    internal PermutationAny(RandomSource source, T[] items) {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _items  = items ?? throw new ArgumentNullException(nameof(items));
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <summary>A Fisher–Yates shuffle of a copy of the items: one draw per position but the first.</summary>
    public List<T> Generate() {
        List<T>      order  = [.. _items];
        SeededRandom random = _source.Current;
        for (int last = order.Count - 1; last > 0; last--) {
            int chosen = random.Next(last + 1);
            (order[last], order[chosen]) = (order[chosen], order[last]);
        }

        return order;
    }

}
