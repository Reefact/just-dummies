namespace JustDummies;

/// <summary>
///     A value-type generator made optional: <c>null</c> about half the time and, otherwise, a value the wrapped
///     generator draws. What <see cref="NullableExtensions.OrNull{T}" /> returns.
/// </summary>
/// <remarks>
///     Not a <see cref="DerivedAny{T}" />, which is what this used to be, and the difference is the whole reason this
///     type exists. A derived generator advertises no <see cref="ICardinalityHint{T}" /> — an arbitrary factory has
///     no inverse to answer membership with — so a distinct collection over one had no ceiling, drew a count the
///     element domain could not fill, and died on the bounded redraw. A set of <c>Slot?</c> drawn through
///     <c>OrNull()</c> is a domain of the enum's members and <c>null</c>, and no generator should refuse it.
///     <para>
///         Making a value optional is a projection whose inverse is known, as lifting it is (ADR-0094): a value is
///         either <c>null</c> or one the wrapped generator produced. So both members of the hint answer soundly — the
///         count is the wrapped generator's plus one, and membership is "is <c>null</c>, or is one of its" (ADR-0104).
///     </para>
/// </remarks>
/// <typeparam name="T">The underlying value type.</typeparam>
internal sealed class OrNullAny<T> : IAny<T?>, IHasRandomSource, IReproducibilityHint, IComparerSensitiveCardinality<T?>
    where T : struct {

    #region Fields declarations

    private readonly bool          _drawsOnlyFromSource;
    private readonly RandomSource? _source;
    private readonly IAny<T>       _underlying;

    #endregion

    internal OrNullAny(IAny<T> underlying) {
        if (underlying is null) { throw new ArgumentNullException(nameof(underlying)); }

        _underlying          = underlying;
        _source              = AnyDerivation.SourceOf(underlying);
        _drawsOnlyFromSource = AnyDerivation.IsReproducible(underlying);
    }

    RandomSource? IHasRandomSource.Source => _source;

    bool IReproducibilityHint.DrawsOnlyFromSource => _drawsOnlyFromSource;

    /// <summary>The wrapped generator's own bound, plus the one value it never draws: <c>null</c>.</summary>
    long? ICardinalityHint<T?>.DistinctCardinality => AnyDerivation.CardinalityWithNull(AnyDerivation.CardinalityOf(_underlying));

    /// <summary>Whether <paramref name="value" /> is <c>null</c>, or one the wrapped generator could produce.</summary>
    bool ICardinalityHint<T?>.Contains(T? value) {
        return !value.HasValue || (_underlying is ICardinalityHint<T> hint && hint.Contains(value.Value));
    }

    /// <summary>
    ///     The bound under a collection's own comparer: the wrapped generator's answer, plus <c>null</c>.
    /// </summary>
    /// <remarks>
    ///     Declared unconditionally because an interface cannot be implemented conditionally, and answered so that
    ///     it changes nothing for a generator that is not comparer-sensitive: it then gives the same number the
    ///     plain bound gives (ADR-0069).
    /// </remarks>
    long? IComparerSensitiveCardinality<T?>.CardinalityUnderACustomComparer {
        get {
            return AnyDerivation.CardinalityWithNull(_underlying is IComparerSensitiveCardinality<T> sensitive
                                                         ? sensitive.CardinalityUnderACustomComparer
                                                         : AnyDerivation.CardinalityOf(_underlying));
        }
    }

    /// <inheritdoc />
    public T? Generate() {
        RandomSource working = _source ?? AmbientRandomSource.Instance;

        return working.Current.Next(NullableExtensions.NullDrawOutcomes) == 0 ? (T?)null : _underlying.Generate();
    }

}
