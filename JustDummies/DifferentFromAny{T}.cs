namespace JustDummies;

/// <summary>
///     What <see cref="AnyExtensions.DifferentFrom{T}" /> returns: a generator redrawing while its operand yields the
///     excluded value — the <b>rejective</b> exclusion, available on any <see cref="IAny{T}" />. It sees nothing before
///     <see cref="Generate" />; a draw that keeps producing the excluded value exhausts a bounded redraw and fails with
///     its seed (ADR-0121).
/// </summary>
/// <typeparam name="T">The type of the generated values.</typeparam>
internal sealed class DifferentFromAny<T> : IAny<T>, IHasRandomSource, IReproducibilityHint, ICardinalityHint<T> {

    #region Statics members declarations

    /// <summary>
    ///     How many draws in a row may produce the excluded value before the redraw gives up. A generator that yields the
    ///     excluded value this often yields nothing else in practice — a pinned or single-valued one — and a larger budget
    ///     would only delay the report.
    /// </summary>
    internal const int RedrawBudget = 1_000;

    #endregion

    #region Fields declarations

    private readonly bool          _drawsOnlyFromSource;
    private readonly T             _excluded;
    private readonly RandomSource? _source;
    private readonly IAny<T>       _underlying;

    #endregion

    internal DifferentFromAny(IAny<T> underlying, T excluded) {
        if (underlying is null) { throw new ArgumentNullException(nameof(underlying)); }

        _underlying          = underlying;
        _excluded            = excluded;
        _source              = AnyDerivation.SourceOf(underlying);
        _drawsOnlyFromSource = AnyDerivation.IsReproducible(underlying);
    }

    RandomSource? IHasRandomSource.Source => _source;

    bool IReproducibilityHint.DrawsOnlyFromSource => _drawsOnlyFromSource;

    /// <summary>The operand's bound, less the excluded value when the operand could draw it.</summary>
    long? ICardinalityHint<T>.DistinctCardinality {
        get {
            long? bound = AnyDerivation.CardinalityOf(_underlying);
            if (bound is not long count || _underlying is not ICardinalityHint<T> hint || !hint.Contains(_excluded)) { return bound; }

            return Math.Max(0, count - 1);
        }
    }

    /// <summary>Whether <paramref name="value" /> is one the operand could draw and not the excluded one.</summary>
    bool ICardinalityHint<T>.Contains(T value) {
        return !EqualityComparer<T>.Default.Equals(value, _excluded) && _underlying is ICardinalityHint<T> hint && hint.Contains(value);
    }

    /// <inheritdoc />
    public T Generate() {
        for (int attempt = 0; attempt < RedrawBudget; attempt++) {
            T drawn = _underlying.Generate();
            if (!EqualityComparer<T>.Default.Equals(drawn, _excluded)) { return drawn; }
        }

        throw Exhausted();
    }

    private AnyGenerationException Exhausted() {
        Replay? replay = null;
        if (_source is not null) { replay = _drawsOnlyFromSource ? Replay.Of(_source) : Replay.PartialOf(_source); }

        string message = $"Could not draw a value different from {AnyDerivation.Display(_excluded)}: {RedrawBudget.ToString(System.Globalization.CultureInfo.InvariantCulture)} draws in a row produced it. " +
                         "The redraw is bounded, so this is an exhausted budget rather than a proof — though the usual cause is a generator that yields nothing else, such as one pinned to that value.";
        if (replay is not null) { message += $" {replay.Guidance}"; }

        return new AnyGenerationException(message, replay?.Seed);
    }

}
