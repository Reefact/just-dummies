using System;
using System.Collections.Generic;
using System.Linq;

namespace JustDummies.Analyzers;

/// <summary>
///     The integer domain a scalar chain has narrowed to, rebuilt constraint by constraint in declaration order.
///     <c>JD023</c> (the domain became empty), <c>JD024</c> (a constraint narrowed nothing) and <c>JD038</c> (one
///     value is left) read it: one asks whether anything remains, one whether anything changed, one how much is left.
/// </summary>
/// <remarks>
///     Integers only, and only where every argument folds to a constant: the integer generators, and the temporal
///     ones through the ticks or the day number they compare by (issue #356). A chain carrying one unfoldable
///     argument stops being tracked rather than being guessed at: a rule that claims a chain is unsatisfiable must be
///     certain. Bounds are kept in <see cref="long" /> so a constraint at <see cref="int.MinValue" /> cannot overflow
///     the arithmetic that tests them. The declared bounds are kept apart from the generator's own domain — a
///     <c>byte</c> holds 0 to 255 whatever the chain declares — so the emptiness checks judge the real domain while
///     a bound is still compared, for narrowing, with what the chain itself declared.
/// </remarks>
internal sealed class ScalarConstraintState : IScalarDomain {

    /// <summary>
    ///     How many values the emptiness checks will walk before giving up and answering "not empty". It bounds both
    ///     walks below — the range and the lattice — so the rule stays cheap on a chain over a huge domain, at the
    ///     price of a deliberate false negative on one no realistic exclusion set could empty anyway.
    /// </summary>
    private const long MaxWalkLength = 64;

    private readonly Func<long, string> _render;

    private ScalarConstraintState(long minimum, long maximum, (long? Step, long Residue) lattice, HashSet<long>? allowed, HashSet<long> excluded, (long Minimum, long? Maximum, Func<long, string> Render) domain, bool saturated = false) {
        _render       = domain.Render;
        Minimum       = minimum;
        Maximum       = maximum;
        MultipleOf    = lattice.Step;
        Residue       = lattice.Residue;
        Allowed       = allowed;
        Excluded      = excluded;
        DomainMinimum = domain.Minimum;
        DomainMaximum = domain.Maximum;
        Saturated     = saturated;
    }

    public long           Minimum    { get; }
    public long           Maximum    { get; }
    public long?          MultipleOf { get; }

    /// <summary>
    ///     What a value leaves divided by <see cref="MultipleOf" />, in <c>[0, MultipleOf)</c>: <c>0</c> for a
    ///     <c>MultipleOf(...)</c> or an <c>Even()</c>, <c>1</c> for an <c>Odd()</c>.
    /// </summary>
    public long           Residue    { get; }

    public HashSet<long>? Allowed    { get; }
    public HashSet<long>  Excluded   { get; }

    /// <summary>The smallest value the generator's type holds.</summary>
    public long DomainMinimum { get; }

    /// <summary>
    ///     The largest value the generator's type holds, or <c>null</c> when it lies beyond the model — a
    ///     <c>ulong</c>, whose top no <see cref="long" /> reaches.
    /// </summary>
    public long? DomainMaximum { get; }

    // The interval the emptiness checks judge: the declared bounds, clipped to what the type holds.
    private long Low => Minimum > DomainMinimum ? Minimum : DomainMinimum;

    private long High => DomainMaximum is long top && top < Maximum ? top : Maximum;

    /// <summary>
    ///     Set when a bound asked for values beyond the representable range — <c>GreaterThan(long.MaxValue)</c>. The
    ///     domain is empty, and saying so needs a flag rather than an out-of-range bound, because the bounds run to
    ///     the extremes: <c>LessThanOrEqualTo(long.MinValue)</c> is a legal chain that yields exactly one value.
    /// </summary>
    public bool Saturated { get; }

    /// <inheritdoc />
    public bool BoundsCross => !Saturated && Minimum > Maximum;

    /// <inheritdoc />
    public string MinimumRequirement => $"greater than or equal to {_render(Minimum)}";

    /// <inheritdoc />
    public string MaximumRequirement => $"less than or equal to {_render(Maximum)}";

    /// <summary>
    ///     The state before any constraint, over a type holding <paramref name="domainMinimum" /> to
    ///     <paramref name="domainMaximum" />, whose values <paramref name="render" /> writes as the run time does.
    /// </summary>
    public static ScalarConstraintState Unconstrained(long domainMinimum, long? domainMaximum, Func<long, string> render) {
        return new ScalarConstraintState(long.MinValue, long.MaxValue, (null, 0), null, [], (domainMinimum, domainMaximum, render));
    }

    /// <inheritdoc />
    public bool TightensMinimumOf(IScalarDomain previous) {
        return previous is ScalarConstraintState before && Minimum > before.Minimum;
    }

    /// <inheritdoc />
    public bool TightensMaximumOf(IScalarDomain previous) {
        return previous is ScalarConstraintState before && Maximum < before.Maximum;
    }

    /// <inheritdoc />
    public long? CountAdmitted(long limit) {
        if (Saturated) { return 0; }
        if (Allowed is not null) { return Allowed.Count(Admits); }
        if (Low > High) { return 0; }
        // A range left open above on a ulong runs past the model, where values the model never sees survive.
        if (DomainMaximum is null && Maximum == long.MaxValue) { return null; }

        long counted = Survivors().Take((int)Math.Min(limit, MaxWalkLength) + 1).LongCount();

        return counted <= limit ? counted : null;
    }

    /// <inheritdoc />
    public string? TheOnlyValue() {
        if (CountAdmitted(1) != 1) { return null; }

        return _render(Allowed is not null ? Allowed.First(Admits) : Survivors().First());
    }

    /// <summary>
    ///     The values that survive, lowest first — the lattice points, or every value of the range — as far as a walk
    ///     goes; a range wider than the walk yields its top after it, so it is never counted as small.
    /// </summary>
    private IEnumerable<long> Survivors() {
        long low  = Low;
        long high = High;
        long step = MultipleOf ?? 1;
        if (!TryFirstLatticePoint(low, high, step, out long first)) { yield break; }

        long candidate = first;
        for (long walked = 0; walked < MaxWalkLength; walked++) {
            if (Admits(candidate)) { yield return candidate; }
            // Compared before stepping, so the step past the top cannot overflow at the representable extremes.
            if ((ulong)(high - candidate) < (ulong)step) { yield break; }

            candidate += step;
        }

        // The walk ran out before the range did: the rest is taken as surviving.
        yield return high;
    }

    /// <summary>The first point of the lattice at or above <paramref name="low" />, if one lies at or below <paramref name="high" />.</summary>
    private bool TryFirstLatticePoint(long low, long high, long step, out long first) {
        long past = Remainder(low, step) - Residue;
        if (past < 0) { past += step; }
        long rise = past == 0 ? 0 : step - past;
        first = low + rise;

        return (ulong)(high - low) >= (ulong)rise;
    }

    /// <summary>Every argument as the <see cref="long" /> it is, or <c>false</c> for one the model cannot hold.</summary>
    private static bool TryIntegers(IReadOnlyList<decimal> arguments, out IReadOnlyList<long> integers) {
        List<long> read = [];
        integers = read;

        foreach (decimal argument in arguments) {
            if (argument != decimal.Truncate(argument) || argument < long.MinValue || argument > long.MaxValue) { return false; }

            read.Add((long)argument);
        }

        return true;
    }

    /// <summary>Whether no value at all survives the constraints declared so far.</summary>
    public bool IsEmpty() {
        if (Saturated) { return true; }
        if (Allowed is not null) { return !Allowed.Any(Admits); }
        if (Low > High) { return true; }

        // A small finite range can be emptied by its exclusions alone, with the bounds still consistent:
        // Zero().NonZero() pins [0, 0] and then forbids the only value in it.
        if (Excluded.Count > 0 && FitsInAWalk()) {
            for (long value = Low; value <= High; value++) {
                if (Admits(value)) { return false; }
            }

            return true;
        }

        return MultipleOf is long step && !HasMultipleInRange(step);
    }

    // Only walk a range small enough to enumerate, and far enough from the extremes that the arithmetic cannot
    // overflow. A wider range is never declared empty by exclusions: no realistic exclusion set could empty it.
    private bool FitsInAWalk() {
        return Low > long.MinValue / 2 && High < long.MaxValue / 2 && High - Low < MaxWalkLength;
    }

    /// <summary>Whether <paramref name="value" /> survives every constraint declared so far.</summary>
    public bool Admits(long value) {
        if (value < Low || value > High) { return false; }
        if (Excluded.Contains(value)) { return false; }
        if (MultipleOf is long step && step != 0 && Remainder(value, step) != Residue) { return false; }

        return Allowed is null || Allowed.Contains(value);
    }

    /// <inheritdoc />
    public IScalarDomain? Apply(string name, IReadOnlyList<decimal> arguments) {
        return TryIntegers(arguments, out IReadOnlyList<long> integers) ? Apply(name, integers) : null;
    }

    /// <summary>
    ///     Applies one constraint, returning the narrowed state — or <c>null</c> when the constraint is one this model
    ///     does not track, which abandons the chain rather than misreading it.
    /// </summary>
    public ScalarConstraintState? Apply(string name, IReadOnlyList<long> arguments) {
        // Where the type's top lies beyond the model — a ulong — a bound at long.MaxValue cannot be told from that
        // top: it may narrow the domain or empty half of it. The chain is abandoned rather than judged on a guess.
        if (DomainMaximum is null && arguments.Contains(long.MaxValue)) { return null; }

        switch (name) {
            case "Positive":            return WithMinimum(1);
            case "Negative":            return WithMaximum(-1);
            case "Zero":                return WithMinimum(0)?.WithMaximum(0);
            case "NonZero":             return WithExcluded(0);

            case "GreaterThan" or "LessThan" or "GreaterThanOrEqualTo" or "LessThanOrEqualTo" when arguments.Count == 1:
                return WithBound(name, arguments[0]);

            // Transposed bounds are an argument error the builder refuses before any constraint is applied — JD014's
            // diagnosis, not an empty domain — so the chain is left to that rule rather than reported twice.
            case "Between" when arguments.Count == 2:
                return arguments[0] > arguments[1] ? null : WithMinimum(arguments[0])?.WithMaximum(arguments[1]);
            // A step the guard refuses is JD014's; a second lattice is JD034's unless it is the same one, which the
            // run time ignores. Either way the chain is left to that rule rather than judged on a lattice it rejects.
            case "MultipleOf" when arguments.Count == 1 && arguments[0] > 0: return WithLattice(arguments[0], 0);
            case "Odd" when arguments.Count == 0:                            return WithLattice(2, 1);
            case "Even" when arguments.Count == 0:                           return WithLattice(2, 0);
            // A granularity, read by the walk as the lattice it is: a step and the residue its anchor leaves.
            case "OnLattice" when arguments.Count == 2 && arguments[0] > 0:  return WithLattice(arguments[0], arguments[1]);

            // A second value set is JD034's, unless it lists the same values, which the run time ignores.
            case "OneOf" when arguments.Count > 0: {
                HashSet<long> allowed = [.. arguments];

                return Allowed is null || Allowed.SetEquals(allowed)
                           ? new ScalarConstraintState(Minimum, Maximum, (MultipleOf, Residue), Allowed ?? allowed, Excluded, (DomainMinimum, DomainMaximum, _render))
                           : null;
            }

            case "Except" or "DifferentFrom" when arguments.Count > 0: {
                HashSet<long> excluded = [.. Excluded, .. arguments];

                return new ScalarConstraintState(Minimum, Maximum, (MultipleOf, Residue), Allowed, excluded, (DomainMinimum, DomainMaximum, _render));
            }

            // Anything else — a granularity, a scale, a name this model has never seen — ends the walk.
            default: return null;
        }
    }

    /// <inheritdoc />
    public bool NarrowsNothing(IScalarDomain candidate) {
        return candidate is ScalarConstraintState next && NarrowsNothing(next);
    }

    /// <summary>Whether applying <paramref name="candidate" /> would leave the domain exactly as it is.</summary>
    public bool NarrowsNothing(ScalarConstraintState candidate) {
        return candidate.Minimum == Minimum
            && candidate.Maximum == Maximum
            && candidate.MultipleOf == MultipleOf
            && candidate.Residue == Residue
            && candidate.Excluded.Count == Excluded.Count
            && (candidate.Allowed?.Count ?? -1) == (Allowed?.Count ?? -1);
    }

    /// <inheritdoc />
    public bool ExclusionIsInert(IReadOnlyList<decimal> values) {
        return TryIntegers(values, out IReadOnlyList<long> integers) && ExclusionIsInert(integers);
    }

    /// <summary>
    ///     Whether an exclusion removes a value the domain could never have produced anyway — the silent case, where
    ///     the author excluded a sentinel the generator was never going to draw.
    /// </summary>
    public bool ExclusionIsInert(IReadOnlyList<long> values) {
        return values.Count > 0 && values.All(value => !Admits(value));
    }

    /// <summary>
    ///     One bound. Nothing is greater than the largest value the type holds, nor less than the smallest: those two
    ///     ask for an empty domain rather than for a bound, and computing one at the model's own edge would overflow.
    /// </summary>
    private ScalarConstraintState? WithBound(string name, long bound) {
        return name switch {
            "GreaterThan"          => DomainMaximum is long top && bound >= top ? Saturate() : WithMinimum(bound + 1),
            "LessThan"             => bound <= DomainMinimum ? Saturate() : WithMaximum(bound - 1),
            "GreaterThanOrEqualTo" => WithMinimum(bound),
            _                      => WithMaximum(bound)
        };
    }

    private ScalarConstraintState? WithLattice(long step, long residue) {
        if (MultipleOf is long declared) { return declared == step && Residue == residue ? this : null; }

        return new ScalarConstraintState(Minimum, Maximum, (step, residue), Allowed, Excluded, (DomainMinimum, DomainMaximum, _render));
    }

    private ScalarConstraintState Saturate() {
        return new ScalarConstraintState(Minimum, Maximum, (MultipleOf, Residue), Allowed, Excluded, (DomainMinimum, DomainMaximum, _render), saturated: true);
    }

    private ScalarConstraintState? WithMinimum(long minimum) {
        return minimum <= Minimum
            ? new ScalarConstraintState(Minimum, Maximum, (MultipleOf, Residue), Allowed, Excluded, (DomainMinimum, DomainMaximum, _render))
            : new ScalarConstraintState(minimum, Maximum, (MultipleOf, Residue), Allowed, Excluded, (DomainMinimum, DomainMaximum, _render));
    }

    private ScalarConstraintState? WithMaximum(long maximum) {
        return maximum >= Maximum
            ? new ScalarConstraintState(Minimum, Maximum, (MultipleOf, Residue), Allowed, Excluded, (DomainMinimum, DomainMaximum, _render))
            : new ScalarConstraintState(Minimum, maximum, (MultipleOf, Residue), Allowed, Excluded, (DomainMinimum, DomainMaximum, _render));
    }

    private ScalarConstraintState WithExcluded(long value) {
        HashSet<long> excluded = [.. Excluded, value];

        return new ScalarConstraintState(Minimum, Maximum, (MultipleOf, Residue), Allowed, excluded, (DomainMinimum, DomainMaximum, _render));
    }

    // Is there any multiple of step inside [Minimum, Maximum] that survives the exclusions? The range can be huge, so
    // this walks the lattice from its first multiple rather than the range itself, and gives up (answering "yes")
    // once the walk is longer than any realistic exclusion set could rule out.
    private bool HasMultipleInRange(long step) {
        if (step == 0) { return false; }

        // A ulong range left open above runs past the model's top, and every multiple in (long.MaxValue, ulong.MaxValue]
        // survives — the exclusions are longs. The walk below sees only the part the model holds, so it is not asked.
        if (DomainMaximum is null && Maximum == long.MaxValue) { return true; }

        long low  = Low;
        long high = High;
        // The first lattice point at or above the low end: how far the low end lies past the residue, then up to the
        // next point. The width is compared unsigned, since high - low overflows a long across zero at the extremes,
        // and the point is reached only once it is known to fit below the high end.
        long past = Remainder(low, step) - Residue;
        if (past < 0) { past += step; }
        long rise = past == 0 ? 0 : step - past;
        if ((ulong)(high - low) < (ulong)rise) { return false; }
        long first = low + rise;

        for (long candidate = first, seen = 0; candidate <= high && seen < MaxWalkLength; candidate += step, seen++) {
            if (!Excluded.Contains(candidate) && (Allowed is null || Allowed.Contains(candidate))) { return true; }
        }

        // The walk gave up before finding one; only a range genuinely wider than the walk can still hold a multiple.
        // Compare by division so the subtraction cannot overflow at the representable extremes — which is why both
        // sides are halved, the right one carrying half the walk length rather than the whole of it.
        return high / 2 - low / 2 >= step * (MaxWalkLength / 2);
    }

    /// <summary>What <paramref name="value" /> leaves divided by <paramref name="step" />, in <c>[0, step)</c> whatever its sign.</summary>
    private static long Remainder(long value, long step) {
        long remainder = value % step;

        return remainder < 0 ? remainder + step : remainder;
    }

}
