using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace JustDummies.Analyzers;

/// <summary>
///     The <see cref="decimal" /> domain an <c>Any.Decimal()</c> chain has narrowed to (issue #356). A bound is strict
///     or inclusive, as the generator keeps it; the lattice is a step (<c>MultipleOf</c>) or a scale (<c>WithScale</c>),
///     and a step lies on its scale's grid or the chain is JD034's.
/// </summary>
/// <remarks>
///     A decimal interval that is not a point holds more values than any exclusion set could remove, so emptiness is
///     decided by the bounds, the allow-list and the lattice only — a point interval by its one value. A domain that
///     narrows to one value is counted by the same walks, which is what JD038 reads.
/// </remarks>
internal sealed class DecimalConstraintState : IScalarDomain {

    /// <summary>The most decimal places a scale may ask for, as the generator's guard has it.</summary>
    private const int MaxScale = 28;

    /// <summary>How many lattice points the walks visit before answering "not empty", as the integer model does.</summary>
    private const int MaxWalkLength = 64;

    private readonly decimal          _minimum;
    private readonly bool             _minimumStrict;
    private readonly decimal          _maximum;
    private readonly bool             _maximumStrict;
    private readonly decimal?         _step;
    private readonly int?             _scale;
    private readonly HashSet<decimal>? _allowed;
    private readonly HashSet<decimal>  _excluded;

    private DecimalConstraintState((decimal Value, bool Strict) minimum, (decimal Value, bool Strict) maximum, (decimal? Step, int? Scale) lattice, HashSet<decimal>? allowed, HashSet<decimal> excluded) {
        _minimum       = minimum.Value;
        _minimumStrict = minimum.Strict;
        _maximum       = maximum.Value;
        _maximumStrict = maximum.Strict;
        _step          = lattice.Step;
        _scale         = lattice.Scale;
        _allowed       = allowed;
        _excluded      = excluded;
    }

    /// <inheritdoc />
    public bool Saturated => false;

    /// <inheritdoc />
    public bool BoundsCross => _minimum > _maximum || (_minimum == _maximum && (_minimumStrict || _maximumStrict));

    /// <inheritdoc />
    public string MinimumRequirement => $"greater than {(_minimumStrict ? "" : "or equal to ")}{Render(_minimum)}";

    /// <inheritdoc />
    public string MaximumRequirement => $"less than {(_maximumStrict ? "" : "or equal to ")}{Render(_maximum)}";

    /// <summary>The grid every value must lie on: the step, or the scale's quantum, or none.</summary>
    private decimal? Grid => _step ?? (_scale is int places ? Quantum(places) : null);

    /// <summary>The state before any constraint.</summary>
    public static DecimalConstraintState Unconstrained() {
        return new DecimalConstraintState((decimal.MinValue, false), (decimal.MaxValue, false), (null, null), null, []);
    }

    /// <summary>A value as the run time writes it in a message.</summary>
    public static string Render(decimal value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public IScalarDomain? Apply(string name, IReadOnlyList<decimal> arguments) {
        switch (name) {
            case "Positive": return WithMinimum(0m, true);
            case "Negative": return WithMaximum(0m, true);
            case "Zero":     return WithMinimum(0m, false).WithMaximum(0m, false);
            case "NonZero":  return WithExcluded([0m]);

            case "GreaterThan" when arguments.Count == 1:          return WithMinimum(arguments[0], true);
            case "GreaterThanOrEqualTo" when arguments.Count == 1: return WithMinimum(arguments[0], false);
            case "LessThan" when arguments.Count == 1:             return WithMaximum(arguments[0], true);
            case "LessThanOrEqualTo" when arguments.Count == 1:    return WithMaximum(arguments[0], false);

            // Transposed bounds are JD014's; a step or a scale the guard refuses is too, and a second step or scale,
            // or a step off the scale's grid, is JD034's (ADR-0123): the chain is left to them rather than judged.
            case "Between" when arguments.Count == 2:
                return arguments[0] > arguments[1] ? null : WithMinimum(arguments[0], false).WithMaximum(arguments[1], false);

            case "MultipleOf" when arguments.Count == 1:
                return arguments[0] > 0m && _step is null && OnTheGrid(arguments[0], _scale)
                           ? new DecimalConstraintState((_minimum, _minimumStrict), (_maximum, _maximumStrict), (arguments[0], _scale), _allowed, _excluded)
                           : null;

            case "WithScale" when arguments.Count == 1 && arguments[0] >= 0m && arguments[0] <= MaxScale && arguments[0] == decimal.Truncate(arguments[0]): {
                int places = (int)arguments[0];

                return _scale is null && (_step is not decimal step || OnTheGrid(step, places))
                           ? new DecimalConstraintState((_minimum, _minimumStrict), (_maximum, _maximumStrict), (_step, places), _allowed, _excluded)
                           : null;
            }

            // A second value set is JD034's, unless it lists the same values, which the run time ignores.
            case "OneOf" when arguments.Count > 0: {
                HashSet<decimal> allowed = [.. arguments];

                return _allowed is null || _allowed.SetEquals(allowed)
                           ? new DecimalConstraintState((_minimum, _minimumStrict), (_maximum, _maximumStrict), (_step, _scale), _allowed ?? allowed, _excluded)
                           : null;
            }

            case "Except" or "DifferentFrom" when arguments.Count > 0: return WithExcluded(arguments);

            default: return null;
        }
    }

    /// <inheritdoc />
    public bool IsEmpty() {
        if (_allowed is not null) { return !_allowed.Any(Admits); }
        if (BoundsCross) { return true; }
        // A grid the arithmetic cannot place over the interval — one at the type's own edge — is never declared empty.
        if (Grid is decimal step) { return TryFirstLatticePoint(step, out decimal first) && !LatticePoints(first, step).Any(); }

        return _minimum == _maximum && _excluded.Contains(_minimum);
    }

    /// <inheritdoc />
    public bool NarrowsNothing(IScalarDomain candidate) {
        return candidate is DecimalConstraintState next
            && next._minimum == _minimum && next._minimumStrict == _minimumStrict
            && next._maximum == _maximum && next._maximumStrict == _maximumStrict
            && next._step == _step && next._scale == _scale
            && next._excluded.Count == _excluded.Count
            && (next._allowed?.Count ?? -1) == (_allowed?.Count ?? -1);
    }

    /// <inheritdoc />
    public bool ExclusionIsInert(IReadOnlyList<decimal> values) {
        return values.Count > 0 && values.All(value => !Admits(value));
    }

    /// <inheritdoc />
    public bool TightensMinimumOf(IScalarDomain previous) {
        return previous is DecimalConstraintState before && (_minimum > before._minimum || (_minimum == before._minimum && _minimumStrict && !before._minimumStrict));
    }

    /// <inheritdoc />
    public bool TightensMaximumOf(IScalarDomain previous) {
        return previous is DecimalConstraintState before && (_maximum < before._maximum || (_maximum == before._maximum && _maximumStrict && !before._maximumStrict));
    }

    /// <inheritdoc />
    public long? CountAdmitted(long limit) {
        if (_allowed is not null) { return _allowed.Count(Admits); }
        if (BoundsCross) { return 0; }
        if (Grid is decimal step) {
            if (!TryFirstLatticePoint(step, out decimal first)) { return null; }

            int counted = LatticePoints(first, step).Take((int)Math.Min(limit, MaxWalkLength) + 1).Count();

            return counted <= limit ? counted : null;
        }
        if (_minimum == _maximum) { return _excluded.Contains(_minimum) ? 0 : 1; }

        return null;
    }

    /// <inheritdoc />
    public string? TheOnlyValue() {
        if (CountAdmitted(1) != 1) { return null; }
        if (_allowed is not null) { return Render(_allowed.First(Admits)); }
        if (Grid is decimal step && TryFirstLatticePoint(step, out decimal first)) { return Render(LatticePoints(first, step).First()); }

        return Render(_minimum);
    }

    /// <summary>Whether <paramref name="value" /> survives every constraint declared so far.</summary>
    private bool Admits(decimal value) {
        if (value < _minimum || (value == _minimum && _minimumStrict)) { return false; }
        if (value > _maximum || (value == _maximum && _maximumStrict)) { return false; }
        if (_excluded.Contains(value)) { return false; }
        if (Grid is decimal step && decimal.Remainder(value, step) != 0m) { return false; }

        return _allowed is null || _allowed.Contains(value);
    }

    /// <summary>
    ///     The surviving lattice points of the interval, lowest first, at most a walk of them: a wider interval is never
    ///     declared empty, and never counted.
    /// </summary>
    private IEnumerable<decimal> LatticePoints(decimal first, decimal step) {
        decimal candidate = first;
        for (int seen = 0; seen < MaxWalkLength; seen++) {
            if (candidate > _maximum || (candidate == _maximum && _maximumStrict)) { yield break; }
            if (!_excluded.Contains(candidate) && (_allowed is null || _allowed.Contains(candidate))) { yield return candidate; }

            // One guarded addition per point, never a product: a step as wide as the type overflows a multiple of it
            // at the second or third point, and a sum that leaves the type is a point past the interval, which ends the
            // walk — no grid of this size empties an interval at the type's own edge.
            if (Stepped(candidate, step) is not decimal next) { yield break; }

            candidate = next;
        }

        // The walk ran out before the interval did: the rest is taken as surviving.
        yield return _maximum;
    }

    /// <summary><paramref name="first" /> moved up by <paramref name="distance" />, or <c>null</c> where that leaves the type.</summary>
    private static decimal? Stepped(decimal first, decimal distance) {
        try {
            return first + distance;
        } catch (OverflowException) {
            return null;
        }
    }

    /// <summary>
    ///     The first point of the grid the minimum admits, or <c>false</c> where the arithmetic overflows — an
    ///     interval at the type's own edge, which no grid of this size empties and no walk counts.
    /// </summary>
    private bool TryFirstLatticePoint(decimal step, out decimal first) {
        try {
            first = Math.Ceiling(_minimum / step) * step;
            if (first == _minimum && _minimumStrict) { first += step; }

            return true;
        } catch (OverflowException) {
            first = _minimum;

            return false;
        }
    }

    private DecimalConstraintState WithMinimum(decimal bound, bool strict) {
        // A looser bound, or the same bound no stricter, is a no-op, as the generator folds it.
        if (bound < _minimum || (bound == _minimum && (_minimumStrict || !strict))) { return this; }

        return new DecimalConstraintState((bound, strict), (_maximum, _maximumStrict), (_step, _scale), _allowed, _excluded);
    }

    private DecimalConstraintState WithMaximum(decimal bound, bool strict) {
        if (bound > _maximum || (bound == _maximum && (_maximumStrict || !strict))) { return this; }

        return new DecimalConstraintState((_minimum, _minimumStrict), (bound, strict), (_step, _scale), _allowed, _excluded);
    }

    private DecimalConstraintState WithExcluded(IEnumerable<decimal> values) {
        HashSet<decimal> excluded = [.. _excluded, .. values];

        return new DecimalConstraintState((_minimum, _minimumStrict), (_maximum, _maximumStrict), (_step, _scale), _allowed, excluded);
    }

    /// <summary>Whether <paramref name="step" /> is a multiple of the scale's quantum — the condition for the two to hold together (ADR-0123).</summary>
    private static bool OnTheGrid(decimal step, int? scale) {
        return scale is not int places || decimal.Round(step, places) == step;
    }

    /// <summary>The smallest positive value of a given scale: <c>0.01</c> for two places.</summary>
    private static decimal Quantum(int places) {
        return new decimal(1, 0, 0, false, (byte)places);
    }

}
