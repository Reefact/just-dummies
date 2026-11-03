// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VoltageLimits --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using GridLoadFlow.Domain;
using JustDummies;

namespace GridLoadFlow.Tests.Regular;

/// <summary>
///     A generator of arbitrary <see cref="VoltageLimits" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVoltageLimits : IAny<VoltageLimits> {

    private AnyOrderedPair<double> _minimumPuAndMaximumPu;

    private AnyDouble _minimumPuRecipe;
    private AnyDouble _maximumPuRecipe;

    private double? _minimumPuPin;
    private double? _maximumPuPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVoltageLimits()
        : this(minimumPu: AnyValidMinimumPu(),
               maximumPu: AnyValidMaximumPu()) { }

    private static AnyDouble AnyValidMinimumPu() {
        return Any.Double().Between(VoltageLimits.AbsoluteMinimumPu, 1d);
    }

    private static AnyDouble AnyValidMaximumPu() {
        return Any.Double().Between(1d, VoltageLimits.AbsoluteMaximumPu);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyVoltageLimits(AnyDouble minimumPu,
                             AnyDouble maximumPu) {
        _minimumPuRecipe       = minimumPu;
        _maximumPuRecipe       = maximumPu;
        _minimumPuAndMaximumPu = MinimumPuAndMaximumPu();
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyVoltageLimits(AnyVoltageLimits from) {
        _minimumPuAndMaximumPu = from._minimumPuAndMaximumPu;
        _minimumPuRecipe       = from._minimumPuRecipe;
        _minimumPuPin          = from._minimumPuPin;
        _maximumPuRecipe       = from._maximumPuRecipe;
        _maximumPuPin          = from._maximumPuPin;
    }

    /// <summary>Draws <c>minimumPu</c> and <c>maximumPu</c> together, <c>minimumPu</c> always before <c>maximumPu</c> as the constructor requires, each from its current recipe and pin.</summary>
    private AnyOrderedPair<double> MinimumPuAndMaximumPu() {
        AnyOrderedPair<double> pair = Any.OrderedPair(_minimumPuRecipe, _maximumPuRecipe, strict: true);

        if (_minimumPuPin is { } minimumPu) { pair = pair.WithFirst(minimumPu); }
        if (_maximumPuPin is { } maximumPu) { pair = pair.WithSecond(maximumPu); }

        return pair;
    }

    /// <summary>Pins <c>minimumPu</c> to a fixed value, which bounds <c>maximumPu</c>; its recipe is kept for a later refinement.</summary>
    public AnyVoltageLimits WithMinimumPu(double value) {
        AnyVoltageLimits copy = new AnyVoltageLimits(this);

        copy._minimumPuPin          = value;
        copy._minimumPuAndMaximumPu = copy.MinimumPuAndMaximumPu();

        return copy;
    }

    /// <summary>Refines the recipe <c>minimumPu</c> is drawn from — <c>AnyValidMinimumPu()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>maximumPu</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVoltageLimits WithMinimumPu(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVoltageLimits copy = new AnyVoltageLimits(this);

        copy._minimumPuRecipe       = configure(_minimumPuRecipe);
        copy._minimumPuPin          = null;
        copy._minimumPuAndMaximumPu = copy.MinimumPuAndMaximumPu();

        return copy;
    }

    /// <summary>Pins <c>maximumPu</c> to a fixed value, which bounds <c>minimumPu</c>; its recipe is kept for a later refinement.</summary>
    public AnyVoltageLimits WithMaximumPu(double value) {
        AnyVoltageLimits copy = new AnyVoltageLimits(this);

        copy._maximumPuPin          = value;
        copy._minimumPuAndMaximumPu = copy.MinimumPuAndMaximumPu();

        return copy;
    }

    /// <summary>Refines the recipe <c>maximumPu</c> is drawn from — <c>AnyValidMaximumPu()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>minimumPu</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVoltageLimits WithMaximumPu(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVoltageLimits copy = new AnyVoltageLimits(this);

        copy._maximumPuRecipe       = configure(_maximumPuRecipe);
        copy._maximumPuPin          = null;
        copy._minimumPuAndMaximumPu = copy.MinimumPuAndMaximumPu();

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="VoltageLimits" />.</summary>
    public VoltageLimits Generate() {
        (double minimumPu, double maximumPu) = _minimumPuAndMaximumPu.Generate();

        return new VoltageLimits(minimumPu,
                                 maximumPu);
    }

}
