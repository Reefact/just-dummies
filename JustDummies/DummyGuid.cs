#region Usings declarations

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace JustDummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="Guid" /> values, drawn from the seedable source — unlike
///     <see cref="Guid.NewGuid" />, a generated identifier is reproducible inside an
///     <c>Dummy.Reproducibly(...)</c> run. An unconstrained draw is, for every practical purpose, never
///     <see cref="Guid.Empty" />; chain <see cref="NonEmpty" /> to make that requirement explicit, or
///     <see cref="Empty" /> to pin the empty identifier. Contradictory constraints fail eagerly with a
///     <see cref="ConflictingDummyConstraintException" /> naming both sides.
/// </summary>
public sealed class DummyGuid : IDummy<Guid>, IHasRandomSource, ICardinalityHint<Guid>, IPoolInspection<Guid> {

    /// <summary>How many bytes a <see cref="Guid" /> is made of — its 128 bits, which a draw fills whole.</summary>
    private const int GuidByteCount = 16;

    /// <summary>How many values a pin leaves producible — the one it fixed.</summary>
    private const int PinnedCardinality = 1;

    #region Statics members declarations

    internal static DummyGuid Create(RandomSource source) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

        return new DummyGuid(source, null, null, null, null, [], []);
    }

    private static string V(Guid value) {
        return value.ToString("D");
    }

    private static string Join(Guid[] values) {
        return string.Join(", ", values.Select(V));
    }

    // Increments the 16-byte buffer by one with carry, from the last byte down — the full-width successor of
    // new Guid(bytes). Incrementing only the last byte (the former behaviour) wraps 255 back to 0 and can cycle
    // forever when every last-byte variant of a prefix is excluded; propagating the carry into the higher bytes
    // cannot, because it walks distinct values across the whole 128-bit space.
    private static void Increment(byte[] bytes) {
        for (int i = bytes.Length - 1; i >= 0; i--) {
            if (++bytes[i] != 0) { return; }
        }
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<Guid>? _allowed;
    private readonly ConstraintCall?      _allowedConstraint;
    private readonly List<Guid>?          _effectiveAllowed;
    private readonly IReadOnlyList<Guid>  _excluded;
    // Provenance for the diagnostic path only: _excluded and _excludedSet drive every draw decision, while this
    // records WHICH constraint contributed which values, so an exhausted pool or a forbidden pin can name the
    // exclusion responsible. Same split as the interval engines (OrdinalIntervalSpec._exclusions).
    private readonly IReadOnlyList<(ConstraintCall Constraint, Guid[] Values)> _exclusions;
    private readonly HashSet<Guid>        _excludedSet;
    private readonly Guid?                _pinned;
    private readonly ConstraintCall?      _pinnedConstraint;
    private readonly RandomSource         _source;

    #endregion

    private DummyGuid(RandomSource source, Guid? pinned, ConstraintCall? pinnedConstraint,
                    IReadOnlyList<Guid>? allowed, ConstraintCall? allowedConstraint, IReadOnlyList<Guid> excluded,
                    IReadOnlyList<(ConstraintCall Constraint, Guid[] Values)> exclusions) {
        _source            = source;
        _pinned            = pinned;
        _pinnedConstraint  = pinnedConstraint;
        _allowed           = allowed;
        _allowedConstraint = allowedConstraint;
        _excluded          = excluded;
        _exclusions        = exclusions;
        // Materialized once here — "constrain once, draw many": Generate never refilters the allow-list, and
        // the exclusion walk tests membership against a set rather than rescanning the list on every step.
        _excludedSet       = [.. excluded];
        _effectiveAllowed  = allowed?.Where(value => !_excludedSet.Contains(value)).ToList();
    }

    RandomSource? IHasRandomSource.Source => _source;

    // Pinned to a single value, or bounded by an allow-list; otherwise the domain is effectively unbounded.
    long? ICardinalityHint<Guid>.DistinctCardinality => _pinned is not null ? PinnedCardinality : _effectiveAllowed?.Count;

    // Mirrors Generate: the pin, then the allow-list, then the full space minus the exclusions.
    bool ICardinalityHint<Guid>.Contains(Guid value) {
        if (_pinned is Guid pinned) { return pinned == value; }
        if (_effectiveAllowed is not null) { return _effectiveAllowed.Contains(value); }

        return !_excluded.Contains(value);
    }

    // Explicit, like the cardinality hint above (ADR-0067). A Guid generator without a pool draws from the whole
    // 128-bit space, which becomes the caller's business only once they hand over a list of their own.
    bool IPoolInspection<Guid>.IsPooled => _effectiveAllowed is not null;

    IReadOnlyList<Guid> IPoolInspection<Guid>.GetSurvivors() {
        if (_effectiveAllowed is null) { return Array.Empty<Guid>(); }
        // A pin short-circuits Generate before the allow-list is ever reached, so it is the whole drawable domain.
        // Reporting the rest as survivors would name values no draw can yield -- the one direction this feature
        // must never take.
        if (_pinned is Guid pinned) { return new ReadOnlyCollection<Guid>([pinned]); }

        return new ReadOnlyCollection<Guid>(_effectiveAllowed.ToArray());
    }

    IReadOnlyList<PoolRejection<Guid>> IPoolInspection<Guid>.GetRejections() {
        if (_allowed is null) { return Array.Empty<PoolRejection<Guid>>(); }

        List<PoolRejection<Guid>> rejections = [];
        foreach (Guid value in _allowed) {
            List<DeclaredConstraint> culprits = [];
            // A pooled value the pin displaces is refused as surely as an excluded one, and the pin is what to
            // loosen. Validated guarantees the pin is itself in the pool, so it is never its own culprit.
            if (_pinned is Guid pinned && value != pinned) { culprits.Add(_pinnedConstraint!.ToDeclaredConstraint()); }

            // Named ALONGSIDE the pin, never instead of it. This branch used to be skipped whenever the pin had
            // spoken, so a value both displaced and excluded was blamed on the pin alone -- a reader who dropped
            // the pin, the only remedy offered, found the value still absent. That is the misdirection
            // PoolRejection exists to prevent, and it must hold with a pin in force as it does without one.
            if (_excludedSet.Contains(value)) {
                culprits.AddRange(_exclusions
                                  .Where(entry => entry.Values.Contains(value))
                                  .Select(entry => entry.Constraint.ToDeclaredConstraint()));
            }

            if (culprits.Count == 0) { continue; }

            rejections.Add(new PoolRejection<Guid>(value, culprits));
        }

        return new ReadOnlyCollection<PoolRejection<Guid>>(rejections);
    }

    /// <summary>Requires an identifier different from <see cref="Guid.Empty" />.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyGuid NonEmpty() {
        return WithExcluded([Guid.Empty], ConstraintCall.Of(nameof(NonEmpty)));
    }

    /// <summary>Pins the identifier to <see cref="Guid.Empty" />.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyGuid Empty() {
        return Validated(new DummyGuid(_source, Guid.Empty, ConstraintCall.Of(nameof(Empty)), _allowed, _allowedConstraint, _excluded, _exclusions), ConstraintCall.Of(nameof(Empty)));
    }

    /// <summary>Requires the identifier to be one of the supplied values. Declared once per generator.</summary>
    /// <param name="values">The allowed values; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyGuid OneOf(params Guid[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        ConstraintCall constraint = ConstraintCall.Of(nameof(OneOf), Join(values));
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_allowedConstraint == constraint) { return this; }
        if (_allowedConstraint is not null) { throw ConflictingDummyConstraintException.AlreadyDefined(constraint, _allowedConstraint); }

        return Validated(new DummyGuid(_source, _pinned, _pinnedConstraint, values.Distinct().ToArray(), constraint, _excluded, _exclusions), constraint);
    }

    /// <summary>Requires the identifier to be none of the supplied values.</summary>
    /// <param name="values">The forbidden values.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyGuid Except(params Guid[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return WithExcluded(values, ConstraintCall.Of(nameof(Except), Join(values)));
    }

    /// <summary>
    ///     Requires the identifier to differ from <paramref name="value" /> — typically an existing value the test
    ///     already holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated identifier must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyGuid DifferentFrom(Guid value) {
        return WithExcluded([value], ConstraintCall.Of(nameof(DifferentFrom), V(value)));
    }

    /// <inheritdoc />
    public Guid Generate() {
        if (_pinned is Guid pinned) { return pinned; }

        SeededRandom random = _source.Current;
        if (_effectiveAllowed is not null) {
            return _effectiveAllowed[random.Next(_effectiveAllowed.Count)];
        }

        byte[] bytes = new byte[GuidByteCount];
        random.NextBytes(bytes);
        Guid candidate = new(bytes);
        // Colliding with an excluded identifier has probability |excluded| / 2^128 per draw. On a collision,
        // walk the whole 128-bit value with carry — the full-width successor of the drawn bytes — off the
        // excluded values. The exclusion set can never fill the 128-bit space, so the walk visits distinct
        // values until it lands on an allowed one and terminates: the same deterministic escape
        // OrdinalIntervalSpec and WideIntervalSpec already use for their 128-bit siblings.
        while (_excludedSet.Contains(candidate)) {
            Increment(bytes);
            candidate = new(bytes);
        }

        return candidate;
    }

    private DummyGuid WithExcluded(Guid[] values, ConstraintCall applying) {
        List<Guid>                                        excluded   = [.. _excluded, .. values];
        List<(ConstraintCall Constraint, Guid[] Values)>  exclusions = [.. _exclusions, (applying, values.ToArray())];

        return Validated(new DummyGuid(_source, _pinned, _pinnedConstraint, _allowed, _allowedConstraint, excluded, exclusions), applying);
    }

    [SuppressMessage(NetAnalyzersRule.CA1822.Category, NetAnalyzersRule.CA1822.Id, Justification = SuppressionJustification.CA1822.UniformValidatedHook)]
    [SuppressMessage(SonarRule.S2325.Category, SonarRule.S2325.Id, Justification = SuppressionJustification.S2325.UniformValidatedHook)]
    private DummyGuid Validated(DummyGuid candidate, ConstraintCall applying) {
        if (candidate._pinned is Guid pinned) {
            if (candidate._excluded.Contains(pinned)) {
                throw ConflictingDummyConstraintException.PinnedValueExcluded(applying, candidate._pinnedConstraint!, V(pinned),
                                                                            Forbids(candidate.ExcludingConstraintsFor(pinned), applying));
            }
            if (candidate._allowed is not null && !candidate._allowed.Contains(pinned)) {
                throw ConflictingDummyConstraintException.PinnedValueNotAllowed(applying, candidate._pinnedConstraint!, V(pinned), candidate._allowedConstraint!);
            }

            return candidate;
        }

        if (candidate._effectiveAllowed is not null && candidate._effectiveAllowed.Count == 0) {
            throw ConflictingDummyConstraintException.NoValueRemains(applying, candidate.DescribeExhaustion(applying));
        }

        return candidate;
    }

    /// <summary>
    ///     Why the allow-list is empty, naming the constraint that emptied it. An exclusion is what removes values,
    ///     so it is what the sentence must name; the allow-list is the victim, and naming it produced the
    ///     self-referential "no value OneOf(...) allows remains available" this replaces.
    /// </summary>
    private string DescribeExhaustion(ConstraintCall applying) {
        IReadOnlyList<ConstraintCall> culprits = ExcludingConstraintsInEffect();

        // No exclusion bit: reachable only if the allow-list was empty on arrival, which OneOf refuses. Kept as an
        // honest fallback rather than an assertion, since a message is not worth throwing a second exception over.
        if (culprits.Count == 0) { return $"no value {_allowedConstraint} allows remains available"; }

        return $"{Forbids(culprits, applying)} every value {_allowedConstraint} allows";
    }

    /// <summary>
    ///     The distinct exclusion constraints that actually caused the exhaustion — those forbidding at least one
    ///     value the allow-list would otherwise permit. An exclusion whose values were never allowed anyway did not
    ///     cause anything, so naming it would mislead; first-declared order is preserved.
    /// </summary>
    private IReadOnlyList<ConstraintCall> ExcludingConstraintsInEffect() {
        List<ConstraintCall> names = [];
        foreach ((ConstraintCall constraint, Guid[] values) in _exclusions) {
            if (names.Contains(constraint)) { continue; }
            if (values.Any(value => _allowed is null || _allowed.Contains(value))) { names.Add(constraint); }
        }

        return names;
    }

    /// <summary>The distinct exclusion constraints that forbid <paramref name="value" />, in first-declared order.</summary>
    private IReadOnlyList<ConstraintCall> ExcludingConstraintsFor(Guid value) {
        List<ConstraintCall> names = [];
        foreach ((ConstraintCall constraint, Guid[] values) in _exclusions) {
            if (names.Contains(constraint)) { continue; }
            if (values.Contains(value)) { names.Add(constraint); }
        }

        return names;
    }

    /// <summary>
    ///     The subject of a forbidding clause. A single culprit that is the constraint being applied becomes "it",
    ///     so the message reads "Cannot apply Except(x) because ... and it forbids it" rather than repeating the
    ///     constraint on both sides of "because".
    /// </summary>
    private static string Forbids(IReadOnlyList<ConstraintCall> names, ConstraintCall applying) {
        if (names.Count == 0) { return "the exclusions forbid"; }
        if (names.Count == 1) { return names[0] == applying ? "it forbids" : $"{names[0]} forbids"; }

        return $"{string.Join(", ", names)} forbid";
    }

}
