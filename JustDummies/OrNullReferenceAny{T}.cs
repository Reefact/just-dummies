namespace JustDummies;

/// <summary>
///     A reference-type generator made optional: <c>null</c> about half the time and, otherwise, a value the wrapped
///     generator draws. What <see cref="NullableReferenceExtensions.OrNull{T}" /> returns — the sibling of
///     <see cref="OrNullAny{T}" />, which carries the reasoning both share (ADR-0104).
/// </summary>
/// <typeparam name="T">The underlying reference type.</typeparam>
internal sealed class OrNullReferenceAny<T> : IAny<T?>, IHasRandomSource, IReproducibilityHint, IComparerSensitiveCardinality<T?>
    where T : class {

    #region Fields declarations

    private readonly bool          _addsNull;
    private readonly bool          _drawsOnlyFromSource;
    private readonly RandomSource? _source;
    private readonly IAny<T>       _underlying;

    #endregion

    internal OrNullReferenceAny(IAny<T> underlying) {
        if (underlying is null) { throw new ArgumentNullException(nameof(underlying)); }

        _underlying          = underlying;
        _source              = AnyDerivation.SourceOf(underlying);
        _drawsOnlyFromSource = AnyDerivation.IsReproducible(underlying);
        // Asked of the hint, never of the wrapped generator's type: whatever relays a domain that already holds null
        // — this generator applied twice, or any generator forwarding one — says so through its own membership.
        _addsNull            = !(underlying is ICardinalityHint<T> hint && hint.Contains(null!));
    }

    RandomSource? IHasRandomSource.Source => _source;

    bool IReproducibilityHint.DrawsOnlyFromSource => _drawsOnlyFromSource;

    /// <summary>
    ///     The wrapped generator's own bound, plus <c>null</c> — unless its domain holds <c>null</c> already. Unlike
    ///     with a <see cref="Nullable{T}" />, applying <c>OrNull()</c> twice to a reference generator still compiles,
    ///     because nullable reference annotations do not change the runtime type, and counting <c>null</c> twice
    ///     would over-state the domain in the direction that sends a distinct collection past it.
    /// </summary>
    long? ICardinalityHint<T?>.DistinctCardinality => WithNull(AnyDerivation.CardinalityOf(_underlying));

    /// <summary>Whether <paramref name="value" /> is <c>null</c>, or one the wrapped generator could produce.</summary>
    bool ICardinalityHint<T?>.Contains(T? value) {
        return value is null || (_underlying is ICardinalityHint<T> hint && hint.Contains(value));
    }

    /// <summary>
    ///     The bound under a collection's own comparer: the wrapped generator's answer, plus <c>null</c>.
    /// </summary>
    /// <remarks>
    ///     Declared unconditionally for the reason <see cref="OrNullAny{T}" /> gives, and answered the same way
    ///     (ADR-0069).
    /// </remarks>
    long? IComparerSensitiveCardinality<T?>.CardinalityUnderACustomComparer {
        get {
            return WithNull(_underlying is IComparerSensitiveCardinality<T> sensitive
                                ? sensitive.CardinalityUnderACustomComparer
                                : AnyDerivation.CardinalityOf(_underlying));
        }
    }

    private long? WithNull(long? cardinality) {
        return _addsNull ? AnyDerivation.CardinalityWithNull(cardinality) : cardinality;
    }

    /// <inheritdoc />
    public T? Generate() {
        RandomSource working = _source ?? AmbientRandomSource.Instance;

        return working.Current.Next(NullableExtensions.NullDrawOutcomes) == 0 ? null : _underlying.Generate();
    }

}
