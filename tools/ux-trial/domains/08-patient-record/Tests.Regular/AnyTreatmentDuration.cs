// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TreatmentDuration --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Clinical.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TreatmentDuration" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTreatmentDuration : IAny<TreatmentDuration> {

    private IAny<int> _days;

    private AnyInt32 _daysRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTreatmentDuration()
        : this(days: AnyValidDays()) { }

    private static AnyInt32 AnyValidDays() {
        return Any.Int32().Between(1, TreatmentDuration.MaxDays);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTreatmentDuration(AnyInt32 days) {
        _days       = days;
        _daysRecipe = days;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTreatmentDuration(AnyTreatmentDuration from) {
        _days       = from._days;
        _daysRecipe = from._daysRecipe;
    }

    /// <summary>Pins <c>days</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTreatmentDuration WithDays(int value) {
        AnyTreatmentDuration copy = new AnyTreatmentDuration(this);

        copy._days = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>days</c> is drawn from — <c>AnyValidDays()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTreatmentDuration WithDays(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTreatmentDuration copy = new AnyTreatmentDuration(this);

        copy._daysRecipe = configure(_daysRecipe);
        copy._days       = copy._daysRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TreatmentDuration" />.</summary>
    public TreatmentDuration Generate() {
        return new TreatmentDuration(_days.Generate());
    }

}
