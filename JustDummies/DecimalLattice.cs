#region Usings declarations

using System.Globalization;

#endregion

namespace JustDummies;

/// <summary>
///     The two lattice declarations a <see cref="decimal" /> generator can carry — a scale and a step — and the one
///     grid they agree on. Each is declared once; together they hold only when the step lies on the scale's grid,
///     and the grid drawn from is then the step's (ADR-0123).
/// </summary>
internal sealed class DecimalLattice {

    private const int NoScale = -1;

    /// <summary>No lattice declared: every value of the interval is drawable.</summary>
    internal static readonly DecimalLattice None = new(NoScale, null, 0m, null);

    private readonly int             _scale;
    private readonly ConstraintCall? _scaleConstraint;
    private readonly decimal         _step;
    private readonly ConstraintCall? _stepConstraint;

    private DecimalLattice(int scale, ConstraintCall? scaleConstraint, decimal step, ConstraintCall? stepConstraint) {
        _scale           = scale;
        _scaleConstraint = scaleConstraint;
        _step            = step;
        _stepConstraint  = stepConstraint;
        if (step > 0m) {
            Grid = DecimalGrid.OfStep(step);
        } else if (scale >= 0) {
            Grid = DecimalGrid.OfScale(scale);
        }
    }

    /// <summary>The grid the declarations agree on, or <c>null</c> when none is declared.</summary>
    internal DecimalGrid? Grid { get; }

    /// <summary>The constraint whose grid is drawn from — the step when one is declared, the scale otherwise.</summary>
    internal ConstraintCall? GridConstraint => _stepConstraint ?? _scaleConstraint;

    /// <summary>
    ///     Adds a scale. The same one again changes nothing; a different one conflicts with the first, and one whose
    ///     grid the declared step does not lie on conflicts with the step.
    /// </summary>
    internal DecimalLattice WithScale(int scale, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (_scale >= 0) {
            if (_scale == scale) { return this; }

            // _scale and _scaleConstraint are written as a pair, so a declared scale always carries its constraint.
            throw ConflictingAnyConstraintException.AlreadyDefined(applying, _scaleConstraint!);
        }
        if (_stepConstraint is not null && !DecimalGrid.OfScale(scale).Contains(_step)) {
            throw ConflictingAnyConstraintException.StepOffTheScale(applying, _stepConstraint, Quantum(scale));
        }

        return new DecimalLattice(scale, applying, _step, _stepConstraint);
    }

    /// <summary>
    ///     Adds a step, strictly positive. The same one again changes nothing; a different one conflicts with the
    ///     first, and one off the declared scale's grid conflicts with the scale.
    /// </summary>
    internal DecimalLattice WithStep(decimal step, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (_stepConstraint is not null) {
            if (_step == step) { return this; }

            throw ConflictingAnyConstraintException.AlreadyDefined(applying, _stepConstraint);
        }
        if (_scaleConstraint is not null && !DecimalGrid.OfScale(_scale).Contains(step)) {
            throw ConflictingAnyConstraintException.StepOffTheScale(applying, _scaleConstraint, Quantum(_scale));
        }

        return new DecimalLattice(_scale, _scaleConstraint, step, applying);
    }

    /// <summary>
    ///     Each declared lattice with the test a value must pass to satisfy it — what a pool's rejections are
    ///     explained with, one constraint at a time.
    /// </summary>
    internal IEnumerable<(ConstraintCall Constraint, Func<decimal, bool> Admits)> Declarations() {
        if (_scaleConstraint is not null) {
            DecimalGrid scaleGrid = DecimalGrid.OfScale(_scale);

            yield return (_scaleConstraint, scaleGrid.Contains);
        }
        if (_stepConstraint is not null) { yield return (_stepConstraint, Grid!.Contains); }
    }

    private static string Quantum(int scale) {
        return (1m / DecimalGrid.Pow10(scale)).ToString(CultureInfo.InvariantCulture);
    }

}
