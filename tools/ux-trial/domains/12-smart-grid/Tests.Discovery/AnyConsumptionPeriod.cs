// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ConsumptionPeriod --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace SmartGrid.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ConsumptionPeriod" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyConsumptionPeriod : IAny<ConsumptionPeriod> {

    private IAny<MeterReading> _opening;
    private IAny<MeterReading> _closing;

    private AnyMeterReading _openingRecipe;
    private AnyMeterReading _closingRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyConsumptionPeriod()
        : this(opening: AnyValidOpening(),
               closing: AnyValidClosing()) { }

    private static AnyMeterReading AnyValidOpening() {
        // Leaves room after it for a closing reading.
        return new AnyMeterReading().WithReadingDate(d => d.Before(new DateOnly(2090, 1, 1)));
    }

    private static AnyMeterReading AnyValidClosing() {
        return new AnyMeterReading();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyConsumptionPeriod(AnyMeterReading opening,
                                 AnyMeterReading closing) {
        _opening       = opening;
        _openingRecipe = opening;
        _closing       = closing;
        _closingRecipe = closing;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyConsumptionPeriod(AnyConsumptionPeriod from) {
        _opening       = from._opening;
        _openingRecipe = from._openingRecipe;
        _closing       = from._closing;
        _closingRecipe = from._closingRecipe;
    }

    /// <summary>Pins <c>opening</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyConsumptionPeriod WithOpening(MeterReading value) {
        AnyConsumptionPeriod copy = new AnyConsumptionPeriod(this);

        copy._opening = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>opening</c> is drawn from — <c>AnyValidOpening()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMeterReading, AnyMeterReading&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyConsumptionPeriod WithOpening(Func<AnyMeterReading, AnyMeterReading> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyConsumptionPeriod copy = new AnyConsumptionPeriod(this);

        copy._openingRecipe = configure(_openingRecipe);
        copy._opening       = copy._openingRecipe;

        return copy;
    }

    /// <summary>Pins <c>closing</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyConsumptionPeriod WithClosing(MeterReading value) {
        AnyConsumptionPeriod copy = new AnyConsumptionPeriod(this);

        copy._closing = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>closing</c> is drawn from — <c>AnyValidClosing()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMeterReading, AnyMeterReading&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyConsumptionPeriod WithClosing(Func<AnyMeterReading, AnyMeterReading> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyConsumptionPeriod copy = new AnyConsumptionPeriod(this);

        copy._closingRecipe = configure(_closingRecipe);
        copy._closing       = copy._closingRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ConsumptionPeriod" />.</summary>
    public ConsumptionPeriod Generate() {
        // The period requires the same delivery point, a later date and an index that did not go down: the closing
        // reading is drawn from the opening one.
        MeterReading opening = _opening.Generate();
        MeterReading closing = _closingRecipe.WithDeliveryPoint(opening.DeliveryPoint)
                                             .WithReadingDate(d => d.After(opening.ReadingDate))
                                             .WithIndex(i => i.WithKwh(k => k.GreaterThanOrEqualTo(opening.Index.Kwh)))
                                             .Generate();

        return new ConsumptionPeriod(opening, closing);
    }

}
