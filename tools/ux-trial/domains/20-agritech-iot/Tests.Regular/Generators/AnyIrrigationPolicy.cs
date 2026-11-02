// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate IrrigationPolicy --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace AgriTech.Domain;

/// <summary>
///     A generator of arbitrary <see cref="IrrigationPolicy" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyIrrigationPolicy : IAny<IrrigationPolicy> {

    private IAny<double> _dryThresholdPercent;
    private IAny<int>    _minimumReadings;

    private AnyDouble _dryThresholdPercentRecipe;
    private AnyInt32  _minimumReadingsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyIrrigationPolicy()
        : this(dryThresholdPercent: AnyValidDryThresholdPercent(),
               minimumReadings:     AnyValidMinimumReadings()) { }

    private static AnyDouble AnyValidDryThresholdPercent() {
        return Any.Double().Between(0d, 100d);
    }

    private static AnyInt32 AnyValidMinimumReadings() {
        return Any.Int32().Positive();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyIrrigationPolicy(AnyDouble dryThresholdPercent,
                                AnyInt32  minimumReadings) {
        _dryThresholdPercent       = dryThresholdPercent;
        _dryThresholdPercentRecipe = dryThresholdPercent;
        _minimumReadings           = minimumReadings;
        _minimumReadingsRecipe     = minimumReadings;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyIrrigationPolicy(AnyIrrigationPolicy from) {
        _dryThresholdPercent       = from._dryThresholdPercent;
        _dryThresholdPercentRecipe = from._dryThresholdPercentRecipe;
        _minimumReadings           = from._minimumReadings;
        _minimumReadingsRecipe     = from._minimumReadingsRecipe;
    }

    /// <summary>Pins <c>dryThresholdPercent</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyIrrigationPolicy WithDryThresholdPercent(double value) {
        AnyIrrigationPolicy copy = new AnyIrrigationPolicy(this);

        copy._dryThresholdPercent = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>dryThresholdPercent</c> is drawn from — <c>AnyValidDryThresholdPercent()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyIrrigationPolicy WithDryThresholdPercent(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyIrrigationPolicy copy = new AnyIrrigationPolicy(this);

        copy._dryThresholdPercentRecipe = configure(_dryThresholdPercentRecipe);
        copy._dryThresholdPercent       = copy._dryThresholdPercentRecipe;

        return copy;
    }

    /// <summary>Pins <c>minimumReadings</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyIrrigationPolicy WithMinimumReadings(int value) {
        AnyIrrigationPolicy copy = new AnyIrrigationPolicy(this);

        copy._minimumReadings = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>minimumReadings</c> is drawn from — <c>AnyValidMinimumReadings()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyIrrigationPolicy WithMinimumReadings(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyIrrigationPolicy copy = new AnyIrrigationPolicy(this);

        copy._minimumReadingsRecipe = configure(_minimumReadingsRecipe);
        copy._minimumReadings       = copy._minimumReadingsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="IrrigationPolicy" />.</summary>
    public IrrigationPolicy Generate() {
        return new IrrigationPolicy(_dryThresholdPercent.Generate(),
                                    _minimumReadings.Generate());
    }

}
