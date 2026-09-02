namespace JustDummies;

/// <summary>
///     A value-type generator seen as a generator of <see cref="Nullable{T}" />: the same values, the wider type,
///     and never <c>null</c>.
/// </summary>
/// <remarks>
///     Distinct from a <see cref="DerivedAny{T}" /> built over <c>As(value =&gt; (T?)value)</c>, which is how this
///     conversion used to be written, and the difference is the whole reason this type exists. A derived generator
///     advertises no <see cref="ICardinalityHint{T}" /> — it cannot, because an arbitrary factory has no inverse to
///     answer membership with — so a distinct collection over one has no ceiling, draws a count the element pool
///     cannot fill, and dies on the bounded redraw. A set of three <c>Slot?</c> is not a domain any generator should
///     refuse.
///     <para>
///         The lift is the one projection that escapes that: it is total and injective, and its inverse is
///         <see cref="Nullable{T}.Value" />. So both members of the hint forward soundly rather than only one of
///         them — the count is the wrapped generator's, and membership is "has a value, and that value is one of
///         its". This widens nothing about derived generators; it adds a generator that is not a derivation.
///     </para>
/// </remarks>
/// <typeparam name="T">The underlying value type.</typeparam>
internal sealed class NullableAny<T> : IAny<T?>, IHasRandomSource, IReproducibilityHint, IComparerSensitiveCardinality<T?>
    where T : struct {

    #region Fields declarations

    private readonly bool          _drawsOnlyFromSource;
    private readonly IAny<T>       _underlying;
    private readonly RandomSource? _source;

    #endregion

    internal NullableAny(IAny<T> underlying) {
        if (underlying is null) { throw new ArgumentNullException(nameof(underlying)); }

        _underlying          = underlying;
        _source              = AnyDerivation.SourceOf(underlying);
        _drawsOnlyFromSource = AnyDerivation.IsReproducible(underlying);
    }

    RandomSource? IHasRandomSource.Source => _source;

    bool IReproducibilityHint.DrawsOnlyFromSource => _drawsOnlyFromSource;

    /// <summary>The wrapped generator's own bound, unchanged: lifting adds a type, not a value.</summary>
    long? ICardinalityHint<T?>.DistinctCardinality => AnyDerivation.CardinalityOf(_underlying);

    /// <summary>
    ///     Whether the wrapped generator could produce <paramref name="value" />, once it has one.
    /// </summary>
    /// <remarks>
    ///     <c>null</c> answers <c>false</c>, and that is the right answer rather than a conservative one: this
    ///     generator never draws it (ADR-0064), so a collection pinning <c>null</c> through <c>Containing(...)</c>
    ///     is genuinely extending the domain and must be counted as doing so.
    /// </remarks>
    bool ICardinalityHint<T?>.Contains(T? value) {
        return value.HasValue && _underlying is ICardinalityHint<T> hint && hint.Contains(value.Value);
    }

    /// <summary>
    ///     The bound under a collection's own comparer, deferred to the wrapped generator exactly as it answers.
    /// </summary>
    /// <remarks>
    ///     Declared unconditionally because an interface cannot be implemented conditionally, and answered so that
    ///     it changes nothing for a generator that is not comparer-sensitive: it then gives the same number the
    ///     plain bound gives, which is what asking a non-sensitive generator would have returned anyway (ADR-0069).
    /// </remarks>
    long? IComparerSensitiveCardinality<T?>.CardinalityUnderACustomComparer {
        get {
            return _underlying is IComparerSensitiveCardinality<T> sensitive
                       ? sensitive.CardinalityUnderACustomComparer
                       : AnyDerivation.CardinalityOf(_underlying);
        }
    }

    /// <inheritdoc />
    public T? Generate() {
        return _underlying.Generate();
    }

}
