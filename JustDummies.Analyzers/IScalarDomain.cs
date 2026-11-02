using System.Collections.Generic;

namespace JustDummies.Analyzers;

/// <summary>
///     The domain a scalar chain has narrowed to, rebuilt constraint by constraint in declaration order, as JD023,
///     JD024 and JD038 read it: whether anything remains, whether anything changed, whether exactly one value is left.
/// </summary>
/// <remarks>
///     Two models implement it. <see cref="ScalarConstraintState" /> reasons in <see cref="long" /> over the integer
///     generators and, through their ticks or day numbers, the temporal ones; <see cref="DecimalConstraintState" />
///     reasons in <see cref="decimal" />, whose bounds are strict or inclusive and whose lattice is a step or a scale.
///     Every argument reaches a model as a <see cref="decimal" />, which holds every integer and every ordinal
///     exactly; a model that cannot hold one abandons the chain rather than misreading it.
/// </remarks>
internal interface IScalarDomain {

    /// <summary>
    ///     Set when a bound asked for values beyond what the type holds — <c>GreaterThan(long.MaxValue)</c>. The
    ///     domain is empty, and no pair of bounds says so.
    /// </summary>
    bool Saturated { get; }

    /// <summary>
    ///     Whether the declared bounds cross: the minimum lies above the maximum, or at it with a strict side. The
    ///     shape the run time words as one bound colliding with another.
    /// </summary>
    bool BoundsCross { get; }

    /// <summary>What the minimum requires, worded as the run time words it — <c>greater than or equal to 6</c>.</summary>
    string MinimumRequirement { get; }

    /// <summary>What the maximum requires, worded as the run time words it — <c>less than 6</c>.</summary>
    string MaximumRequirement { get; }

    /// <summary>
    ///     Applies one constraint, returning the narrowed domain — or <c>null</c> when the constraint is one this model
    ///     does not track, or one another rule owns, which abandons the chain rather than misreading it.
    /// </summary>
    IScalarDomain? Apply(string name, IReadOnlyList<decimal> arguments);

    /// <summary>Whether no value at all survives the constraints declared so far.</summary>
    bool IsEmpty();

    /// <summary>Whether applying <paramref name="candidate" /> would leave the domain exactly as it is.</summary>
    bool NarrowsNothing(IScalarDomain candidate);

    /// <summary>Whether an exclusion removes only values the domain could never have produced anyway.</summary>
    bool ExclusionIsInert(IReadOnlyList<decimal> values);

    /// <summary>Whether this domain's minimum is tighter than <paramref name="previous" />'s.</summary>
    bool TightensMinimumOf(IScalarDomain previous);

    /// <summary>Whether this domain's maximum is tighter than <paramref name="previous" />'s.</summary>
    bool TightensMaximumOf(IScalarDomain previous);

    /// <summary>
    ///     How many values survive, when that can be settled and is at most <paramref name="limit" />; <c>null</c>
    ///     where the domain is wider than that, or wider than the model can count.
    /// </summary>
    long? CountAdmitted(long limit);

    /// <summary>The one value a domain of one admits, rendered as the run time writes it, or <c>null</c>.</summary>
    string? TheOnlyValue();

}
