namespace JustDummies;

/// <summary>
///     What <see cref="AnyExtensions.DifferentFrom{T}" /> and <see cref="AnyExtensions.Except{T}(IAny{T}, IEnumerable{T})" />
///     return: a generator redrawing while its operand yields an excluded value — the <b>rejective</b> exclusion,
///     available on any <see cref="IAny{T}" />. It sees nothing before <see cref="Generate" />; a draw that keeps
///     producing excluded values exhausts one bounded redraw and fails with its seed (ADR-0121, ADR-0133).
/// </summary>
/// <typeparam name="T">The type of the generated values.</typeparam>
internal sealed class ExclusionAny<T> : IAny<T>, IHasRandomSource, IReproducibilityHint, ICardinalityHint<T> {

    #region Statics members declarations

    /// <summary>
    ///     How many draws in a row may produce an excluded value before the redraw gives up. A generator that yields
    ///     excluded values this often yields nothing else in practice — a pinned or single-valued one — and a larger
    ///     budget would only delay the report.
    /// </summary>
    internal const int RedrawBudget = 1_000;

    #endregion

    #region Fields declarations

    private readonly bool          _drawsOnlyFromSource;
    private readonly IReadOnlyList<T> _excluded;
    private readonly HashSet<T>    _excludedSet;
    private readonly RandomSource? _source;
    private readonly IAny<T>       _underlying;

    #endregion

    /// <summary>
    ///     Excludes <paramref name="excluded" />, compared with <see cref="EqualityComparer{T}.Default" />. The values are
    ///     kept once each, in the order given, so a message lists them as the caller wrote them.
    /// </summary>
    internal ExclusionAny(IAny<T> underlying, IEnumerable<T> excluded) {
        if (underlying is null) { throw new ArgumentNullException(nameof(underlying)); }
        if (excluded is null) { throw new ArgumentNullException(nameof(excluded)); }

        List<T> distinct = excluded.Distinct(EqualityComparer<T>.Default).ToList();

        _underlying          = underlying;
        _excluded            = distinct;
        _excludedSet         = new HashSet<T>(distinct, EqualityComparer<T>.Default);
        _source              = AnyDerivation.SourceOf(underlying);
        _drawsOnlyFromSource = AnyDerivation.IsReproducible(underlying);
    }

    RandomSource? IHasRandomSource.Source => _source;

    bool IReproducibilityHint.DrawsOnlyFromSource => _drawsOnlyFromSource;

    /// <summary>The operand's bound, less every excluded value the operand could draw.</summary>
    long? ICardinalityHint<T>.DistinctCardinality {
        get {
            long? bound = AnyDerivation.CardinalityOf(_underlying);
            if (bound is not long count || _underlying is not ICardinalityHint<T> hint) { return bound; }

            return Math.Max(0, count - _excluded.Count(hint.Contains));
        }
    }

    /// <summary>Whether <paramref name="value" /> is one the operand could draw and not an excluded one.</summary>
    bool ICardinalityHint<T>.Contains(T value) {
        return !_excludedSet.Contains(value) && _underlying is ICardinalityHint<T> hint && hint.Contains(value);
    }

    /// <inheritdoc />
    public T Generate() {
        if (TryFindTheOnlyValue(out T only)) { throw OnlyAnExcludedValue(only); }

        for (int attempt = 0; attempt < RedrawBudget; attempt++) {
            T drawn = _underlying.Generate();
            if (!_excludedSet.Contains(drawn)) { return drawn; }
        }

        throw Exhausted();
    }

    /// <summary>
    ///     Whether the operand is known to yield one value and nothing else, and that value is excluded: a domain of one
    ///     value that holds it. Both answers are given under the default equality, the one this exclusion compares with,
    ///     so no redraw could ever succeed — a certainty rather than a budget spent (issue #432).
    /// </summary>
    private bool TryFindTheOnlyValue(out T only) {
        only = default!;
        if (_underlying is not ICardinalityHint<T> hint || hint.DistinctCardinality != 1) { return false; }

        foreach (T value in _excluded) {
            if (!hint.Contains(value)) { continue; }

            only = value;

            return true;
        }

        return false;
    }

    /// <summary>
    ///     The definite refusal. It carries no seed: nothing was drawn to decide it, and any seed would refuse the same.
    /// </summary>
    private AnyGenerationException OnlyAnExcludedValue(T only) {
        return _excluded.Count == 1
                   ? new AnyGenerationException($"Could not draw a value different from {AnyDerivation.Display(only)}: the generator yields only that value.")
                   : new AnyGenerationException($"Could not draw a value outside {DisplayExcluded()}: the generator yields only {AnyDerivation.Display(only)}.");
    }

    private AnyGenerationException Exhausted() {
        Replay? replay = null;
        if (_source is not null) { replay = _drawsOnlyFromSource ? Replay.Of(_source) : Replay.PartialOf(_source); }

        string budget = RedrawBudget.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string message = _excluded.Count == 1
                             ? $"Could not draw a value different from {AnyDerivation.Display(_excluded[0])}: {budget} draws in a row produced it. " +
                               "The redraw is bounded, so this is an exhausted budget rather than a proof — though the usual cause is a generator that yields nothing else, such as one pinned to that value."
                             : $"Could not draw a value outside {DisplayExcluded()}: {budget} draws in a row produced one of them. " +
                               "The redraw is bounded, so this is an exhausted budget rather than a proof — though the usual cause is a generator that yields nothing else.";
        if (replay is not null) { message += $" {replay.Guidance}"; }

        return new AnyGenerationException(message, replay?.Seed);
    }

    private string DisplayExcluded() {
        return "{" + string.Join(", ", _excluded.Select(value => AnyDerivation.Display(value))) + "}";
    }

}
