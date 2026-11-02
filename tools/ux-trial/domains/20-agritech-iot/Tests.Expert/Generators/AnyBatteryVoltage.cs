// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BatteryVoltage --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using AgriTech.Domain;
using JustDummies;

namespace AgriTech.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="BatteryVoltage" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBatteryVoltage : IAny<BatteryVoltage> {

    private IAny<Half> _volts;

    private AnyHalf _voltsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBatteryVoltage()
        : this(volts: AnyValidVolts()) { }

    private static AnyHalf AnyValidVolts() {
        return Any.Half().Between(BatteryVoltage.Minimum, BatteryVoltage.Maximum);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBatteryVoltage(AnyHalf volts) {
        _volts       = volts;
        _voltsRecipe = volts;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBatteryVoltage(AnyBatteryVoltage from) {
        _volts       = from._volts;
        _voltsRecipe = from._voltsRecipe;
    }

    /// <summary>Pins <c>volts</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBatteryVoltage WithVolts(Half value) {
        AnyBatteryVoltage copy = new AnyBatteryVoltage(this);

        copy._volts = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>volts</c> is drawn from — <c>AnyValidVolts()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyHalf, AnyHalf&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBatteryVoltage WithVolts(Func<AnyHalf, AnyHalf> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBatteryVoltage copy = new AnyBatteryVoltage(this);

        copy._voltsRecipe = configure(_voltsRecipe);
        copy._volts       = copy._voltsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="BatteryVoltage" />.</summary>
    public BatteryVoltage Generate() {
        return new BatteryVoltage(_volts.Generate());
    }

}
