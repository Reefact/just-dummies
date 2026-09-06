#region Usings declarations

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace JustDummies;

/// <summary>
///     The immutable engine behind <see cref="AnyDecimal" /> — the same algebra as
///     <see cref="ContinuousIntervalSpec" /> in <see cref="decimal" /> arithmetic. <see cref="decimal" /> has no
///     next-representable-value ladder, so exclusive bounds are expressed as an inclusive bound plus a point
///     exclusion, and a colliding draw is nudged by the smallest decimal increment within a bounded budget. An
///     optional <b>scale lattice</b> (set by <c>WithScale</c>) restricts the domain to the multiples of
///     <c>10^-scale</c> — every value expressible in <c>scale</c> decimal places — by snapping the drawn candidate to
///     the grid, still in one constructive draw.
/// </summary>
internal sealed class DecimalIntervalSpec {

    private const int NoScale     = -1;
    private const int NudgeBudget = 128;

    /// <summary>The most decimal places a <see cref="decimal" /> carries — the widest scale its 96-bit mantissa allows.</summary>
    internal const int MaxScale = 28;

    /// <summary>How many bytes that mantissa spans: 96 bits, which <see cref="BitConverter" /> reads back as three limbs.</summary>
    private const int MantissaByteCount = 3 * sizeof(int);

    private static readonly decimal SmallestStep = 0.0000000000000000000000000001m;
    private static readonly decimal MaxFraction  = 7.9228162514264337593543950335m;

    #region Statics members declarations

    internal static DecimalIntervalSpec Unconstrained(string typeName, Func<decimal, string> render) {
        if (typeName is null) { throw new ArgumentNullException(nameof(typeName)); }
        if (render is null) { throw new ArgumentNullException(nameof(render)); }

        return new DecimalIntervalSpec(typeName, render, decimal.MinValue, null, decimal.MaxValue, null, null, null, [], NoScale, null);
    }

    /// <summary>Ten raised to <paramref name="power" /> as an exact <see cref="decimal" /> (<paramref name="power" /> in <c>[0, 28]</c>).</summary>
    private static decimal Pow10(int power) {
        decimal result = 1m;
        for (int i = 0; i < power; i++) { result *= 10m; }

        return result;
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<decimal>? _allowed;
    private readonly ConstraintCall?         _allowedConstraint;
    private readonly decimal                 _ceiledMin;
    private readonly List<decimal>?          _effectiveAllowed;
    private readonly IReadOnlyList<decimal>  _excluded;
    private readonly IReadOnlyList<(ConstraintCall Constraint, decimal[] Ordinals)> _exclusions;
    private readonly int                     _excludedOnLattice;
    private readonly decimal                 _flooredMax;
    private readonly bool                    _latticeHasPoint;
    private readonly decimal                 _max;
    private readonly ConstraintCall?         _maxConstraint;
    private readonly decimal                 _min;
    private readonly ConstraintCall?         _minConstraint;
    private readonly Func<decimal, string>   _render;
    private readonly int                     _scale;
    private readonly ConstraintCall?         _scaleConstraint;
    private readonly decimal                 _step;
    private readonly string                  _typeName;

    #endregion

    [SuppressMessage(SonarRule.S107.Category, SonarRule.S107.Id, Justification = SuppressionJustification.S107.EngineImmutableState)]
    private DecimalIntervalSpec(string  typeName, Func<decimal, string> render,
                                decimal min,      ConstraintCall? minConstraint,
                                decimal max,      ConstraintCall? maxConstraint,
                                IReadOnlyList<decimal>? allowed, ConstraintCall? allowedConstraint,
                                IReadOnlyList<(ConstraintCall Constraint, decimal[] Ordinals)> exclusions,
                                int     scale,    ConstraintCall? scaleConstraint) {
        _typeName          = typeName;
        _render            = render;
        _min               = min;
        _minConstraint     = minConstraint;
        _max               = max;
        _maxConstraint     = maxConstraint;
        _allowed           = allowed;
        _allowedConstraint = allowedConstraint;
        _exclusions        = exclusions;
        _scale             = scale;
        _scaleConstraint   = scaleConstraint;
        // The flat value set drives every draw-time decision; the provenance in _exclusions is consulted only
        // when a conflict message must name the excluding constraint. Materialized once — "constrain once, draw
        // many" — and DEDUPLICATED, like the ordinal engines': this engine COUNTS excluded grid points to decide
        // satisfiability and cardinality, so a value excluded twice (Except(x).DifferentFrom(x), or a duplicate
        // inside one call) must weigh once, or a satisfiable declaration is refused as exhausted.
        _excluded = exclusions.SelectMany(pair => pair.Ordinals).Distinct().ToList();
        // Lattice-derived state, materialized once — "constrain once, draw many".
        if (scale >= 0) {
            _step            = 1m / Pow10(scale);
            _ceiledMin       = CeilToGrid(min, scale, _step);
            _flooredMax      = FloorToGrid(max, scale, _step);
            _latticeHasPoint = _ceiledMin <= _flooredMax;
            _excludedOnLattice = _excluded.Count(value => value >= min && value <= max && IsOnGrid(value, scale));
        } else {
            _step              = 0m;
            _ceiledMin         = min;
            _flooredMax        = max;
            _latticeHasPoint   = true;
            _excludedOnLattice = 0;
        }
        // Materialized once here — "constrain once, draw many": Generate never refilters the allow-list.
        _effectiveAllowed = allowed?.Where(value => value >= min && value <= max && !IsExcluded(value) && (scale < 0 || IsOnGrid(value, scale))).ToList();
    }

    /// <summary>Tightens the lower bound; a looser bound than the current one is a no-op.</summary>
    internal DecimalIntervalSpec WithMinimum(decimal minimum, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (minimum <= _min) { return this; }

        if (minimum > _max) {
            if (_maxConstraint is null) { throw ConflictingAnyConstraintException.NoValueSatisfies(applying, _typeName); }

            throw ConflictingAnyConstraintException.AlreadyBoundedAbove(applying, _maxConstraint, _render(_max));
        }

        return Validated(new DecimalIntervalSpec(_typeName, _render, minimum, applying, _max, _maxConstraint, _allowed, _allowedConstraint, _exclusions, _scale, _scaleConstraint), applying);
    }

    /// <summary>Tightens the upper bound; a looser bound than the current one is a no-op.</summary>
    internal DecimalIntervalSpec WithMaximum(decimal maximum, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (maximum >= _max) { return this; }

        if (maximum < _min) {
            if (_minConstraint is null) { throw ConflictingAnyConstraintException.NoValueSatisfies(applying, _typeName); }

            throw ConflictingAnyConstraintException.AlreadyBoundedBelow(applying, _minConstraint, _render(_min));
        }

        return Validated(new DecimalIntervalSpec(_typeName, _render, _min, _minConstraint, maximum, applying, _allowed, _allowedConstraint, _exclusions, _scale, _scaleConstraint), applying);
    }

    /// <summary>Tightens the lower bound to strictly above <paramref name="bound" /> — the inclusive bound plus a point exclusion.</summary>
    internal DecimalIntervalSpec WithMinimumAbove(decimal bound, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }

        return WithMinimum(bound, applying).WithExcluded([bound], applying);
    }

    /// <summary>Tightens the upper bound to strictly below <paramref name="bound" /> — the inclusive bound plus a point exclusion.</summary>
    internal DecimalIntervalSpec WithMaximumBelow(decimal bound, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }

        return WithMaximum(bound, applying).WithExcluded([bound], applying);
    }

    /// <summary>Restricts the domain to an explicit allow-list; declared once per generator.</summary>
    internal DecimalIntervalSpec WithAllowed(decimal[] values, ConstraintCall applying) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_allowedConstraint == applying) { return this; }
        if (_allowedConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(applying, _allowedConstraint); }

        decimal[] distinct = values.Distinct().ToArray();

        return Validated(new DecimalIntervalSpec(_typeName, _render, _min, _minConstraint, _max, _maxConstraint, distinct, applying, _exclusions, _scale, _scaleConstraint), applying);
    }

    /// <summary>Adds values the generator must never produce.</summary>
    internal DecimalIntervalSpec WithExcluded(decimal[] values, ConstraintCall applying) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }

        // The applied constraint tags its own values, so a later exhaustion message can name the exclusion
        // that actually emptied the domain rather than a bound that merely happens to border it.
        List<(ConstraintCall Constraint, decimal[] Ordinals)> exclusions = [.. _exclusions, (applying, values)];

        return Validated(new DecimalIntervalSpec(_typeName, _render, _min, _minConstraint, _max, _maxConstraint, _allowed, _allowedConstraint, exclusions, _scale, _scaleConstraint), applying);
    }

    /// <summary>
    ///     Restricts the domain to the multiples of <c>10^-scale</c> — the values expressible in <paramref name="scale" />
    ///     decimal places. A value lattice, not a representation contract: the drawn value lies on the grid, but its
    ///     rendered form is not padded with trailing zeros. Declared once per generator.
    /// </summary>
    internal DecimalIntervalSpec WithScale(int scale, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (_scale >= 0) {
            if (_scale == scale) { return this; }

            // _scale and _scaleConstraint are written as a pair by the constructor and rethreaded as a pair by every
            // rebuild, so a declared scale always carries the name of the constraint that declared it.
            throw ConflictingAnyConstraintException.AlreadyDefined(applying, _scaleConstraint!);
        }

        return Validated(new DecimalIntervalSpec(_typeName, _render, _min, _minConstraint, _max, _maxConstraint, _allowed, _allowedConstraint, _exclusions, scale, applying), applying);
    }

    /// <summary>
    ///     The number of distinct values the specification can produce — the allow-list size when one is set; the number
    ///     of non-excluded grid points when a scale lattice is set and that count fits a <see cref="long" />; <c>1</c>
    ///     for a validated pin; and <c>null</c> otherwise (a wider <see cref="decimal" /> interval is a countable but
    ///     astronomically large domain, so it stays outside the eager cardinality perimeter and a distinct collection
    ///     over it falls back to the bounded draw). Feeds <see cref="ICardinalityHint{T}" />.
    /// </summary>
    internal long? Cardinality {
        get {
            if (_effectiveAllowed is not null) { return _effectiveAllowed.Count; }
            if (_scale >= 0) {
                long? points = LatticePointCount();

                return points is null ? null : Math.Max(0, points.Value - _excludedOnLattice);
            }
            if (_min == _max) { return 1; }

            return null;
        }
    }

    /// <summary>
    ///     Whether <paramref name="value" /> is one the specification could produce — a member of the allow-list when
    ///     one is set, otherwise on the grid (when a scale lattice is set), inside the interval and not excluded.
    ///     Mirrors <see cref="Generate" />'s own domain.
    /// </summary>
    internal bool Contains(decimal value) {
        if (_effectiveAllowed is not null) { return _effectiveAllowed.Contains(value); }
        if (_scale >= 0 && !IsOnGrid(value, _scale)) { return false; }

        return value >= _min && value <= _max && !IsExcluded(value);
    }

    /// <summary>
    ///     Whether an allow-list is in force — the caller supplied the values, and there is a pool to report on.
    ///     Deliberately <b>not</b> "the domain is countable": a plain interval has no pool, and reporting its members
    ///     would enumerate a range nobody supplied (ADR-0067). Feeds <see cref="IPoolInspection{T}.IsPooled" />.
    /// </summary>
    internal bool IsPooled => _effectiveAllowed is not null;

    /// <summary>
    ///     The supplied values satisfying every declared constraint, in the order they were supplied, projected back
    ///     to the caller's own type by <paramref name="project" />.
    /// </summary>
    internal IReadOnlyList<T> GetSurvivors<T>(Func<decimal, T> project) {
        if (project is null) { throw new ArgumentNullException(nameof(project)); }

        return _effectiveAllowed is null
                   ? Array.Empty<T>()
                   : new ReadOnlyCollection<T>(_effectiveAllowed.Select(project).ToArray());
    }

    /// <summary>
    ///     The supplied values no draw can yield, in the order they were supplied, each with the declared constraints
    ///     refusing it — derived from the same declarations the allow-list filter is built from.
    /// </summary>
    internal IReadOnlyList<PoolRejection<T>> GetRejections<T>(Func<decimal, T> project) {
        if (project is null) { throw new ArgumentNullException(nameof(project)); }
        if (_allowed is null) { return Array.Empty<PoolRejection<T>>(); }

        List<PoolRejection<T>> rejections = [];
        foreach (decimal value in _allowed) {
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
    private bool Admits(decimal value) {
        return DeclaredConstraints().All(entry => entry.Admits(value));
    }

    /// <summary>
    ///     Every declared constraint paired with the test a value must pass to satisfy it, grouped by the constraint
    ///     as the caller wrote it and conjoined — one call can set two bounds under one name, and the caller can only
    ///     loosen the call.
    /// </summary>
    private IEnumerable<(ConstraintCall Constraint, Func<decimal, bool> Admits)> DeclaredConstraints() {
        return Declarations()
               .GroupBy(entry => entry.Constraint)
               .Select(group => {
                   Func<decimal, bool>[] tests = group.Select(entry => entry.Admits).ToArray();

                   return (group.Key, (Func<decimal, bool>)(value => tests.All(test => test(value))));
               });
    }

    private IEnumerable<(ConstraintCall Constraint, Func<decimal, bool> Admits)> Declarations() {
        if (_minConstraint is not null) { yield return (_minConstraint, value => value >= _min); }
        if (_maxConstraint is not null) { yield return (_maxConstraint, value => value <= _max); }
        if (_scaleConstraint is not null && _scale >= 0) { yield return (_scaleConstraint, value => IsOnGrid(value, _scale)); }
        foreach ((ConstraintCall constraint, decimal[] excluded) in _exclusions) {
            yield return (constraint, value => !excluded.Contains(value));
        }
    }

    /// <summary>Draws one value satisfying the whole specification.</summary>
    internal decimal Generate(RandomSource source) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

        SeededRandom random = source.Current;

        if (_effectiveAllowed is not null) {
            return _effectiveAllowed[random.Next(_effectiveAllowed.Count)];
        }

        if (_min == _max) { return _min; }

        // A uniform fraction in [0, 1] over the full 96-bit mantissa scale. NextBytes fills all three
        // limbs — including each limb's top bit, which three non-negative Random.Next() draws would pin
        // to zero, capping the fraction near 0.5 and leaving the upper half of every range unreachable.
        byte[] mantissa = new byte[MantissaByteCount];
        random.NextBytes(mantissa);
        decimal fraction = new decimal(
            BitConverter.ToInt32(mantissa, 0),
            BitConverter.ToInt32(mantissa, sizeof(int)),
            BitConverter.ToInt32(mantissa, 2 * sizeof(int)),
            false, MaxScale) / MaxFraction;
        // Draw from the ordinary window rather than the declared interval (ADR-0031). Without it an unconstrained
        // decimal lands within a few decades of decimal.MaxValue, where a further multiplication throws
        // OverflowException and a scale constraint has no fractional digits left to constrain.
        (decimal lower, decimal upper) = DrawnInterval();

        decimal candidate = Clamped(Interpolated(lower, upper, fraction));

        if (_scale >= 0) {
            // Snap the draw onto the grid, then pull it inside the reachable grid window. A snapped point that
            // collides with an exclusion is walked one grid step at a time — ascending first, then descending —
            // a deterministic, bounded walk, not a retry loop.
            decimal snapped = Math.Round(candidate, _scale, MidpointRounding.ToEven);
            if (snapped < _ceiledMin) { snapped      = _ceiledMin; } else if (snapped > _flooredMax) { snapped = _flooredMax; }

            decimal? free = NudgeOnGrid(snapped, true) ?? NudgeOnGrid(snapped, false);
            if (free is null) {
                throw AnyGenerationException.GridNudgeExhausted(_typeName, Replay.Of(source, random.Seed));
            }

            return free.Value;
        }

        // A draw colliding with an excluded point is walked by the smallest decimal step — deterministic and
        // bounded, not a retry loop. (At extreme magnitudes the step can vanish in rounding; the budget then
        // fails the generation loudly instead of looping.)
        int budget = NudgeBudget;
        while (IsExcluded(candidate)) {
            decimal next = Clamped(candidate + SmallestStep);
            if (next == candidate || budget-- == 0) {
                throw AnyGenerationException.ExclusionNudgeExhausted(_typeName, Replay.Of(source, random.Seed));
            }

            candidate = next;
        }

        return candidate;
    }

    /// <summary>
    ///     The interval a draw is actually taken from — the <see cref="decimal" /> twin of
    ///     <c>ContinuousIntervalSpec.DrawnInterval</c>, branch for branch: the declared interval narrowed to the
    ///     ordinary magnitude (ADR-0031), and, where that would leave fewer than two values, a one-sided constraint
    ///     stays ordinary <b>around</b> the bound it declares, an explicitly bounded interval is drawn whole, and an
    ///     extraordinary one-sided bound falls back to the declared domain.
    /// </summary>
    /// <remarks>
    ///     What separates the second branch from the third is the <b>value</b> of the opposing bound, never whether a
    ///     constraint declared it: <c>Between(1e6m, decimal.MaxValue)</c> and <c>GreaterThanOrEqualTo(1e6m)</c>
    ///     denote the same set, so they must draw alike.
    /// </remarks>
    private (decimal Lower, decimal Upper) DrawnInterval() {
        decimal lower = Math.Max(_min, -OrdinaryMagnitude.AsDecimal);
        decimal upper = Math.Min(_max, OrdinaryMagnitude.AsDecimal);
        // Two or more values left: the ordinary case, and the one every existing draw already took.
        if (HoldsTwoDrawableValues(lower, upper)) { return (lower, upper); }

        // Past here the declared interval sits at or beyond the window's edge. A bound short of the type's own edge
        // is one the caller wrote and owns; a bound sitting exactly on that edge is the domain showing through.
        bool floorIsOwned   = _min > decimal.MinValue;
        bool ceilingIsOwned = _max < decimal.MaxValue;
        if (floorIsOwned && ceilingIsOwned) { return (_min, _max); }

        // One side is the domain: carry the window's width to the bound the caller did write rather than dropping it
        // and drawing the whole domain. The headroom is checked first — decimal arithmetic throws on overflow where
        // the binary types saturate, so the add cannot simply be attempted.
        decimal width = 2m * OrdinaryMagnitude.AsDecimal;
        if (floorIsOwned) {
            decimal top = _min <= decimal.MaxValue - width ? Math.Min(_min + width, _max) : _max;
            if (HoldsTwoDrawableValues(_min, top)) { return (_min, top); }
        } else if (ceilingIsOwned) {
            decimal bottom = _max >= decimal.MinValue + width ? Math.Max(_max - width, _min) : _min;
            if (HoldsTwoDrawableValues(bottom, _max)) { return (bottom, _max); }
        }

        // No ordinary slab exists beside the declared bound, so the declared interval is the honest answer. (Both
        // bounds on the domain edge cannot reach here — the window lies inside the decimal domain, so the first
        // branch returned.)
        return (_min, _max);
    }

    /// <summary>
    ///     Whether <c>[lower, upper]</c> holds two or more values a draw could actually land on — the question the
    ///     rule is phrased in, which two distinct endpoints do not answer once a scale lattice is in force.
    /// </summary>
    /// <remarks>
    ///     The grid is what the snap rounds a candidate onto, so an interval straddling a single grid point yields
    ///     one value however wide it reads: <c>Between(999_999.5m, 1_000_001m).WithScale(0)</c> narrows to
    ///     <c>[999_999.5, 1_000_000]</c>, whose only grid point is <c>1 000 000</c> — and the <c>1 000 001</c> the
    ///     caller declared becomes unreachable. Judging the numbers rather than the lattice left exactly the
    ///     singleton this window's rule exists to remove.
    /// </remarks>
    private bool HoldsTwoDrawableValues(decimal lower, decimal upper) {
        if (lower >= upper) { return false; }
        // Without a lattice the span holds astronomically many values, which no exclusion list can empty: a
        // decimal interval with distinct endpoints carries at least 10^20 of them at ordinary magnitudes.
        if (_scale < 0) { return true; }

        // Walk the grid from its first point, skipping what the exclusions forbid. Two survivors is the whole
        // question, and every step that finds none consumes one exclusion, so the walk is bounded by their count
        // however wide the interval — the grid itself is far too large to enumerate.
        decimal last      = FloorToGrid(upper, _scale, _step);
        int     survivors = 0;
        int     budget    = _excluded.Count + 2;
        for (decimal point = CeilToGrid(lower, _scale, _step); point <= last && budget-- > 0;) {
            if (!IsExcluded(point) && ++survivors == 2) { return true; }
            // Stop ON the last grid point rather than stepping past it. Where that point is decimal.MaxValue the
            // step does not merely leave the range, it throws — and the walk would then take a specification whose
            // only surviving value is the domain edge down with it, instead of returning here and falling back to
            // the declared interval.
            if (point >= last) { break; }

            decimal? next = NextDistinct(point, _step);
            if (next is null) { break; }

            point = next.Value;
        }

        return false;
    }

    /// <summary>
    ///     The point <paramref name="fraction" /> of the way from <paramref name="lower" /> to
    ///     <paramref name="upper" />, taken in whichever of two algebraically identical forms cannot leave the
    ///     <see cref="decimal" /> range for this interval's shape.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="decimal" /> throws where the binary types saturate, and it rounds every product to its
    ///         28-29 significant digits, so a form exact in algebra can still overflow. The two forms fail on
    ///         complementary shapes, and the interval's own sign says which one is safe here.
    ///     </para>
    ///     <para>
    ///         <b>Straddling zero</b> takes the convex combination. <c>upper - lower</c> is the operation that
    ///         overflows there — the full domain spans twice <see cref="decimal.MaxValue" /> — while the two
    ///         products carry opposite signs, so their sum can never accumulate past the larger of them. It is the
    ///         form the unconstrained draw was written for: the earlier midpoint shape is symmetric, so
    ///         <c>max / 2</c> rounds up and doubling it lands just past the edge.
    ///     </para>
    ///     <para>
    ///         <b>Both endpoints on one side of zero</b> measures an offset instead, because there it is the convex
    ///         combination that fails: two same-sign products, each rounded, whose sum lands past the edge about
    ///         once in a thousand draws on an interval within a unit of the domain. That is how
    ///         <c>GreaterThanOrEqualTo(decimal.MaxValue - 1m)</c> leaked a bare <c>OverflowException</c> —
    ///         intermittently, carrying no seed and naming no constraint.
    ///     </para>
    ///     <para>
    ///         The offset is anchored to the <b>nearer</b> endpoint, which is what makes the form total rather than
    ///         merely better. Anchoring always to <c>lower</c> leaves one hole: on a wide interval
    ///         <c>upper - lower</c> needs more significant digits than the type carries and rounds up, so
    ///         <c>lower + (upper - lower)</c> lands past <c>upper</c> — past <see cref="decimal.MaxValue" /> itself
    ///         where that is the ceiling. Measuring down from <c>upper</c> in the top half holds the multiplier to
    ///         half the width and makes both endpoints exact by construction rather than by cancellation. Measured
    ///         over 2.4 million interval and fraction pairs across the domain, both endpoint fractions included:
    ///         no overflow, and nothing outside <c>[lower, upper]</c>.
    ///     </para>
    /// </remarks>
    internal static decimal Interpolated(decimal lower, decimal upper, decimal fraction) {
        if (lower < 0m && upper > 0m) { return lower * (1m - fraction) + upper * fraction; }
        if (fraction <= 0.5m) { return lower + (upper - lower) * fraction; }

        return upper - (upper - lower) * (1m - fraction);
    }

    /// <summary>
    ///     The next value above <paramref name="from" /> that this walk can actually reach, or <c>null</c> where
    ///     none is representable.
    /// </summary>
    /// <remarks>
    ///     The scale quantum is what the snap rounds a candidate onto, but it is not always an increment the
    ///     representation can make: a <see cref="decimal" /> at a magnitude of 1e6 carries 22 decimal places, so
    ///     <c>WithScale(28)</c> asks for a step of <c>1e-28</c> that adding leaves the value exactly where it was.
    ///     Walking with it revisits one point until the budget runs out and reports a singleton that is not one —
    ///     which is how <c>Between(999_999m, 2_000_000m).WithScale(28).Except(999_999m)</c> lost its narrowing and
    ///     reached above a million. Where the requested quantum is finer than the representation, the values a draw
    ///     can land on are the representable ones, so the walk steps by the coarser of the two. A decimal's scale
    ///     runs from 0 to 28, so that many decades past the finest quantum always reach an increment the
    ///     representation can make: the loop is bounded by the type, never by the data.
    /// </remarks>
    private static decimal? NextDistinct(decimal from, decimal step) {
        decimal increment = step;
        for (int decade = 0; decade <= MaxScale; decade++) {
            // decimal arithmetic throws rather than saturating, so the headroom is checked before the add.
            if (from > 0m && increment > decimal.MaxValue - from) { return null; }

            decimal next = from + increment;
            if (next != from) { return next; }

            increment *= 10m;
        }

        return null;
    }

    /// <summary>
    ///     Walks from <paramref name="from" /> along the grid — ascending or descending by one step — to the nearest
    ///     value the exclusions allow, staying within the reachable grid window. Returns <c>null</c> when the walk
    ///     reaches the window edge before finding one, so the caller can try the opposite direction.
    /// </summary>
    private decimal? NudgeOnGrid(decimal from, bool ascending) {
        decimal candidate = from;
        int     budget    = NudgeBudget;
        while (IsExcluded(candidate)) {
            decimal next = ascending ? candidate + _step : candidate - _step;
            if (next < _ceiledMin || next > _flooredMax || budget-- == 0) { return null; }

            candidate = next;
        }

        return candidate;
    }

    /// <summary>The number of grid points in <c>[min, max]</c>, or <c>null</c> when that exceeds <see cref="long.MaxValue" />.</summary>
    private long? LatticePointCount() {
        if (!_latticeHasPoint) { return 0; }

        decimal maxCountable = _step * long.MaxValue; // _step is at most 1, so this never overflows
        // The span itself can exceed the decimal range (an unconstrained WithScale spans MinValue..MaxValue). Only a
        // straddling range risks that; when either half alone already outruns the countable span there are too many
        // points, so short-circuit before forming a difference that would throw.
        if (_ceiledMin < 0m && _flooredMax > 0m && (_flooredMax > maxCountable || -_ceiledMin > maxCountable)) { return null; }

        decimal span = _flooredMax - _ceiledMin;
        if (span > maxCountable) { return null; }

        return (long)(span / _step) + 1;
    }

    private static bool IsOnGrid(decimal value, int scale) {
        return Math.Round(value, scale, MidpointRounding.ToEven) == value;
    }

    /// <summary>The smallest grid point at or above <paramref name="value" />.</summary>
    private static decimal CeilToGrid(decimal value, int scale, decimal step) {
        decimal rounded = Math.Round(value, scale, MidpointRounding.ToEven);

        return rounded >= value ? rounded : rounded + step;
    }

    /// <summary>The largest grid point at or below <paramref name="value" />.</summary>
    private static decimal FloorToGrid(decimal value, int scale, decimal step) {
        decimal rounded = Math.Round(value, scale, MidpointRounding.ToEven);

        return rounded <= value ? rounded : rounded - step;
    }

    private decimal Clamped(decimal value) {
        if (value < _min) { return _min; }
        if (value > _max) { return _max; }

        return value;
    }

    private bool IsExcluded(decimal value) {
        return _excluded.Any(excluded => value == excluded);
    }

    [SuppressMessage(NetAnalyzersRule.CA1822.Category, NetAnalyzersRule.CA1822.Id, Justification = SuppressionJustification.CA1822.UniformValidatedHook)]
    [SuppressMessage(SonarRule.S2325.Category, SonarRule.S2325.Id, Justification = SuppressionJustification.S2325.UniformValidatedHook)]
    private DecimalIntervalSpec Validated(DecimalIntervalSpec candidate, ConstraintCall applying) {
        if (candidate.IsSatisfiable()) { return candidate; }

        throw ConflictingAnyConstraintException.NoValueRemains(applying, candidate.DescribeExhaustion(applying));
    }

    private bool IsSatisfiable() {
        if (_effectiveAllowed is not null) { return _effectiveAllowed.Count > 0; }
        if (_scale >= 0) {
            if (!_latticeHasPoint) { return false; }

            long? points = LatticePointCount();

            return points is null || points.Value > _excludedOnLattice;
        }
        if (_min < _max) { return true; }

        return !IsExcluded(_min);
    }

    [SuppressMessage(SonarRule.S125.Category, SonarRule.S125.Id, Justification = SuppressionJustification.S125.ProseNotDisabledCode)]
    private string DescribeExhaustion(ConstraintCall applying) {
        IReadOnlyList<ConstraintCall> culprits = ExcludingConstraintsInEffect();

        if (_allowed is not null) {
            if (culprits.Count == 0) { return $"none of the values {_allowedConstraint} allows satisfies the constraints already defined"; }

            // Only the allow-list values the bounds and scale lattice still permit can be forbidden by an exclusion;
            // if some allowed value was already dropped by a bound or the grid, the exclusions do not forbid "every"
            // allowed value, so the claim is qualified rather than overstated.
            string allowed = _allowed.All(WouldAllowIgnoringExclusions)
                                 ? $"every value {_allowedConstraint} allows"
                                 : $"every value {_allowedConstraint} allows that the other constraints leave";

            return $"{Forbids(culprits, applying)} {allowed}";
        }

        if (_scale >= 0) {
            if (!_latticeHasPoint || culprits.Count == 0) { return $"no {_typeName} value {_scaleConstraint} allows remains between {_render(_min)} and {_render(_max)}"; }

            return $"{Forbids(culprits, applying)} every {_scaleConstraint} value between {_render(_min)} and {_render(_max)}";
        }

        if (culprits.Count == 0) {
            string pinning = _minConstraint?.ToString() ?? _maxConstraint?.ToString() ?? "the declared bounds";

            return $"{pinning} already pins the value to {_render(_min)}, which the exclusions forbid";
        }

        return $"{Forbids(culprits, applying)} {_render(_min)}, {PinningClause()}";
    }

    /// <summary>
    ///     The distinct exclusion constraints that actually caused the exhaustion — those forbidding at least one
    ///     value the interval, scale lattice and allow-list would otherwise permit. An exclusion whose values fall
    ///     outside the surviving domain never bit, so naming it would mislead; first-declared order is preserved.
    /// </summary>
    private IReadOnlyList<ConstraintCall> ExcludingConstraintsInEffect() {
        List<ConstraintCall> names = [];
        foreach ((ConstraintCall constraint, decimal[] values) in _exclusions) {
            if (names.Contains(constraint)) { continue; }
            if (values.Any(WouldAllowIgnoringExclusions)) { names.Add(constraint); }
        }

        return names;
    }

    /// <summary>Whether <paramref name="value" /> would be in the domain if no exclusion were applied.</summary>
    private bool WouldAllowIgnoringExclusions(decimal value) {
        if (_allowed is not null && !_allowed.Contains(value)) { return false; }
        if (_scale >= 0 && !IsOnGrid(value, _scale)) { return false; }

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
