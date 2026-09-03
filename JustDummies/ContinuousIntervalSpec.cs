#region Usings declarations

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

#endregion

namespace JustDummies;

/// <summary>
///     The shared immutable engine behind the binary floating-point generators (<see cref="DummyDouble" />,
///     <see cref="DummySingle" />, and <c>Half</c> on modern targets): an inclusive interval of finite doubles, an
///     optional allow-list, and point exclusions — each bound remembering the constraint that set it, so a conflict
///     message can name both sides. NaN and the infinities are never generated nor accepted: arbitrary test values
///     should cross invariants, not sabotage arithmetic.
/// </summary>
/// <remarks>
///     <para>
///         Narrower value types ride the double engine through a <c>quantize</c> step (for example
///         <c>double → float</c>): bounds are supplied already-representable in the narrow type, sampling happens in
///         double, and the drawn value is quantized then clamped back into the bounds.
///     </para>
///     <para>
///         Excluding a point from a continuum can only collide with a draw on a set of measure zero, but the engine
///         still guarantees the constraint: a colliding draw is nudged to the nearest non-excluded representable
///         value — a bounded deterministic walk along the type's own ladder, ascending then descending from the
///         original draw, not a retry loop. When neither walk finds a free value within its budget the generation
///         fails with an <see cref="DummyGenerationException" /> naming the seed. That is an exhausted <i>local</i>
///         search, not a proof that the range holds no free value, and the message says so: free values further than
///         the budget from the drawn candidate are never examined.
///     </para>
/// </remarks>
internal sealed class ContinuousIntervalSpec {

    private const int NudgeBudget = 128;

    #region Statics members declarations

    /// <summary>
    ///     An unconstrained specification over <c>[domainMin, domainMax]</c>.
    /// </summary>
    /// <remarks>
    ///     The last argument is how the row picks a value in <c>[lower, upper]</c> from one unit sample, for a row
    ///     where uniformity over the reals is the wrong question — a narrow type whose representable values are
    ///     spaced geometrically. Omitted, which is the ordinary case, the draw stays uniform over the interval and
    ///     nothing about it changes.
    /// </remarks>
    internal static ContinuousIntervalSpec Unconstrained(string typeName, Func<double, string> render, Func<double, double> quantize, Func<double, double> nextUp, double domainMin, double domainMax,
                                                         Func<double, double, double, double>? laddered = null) {
        if (typeName is null) { throw new ArgumentNullException(nameof(typeName)); }
        if (render is null) { throw new ArgumentNullException(nameof(render)); }
        if (quantize is null) { throw new ArgumentNullException(nameof(quantize)); }
        if (nextUp is null) { throw new ArgumentNullException(nameof(nextUp)); }

        return new ContinuousIntervalSpec(typeName, render, quantize, nextUp, domainMin, null, domainMax, null, null, null, [], laddered);
    }

    /// <summary>
    ///     Rejects NaN and the infinities — the shared argument guard of every floating-point generator. The message
    ///     names the way out as well as the rule: a user meeting this wall wants a non-finite value for a reason, and
    ///     that reason is served by an explicit pool. Stating only the refusal leaves them concluding the library is
    ///     missing a feature it deliberately does not have.
    /// </summary>
    internal static void EnsureFinite(double value, string parameterName) {
        if (parameterName is null) { throw new ArgumentNullException(nameof(parameterName)); }
        if (double.IsNaN(value) || double.IsInfinity(value)) {
            throw new ArgumentException("The value must be finite: NaN and infinities are never generated, and never accepted as arguments. " +
                                        "To draw a non-finite value that genuinely belongs to your domain, use an explicit pool: Dummy.OneOf(double.NaN, ...).",
                                        parameterName);
        }
    }

    /// <summary>The next representable double above <paramref name="value" /> — the exclusive-bound arithmetic.</summary>
    internal static double NextUp(double value) {
        long bits = BitConverter.DoubleToInt64Bits(value);
        if (bits >= 0L) { bits++; } else if (bits == long.MinValue) { bits = 1L; } else { bits--; }

        return BitConverter.Int64BitsToDouble(bits);
    }

    /// <summary>The next representable double below <paramref name="value" />.</summary>
    internal static double NextDown(double value) {
        return -NextUp(-value);
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<double>? _allowed;
    private readonly ConstraintCall?        _allowedConstraint;
    private readonly List<double>?          _effectiveAllowed;
    private readonly IReadOnlyList<double>  _excluded;
    private readonly IReadOnlyList<(ConstraintCall Constraint, double[] Ordinals)> _exclusions;
    private readonly Func<double, double, double, double>? _laddered;
    private readonly Func<double, double>   _nextUp;
    private readonly double                 _max;
    private readonly ConstraintCall?        _maxConstraint;
    private readonly double                 _min;
    private readonly ConstraintCall?        _minConstraint;
    private readonly Func<double, double>   _quantize;
    private readonly Func<double, string>   _render;
    private readonly string                 _typeName;

    #endregion

    [SuppressMessage(SonarRule.S107.Category, SonarRule.S107.Id, Justification = SuppressionJustification.S107.EngineImmutableState)]
    private ContinuousIntervalSpec(string  typeName, Func<double, string> render, Func<double, double> quantize, Func<double, double> nextUp,
                                   double  min,      ConstraintCall? minConstraint,
                                   double  max,      ConstraintCall? maxConstraint,
                                   IReadOnlyList<double>? allowed, ConstraintCall? allowedConstraint,
                                   IReadOnlyList<(ConstraintCall Constraint, double[] Ordinals)> exclusions,
                                   Func<double, double, double, double>? laddered) {
        _typeName          = typeName;
        _render            = render;
        _quantize          = quantize;
        _nextUp            = nextUp;
        _laddered          = laddered;
        _min               = min;
        _minConstraint     = minConstraint;
        _max               = max;
        _maxConstraint     = maxConstraint;
        _allowed           = allowed;
        _allowedConstraint = allowedConstraint;
        _exclusions        = exclusions;
        // The flat value set drives every draw-time decision; the provenance in _exclusions is consulted only
        // when a conflict message must name the excluding constraint. Materialized once — "constrain once, draw many".
        _excluded          = exclusions.SelectMany(pair => pair.Ordinals).ToList();
        _effectiveAllowed  = allowed?.Where(value => value >= min && value <= max && !IsExcluded(value)).ToList();
    }

    /// <summary>Tightens the lower bound; a looser bound than the current one is a no-op.</summary>
    internal ContinuousIntervalSpec WithMinimum(double minimum, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (double.IsInfinity(minimum)) { throw ConflictingDummyConstraintException.NoValueSatisfies(applying, _typeName); }
        if (minimum <= _min) { return this; }

        if (minimum > _max) {
            if (_maxConstraint is null) { throw ConflictingDummyConstraintException.NoValueSatisfies(applying, _typeName); }

            throw ConflictingDummyConstraintException.AlreadyBoundedAbove(applying, _maxConstraint, _render(_max));
        }

        return Validated(new ContinuousIntervalSpec(_typeName, _render, _quantize, _nextUp, minimum, applying, _max, _maxConstraint, _allowed, _allowedConstraint, _exclusions, _laddered), applying);
    }

    /// <summary>Tightens the upper bound; a looser bound than the current one is a no-op.</summary>
    internal ContinuousIntervalSpec WithMaximum(double maximum, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (double.IsInfinity(maximum)) { throw ConflictingDummyConstraintException.NoValueSatisfies(applying, _typeName); }
        if (maximum >= _max) { return this; }

        if (maximum < _min) {
            if (_minConstraint is null) { throw ConflictingDummyConstraintException.NoValueSatisfies(applying, _typeName); }

            throw ConflictingDummyConstraintException.AlreadyBoundedBelow(applying, _minConstraint, _render(_min));
        }

        return Validated(new ContinuousIntervalSpec(_typeName, _render, _quantize, _nextUp, _min, _minConstraint, maximum, applying, _allowed, _allowedConstraint, _exclusions, _laddered), applying);
    }

    /// <summary>Tightens the lower bound to strictly above <paramref name="bound" /> — via the type's next representable value.</summary>
    internal ContinuousIntervalSpec WithMinimumAbove(double bound, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }

        return WithMinimum(_nextUp(bound), applying);
    }

    /// <summary>Tightens the upper bound to strictly below <paramref name="bound" /> — via the type's next representable value.</summary>
    internal ContinuousIntervalSpec WithMaximumBelow(double bound, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }

        return WithMaximum(-_nextUp(-bound), applying);
    }

    /// <summary>Restricts the domain to an explicit allow-list; declared once per generator.</summary>
    internal ContinuousIntervalSpec WithAllowed(double[] values, ConstraintCall applying) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_allowedConstraint == applying) { return this; }
        if (_allowedConstraint is not null) { throw ConflictingDummyConstraintException.AlreadyDefined(applying, _allowedConstraint); }

        double[] distinct = values.Distinct().ToArray();

        return Validated(new ContinuousIntervalSpec(_typeName, _render, _quantize, _nextUp, _min, _minConstraint, _max, _maxConstraint, distinct, applying, _exclusions, _laddered), applying);
    }

    /// <summary>Adds values the generator must never produce.</summary>
    internal ContinuousIntervalSpec WithExcluded(double[] values, ConstraintCall applying) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }

        // The applied constraint tags its own values, so a later exhaustion message can name the exclusion
        // that actually emptied the domain rather than a bound that merely happens to border it.
        List<(ConstraintCall Constraint, double[] Ordinals)> exclusions = [.. _exclusions, (applying, values)];

        return Validated(new ContinuousIntervalSpec(_typeName, _render, _quantize, _nextUp, _min, _minConstraint, _max, _maxConstraint, _allowed, _allowedConstraint, exclusions, _laddered), applying);
    }

    /// <summary>
    ///     The number of distinct values the specification can produce — the allow-list size when one is set, <c>1</c>
    ///     for a validated pin (<c>_min == _max</c>, a singleton domain), and <c>null</c> otherwise: a floating-point
    ///     range is treated as a continuum (counting its representable values is a type-specific concern the shared
    ///     engine does not carry), so it stays outside the eager cardinality perimeter and a distinct collection over
    ///     it falls back to the bounded draw. Feeds <see cref="ICardinalityHint{T}" />.
    /// </summary>
    [SuppressMessage(SonarRule.S1244.Category, SonarRule.S1244.Id, Justification = SuppressionJustification.S1244.BoundsAreValidatedNotMeasured)]
    internal long? Cardinality {
        get {
            if (_effectiveAllowed is not null) { return _effectiveAllowed.Count; }
            if (_min == _max) { return 1; }

            return null;
        }
    }

    /// <summary>
    ///     The declared interval, for the owner that knows how to count its own representable values — the
    ///     type-specific concern <see cref="Cardinality" /> declines to carry. A 16-bit row holds few enough of them
    ///     that the difference is observable; the wider rows dwarf every cap the collections apply, so only the
    ///     narrow one asks.
    /// </summary>
    internal double Min => _min;

    /// <inheritdoc cref="Min" />
    internal double Max => _max;

    /// <summary>
    ///     Whether <paramref name="value" /> is one the specification could produce — a member of the allow-list when
    ///     one is set, otherwise inside the interval and not excluded. Non-finite inputs fall outside the bounds and
    ///     so return <c>false</c>. Mirrors <see cref="Generate" />'s own domain.
    /// </summary>
    internal bool Contains(double value) {
        if (_effectiveAllowed is not null) { return _effectiveAllowed.Contains(value); }

        return value >= _min && value <= _max && !IsExcluded(value);
    }

    /// <summary>
    ///     Whether an allow-list is in force — the caller supplied the values, and there is a pool to report on. An
    ///     interval has no pool: its members are the engine's, not the caller's, and here they are not even countable
    ///     (ADR-0067). Feeds <see cref="IPoolInspection{T}.IsPooled" />.
    /// </summary>
    internal bool IsPooled => _effectiveAllowed is not null;

    /// <summary>
    ///     The supplied values satisfying every declared constraint, in the order they were supplied, projected back
    ///     to the caller's own type by <paramref name="project" />.
    /// </summary>
    internal IReadOnlyList<T> GetSurvivors<T>(Func<double, T> project) {
        if (project is null) { throw new ArgumentNullException(nameof(project)); }

        return _effectiveAllowed is null
                   ? Array.Empty<T>()
                   : new ReadOnlyCollection<T>(_effectiveAllowed.Select(project).ToArray());
    }

    /// <summary>
    ///     The supplied values no draw can yield, in the order they were supplied, each with the declared constraints
    ///     refusing it — derived from the same declarations the allow-list filter is built from.
    /// </summary>
    internal IReadOnlyList<PoolRejection<T>> GetRejections<T>(Func<double, T> project) {
        if (project is null) { throw new ArgumentNullException(nameof(project)); }
        if (_allowed is null) { return Array.Empty<PoolRejection<T>>(); }

        List<PoolRejection<T>> rejections = [];
        foreach (double value in _allowed) {
            if (Admits(value)) { continue; }

            List<DeclaredConstraint> culprits = DeclaredConstraints()
                                                .Where(entry => !entry.Admits(value))
                                                .Select(entry => entry.Constraint.ToDeclaredConstraint())
                                                .ToList();

            rejections.Add(new PoolRejection<T>(project(value), culprits));
        }

        return new ReadOnlyCollection<PoolRejection<T>>(rejections);
    }

    /// <summary>Whether <paramref name="value" /> satisfies every declared constraint — the allow-list filter.</summary>
    private bool Admits(double value) {
        return DeclaredConstraints().All(entry => entry.Admits(value));
    }

    /// <summary>
    ///     Every declared constraint paired with the test a value must pass to satisfy it, grouped by the constraint
    ///     as the caller wrote it and conjoined — <c>Between</c> sets two bounds under one name, and the caller can
    ///     only loosen the call.
    /// </summary>
    private IEnumerable<(ConstraintCall Constraint, Func<double, bool> Admits)> DeclaredConstraints() {
        return Declarations()
               .GroupBy(entry => entry.Constraint)
               .Select(group => {
                   Func<double, bool>[] tests = group.Select(entry => entry.Admits).ToArray();

                   return (group.Key, (Func<double, bool>)(value => tests.All(test => test(value))));
               });
    }

    private IEnumerable<(ConstraintCall Constraint, Func<double, bool> Admits)> Declarations() {
        if (_minConstraint is not null) { yield return (_minConstraint, value => value >= _min); }
        if (_maxConstraint is not null) { yield return (_maxConstraint, value => value <= _max); }
        foreach ((ConstraintCall constraint, double[] excluded) in _exclusions) {
            yield return (constraint, value => !excluded.Contains(value));
        }
    }

    /// <summary>Draws one value satisfying the whole specification.</summary>
    [SuppressMessage(SonarRule.S1244.Category, SonarRule.S1244.Id, Justification = SuppressionJustification.S1244.ExactEqualityDetectsThePin)]
    internal double Generate(RandomSource source) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

        SeededRandom random = source.Current;

        if (_effectiveAllowed is not null) {
            return _effectiveAllowed[random.Next(_effectiveAllowed.Count)];
        }

        if (_min == _max) { return _min; }

        // Draw from the ordinary window rather than the declared interval (ADR-0031): the window only ever clips,
        // and it steps aside entirely when it would leave the declared interval empty — a caller who asked for a
        // magnitude gets it, a caller who merely permitted one does not.
        double lower = Math.Max(_min, -OrdinaryMagnitude.AsDouble);
        double upper = Math.Min(_max, OrdinaryMagnitude.AsDouble);
        if (lower > upper) {
            lower = _min;
            upper = _max;
        }

        // One unit sample either way, so the two paths consume the seed identically and only the VALUE a laddered
        // row produces differs from what it would have produced uniformly (ADR-0049 pins draws consumed as well as
        // values, and a row switching path must not shift what follows it in the same scope).
        double unit = random.NextDouble();

        // Sample around the midpoint so the span (max - min) never overflows to infinity on wide ranges.
        double mid       = lower / 2 + upper / 2;
        double half      = upper / 2 - lower / 2;
        double candidate = Quantized(_laddered is null ? mid + (2 * unit - 1) * half : _laddered(lower, upper, unit));

        // A draw colliding with an excluded point (a measure-zero event) is nudged to the nearest
        // non-excluded representable neighbour: ascending first, then descending from the original draw
        // when the ascending walk leaves the bounds. Both walks step with the type-aware ladder (_nextUp),
        // so on the narrow types a step lands on the next value of their own type instead of stalling on a
        // sub-ulp double step that re-quantizes to the same value.
        double? free = NudgeToFree(candidate, ascending: true) ?? NudgeToFree(candidate, ascending: false);
        if (free is null) {
            // The inner exception states what was actually established. Both walks are bounded, so their failure
            // means the neighbourhood was exhausted — not that the range holds no free value, which nothing here
            // examined. Reporting the stronger claim would send a caller looking for a contradiction that may not
            // exist, and the shape that reaches this line (a wide range whose free values sit further than the
            // budget from the draw) is precisely the one where it would not.
            throw DummyGenerationException.LocalSearchExhausted(_typeName, Replay.Of(source, random.Seed), NudgeBudget);
        }

        return free.Value;
    }

    /// <summary>
    ///     Walks from <paramref name="from" /> along the type's representable ladder — ascending or descending — to the
    ///     nearest value the exclusions allow, staying within the bounds. Returns <c>null</c> when the walk reaches a
    ///     bound <b>or spends its <see cref="NudgeBudget" /></b> before finding one, so the caller can try the opposite
    ///     direction. Both directions returning <c>null</c> therefore means the neighbourhood is exhausted, which is
    ///     weaker than the range being empty: only the budget was searched.
    /// </summary>
    private double? NudgeToFree(double from, bool ascending) {
        double candidate = from;
        int    budget    = NudgeBudget;
        while (IsExcluded(candidate)) {
            // The type-aware next-up / next-down: -_nextUp(-x) mirrors the ascending step onto the descending ladder.
            double next = ascending ? _nextUp(candidate) : -_nextUp(-candidate);
            if (next < _min || next > _max || budget-- == 0) { return null; }

            candidate = Quantized(next);
        }

        return candidate;
    }

    private double Quantized(double value) {
        double quantized = _quantize(value);
        if (quantized < _min) { return _min; }
        if (quantized > _max) { return _max; }

        return quantized;
    }

    [SuppressMessage(SonarRule.S1244.Category, SonarRule.S1244.Id, Justification = SuppressionJustification.S1244.ExclusionMembershipIsExact)]
    private bool IsExcluded(double value) {
        return _excluded.Any(excluded => value.Equals(excluded));
    }

    [SuppressMessage(NetAnalyzersRule.CA1822.Category, NetAnalyzersRule.CA1822.Id, Justification = SuppressionJustification.CA1822.UniformValidatedHook)]
    [SuppressMessage(SonarRule.S2325.Category, SonarRule.S2325.Id, Justification = SuppressionJustification.S2325.UniformValidatedHook)]
    private ContinuousIntervalSpec Validated(ContinuousIntervalSpec candidate, ConstraintCall applying) {
        if (candidate.IsSatisfiable()) { return candidate; }

        throw ConflictingDummyConstraintException.NoValueRemains(applying, candidate.DescribeExhaustion(applying));
    }

    private bool IsSatisfiable() {
        if (_effectiveAllowed is not null) { return _effectiveAllowed.Count > 0; }
        if (_min < _max) { return true; }

        return !IsExcluded(_min);
    }

    private string DescribeExhaustion(ConstraintCall applying) {
        IReadOnlyList<ConstraintCall> culprits = ExcludingConstraintsInEffect();

        if (_allowed is not null) {
            if (culprits.Count == 0) { return $"none of the values {_allowedConstraint} allows satisfies the constraints already defined"; }

            // Only the allow-list values the bounds still permit can be forbidden by an exclusion; if some allowed
            // value was already dropped by a bound, the exclusions do not forbid "every" allowed value, so the claim
            // is qualified rather than overstated.
            string allowed = _allowed.All(WouldAllowIgnoringExclusions)
                                 ? $"every value {_allowedConstraint} allows"
                                 : $"every value {_allowedConstraint} allows that the other constraints leave";

            return $"{Forbids(culprits, applying)} {allowed}";
        }

        if (culprits.Count == 0) {
            string pinning = _minConstraint?.ToString() ?? _maxConstraint?.ToString() ?? "the declared bounds";

            return $"{pinning} already pins the value to {_render(_min)}, which the exclusions forbid";
        }

        return $"{Forbids(culprits, applying)} {_render(_min)}, {PinningClause()}";
    }

    /// <summary>
    ///     The distinct exclusion constraints that actually caused the exhaustion — those forbidding at least one
    ///     value the interval and allow-list would otherwise permit. An exclusion whose values fall outside the
    ///     surviving domain never bit, so naming it would mislead; first-declared order is preserved.
    /// </summary>
    private IReadOnlyList<ConstraintCall> ExcludingConstraintsInEffect() {
        List<ConstraintCall> names = [];
        foreach ((ConstraintCall constraint, double[] values) in _exclusions) {
            if (names.Contains(constraint)) { continue; }
            if (values.Any(WouldAllowIgnoringExclusions)) { names.Add(constraint); }
        }

        return names;
    }

    /// <summary>Whether <paramref name="value" /> would be in the domain if no exclusion were applied.</summary>
    private bool WouldAllowIgnoringExclusions(double value) {
        if (_allowed is not null && !_allowed.Contains(value)) { return false; }

        return value >= _min && value <= _max;
    }

    /// <summary>
    ///     The subject of the exhaustion clause. A single culprit that is the constraint being applied becomes "it",
    ///     so the message reads "Cannot apply DifferentFrom(1) because it forbids …" rather than repeating the
    ///     constraint on both sides of "because".
    /// </summary>
    private static string Forbids(IReadOnlyList<ConstraintCall> names, ConstraintCall applying) {
        if (names.Count == 1) { return names[0] == applying ? "it forbids" : $"{names[0]} forbids"; }

        return $"{string.Join(", ", names)} forbid";
    }

    /// <summary>Names the bounds that pinned the domain to its single value, for the "forbids X, the only value ... leaves" form.</summary>
    private string PinningClause() {
        List<ConstraintCall> bounds = [];
        if (_minConstraint is not null) { bounds.Add(_minConstraint); }
        if (_maxConstraint is not null && _maxConstraint != _minConstraint) { bounds.Add(_maxConstraint); }

        if (bounds.Count == 0) { return "the only value the declared bounds leave"; }
        if (bounds.Count == 1) { return $"the only value {bounds[0]} leaves"; }

        return $"the only value {string.Join(" and ", bounds)} leave";
    }

}
