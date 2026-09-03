namespace JustDummies;

/// <summary>
///     A generator derived from other generators (<c>As</c>, <c>Combine</c>): it delegates generation to a closure
///     and carries, when known, the random context of the generators it derives from — so a failure inside the
///     derivation can still name the seed that replays the run. It also remembers whether every operand it draws from
///     is reproducible (<see cref="IReproducibilityHint" />): a single foreign operand leaves a non-null source to name
///     but makes the derived value unreproducible, which the seed reporting must not over-promise.
/// </summary>
/// <typeparam name="T">The type of the generated values.</typeparam>
internal sealed class DerivedDummy<T> : IDummy<T>, IHasRandomSource, IReproducibilityHint {

    #region Fields declarations

    private readonly bool          _drawsOnlyFromSource;
    private readonly Func<T>       _generate;
    private readonly RandomSource? _source;

    #endregion

    internal DerivedDummy(RandomSource? source, bool drawsOnlyFromSource, Func<T> generate) {
        if (generate is null) { throw new ArgumentNullException(nameof(generate)); }

        _source              = source;
        _drawsOnlyFromSource = drawsOnlyFromSource;
        _generate            = generate;
    }

    RandomSource? IHasRandomSource.Source => _source;

    bool IReproducibilityHint.DrawsOnlyFromSource => _drawsOnlyFromSource;

    /// <inheritdoc />
    public T Generate() {
        return _generate();
    }

}
