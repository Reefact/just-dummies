// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Meter --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace SmartGrid.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Meter" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMeter : IAny<Meter> {

    private IAny<DeliveryPointId> _deliveryPoint;
    private IAny<TariffOption>    _tariff;
    private IAny<MeterReading>    _commissioningReading;

    private AnyDeliveryPointId    _deliveryPointRecipe;
    private AnyEnum<TariffOption> _tariffRecipe;
    private AnyMeterReading       _commissioningReadingRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMeter()
        : this(deliveryPoint:        AnyValidDeliveryPoint(),
               tariff:               AnyValidTariff(),
               commissioningReading: AnyValidCommissioningReading()) { }

    private static AnyDeliveryPointId AnyValidDeliveryPoint() {
        return new AnyDeliveryPointId();
    }

    private static AnyEnum<TariffOption> AnyValidTariff() {
        return Any.Enum<TariffOption>();
    }

    private static AnyMeterReading AnyValidCommissioningReading() {
        // A meter is commissioned on an actual reading.
        return new AnyMeterReading().WithSource(ReadingSource.Actual);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMeter(AnyDeliveryPointId    deliveryPoint,
                     AnyEnum<TariffOption> tariff,
                     AnyMeterReading       commissioningReading) {
        _deliveryPoint              = deliveryPoint;
        _deliveryPointRecipe        = deliveryPoint;
        _tariff                     = tariff;
        _tariffRecipe               = tariff;
        _commissioningReading       = commissioningReading;
        _commissioningReadingRecipe = commissioningReading;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMeter(AnyMeter from) {
        _deliveryPoint              = from._deliveryPoint;
        _deliveryPointRecipe        = from._deliveryPointRecipe;
        _tariff                     = from._tariff;
        _tariffRecipe               = from._tariffRecipe;
        _commissioningReading       = from._commissioningReading;
        _commissioningReadingRecipe = from._commissioningReadingRecipe;
    }

    /// <summary>Pins <c>deliveryPoint</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeter WithDeliveryPoint(DeliveryPointId value) {
        AnyMeter copy = new AnyMeter(this);

        copy._deliveryPoint = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>deliveryPoint</c> is drawn from — <c>AnyValidDeliveryPoint()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDeliveryPointId, AnyDeliveryPointId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeter WithDeliveryPoint(Func<AnyDeliveryPointId, AnyDeliveryPointId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeter copy = new AnyMeter(this);

        copy._deliveryPointRecipe = configure(_deliveryPointRecipe);
        copy._deliveryPoint       = copy._deliveryPointRecipe;

        return copy;
    }

    /// <summary>Pins <c>tariff</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeter WithTariff(TariffOption value) {
        AnyMeter copy = new AnyMeter(this);

        copy._tariff = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>tariff</c> is drawn from — <c>AnyValidTariff()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;TariffOption&gt;, AnyEnum&lt;TariffOption&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeter WithTariff(Func<AnyEnum<TariffOption>, AnyEnum<TariffOption>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeter copy = new AnyMeter(this);

        copy._tariffRecipe = configure(_tariffRecipe);
        copy._tariff       = copy._tariffRecipe;

        return copy;
    }

    /// <summary>Refines the recipe <c>commissioningReading</c> is drawn from — <c>AnyValidCommissioningReading()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMeterReading, AnyMeterReading&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeter WithCommissioningReading(Func<AnyMeterReading, AnyMeterReading> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeter copy = new AnyMeter(this);

        copy._commissioningReadingRecipe = configure(_commissioningReadingRecipe);
        copy._commissioningReading       = copy._commissioningReadingRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Meter" />.</summary>
    public Meter Generate() {
        // The commissioning reading belongs to the meter's own delivery point (a pin of the reading would be ignored, so there is none).
        DeliveryPointId deliveryPoint = _deliveryPoint.Generate();

        return new Meter(deliveryPoint,
                         _tariff.Generate(),
                         _commissioningReadingRecipe.WithDeliveryPoint(deliveryPoint).Generate());
    }

}
