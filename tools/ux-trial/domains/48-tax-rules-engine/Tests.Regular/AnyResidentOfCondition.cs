// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ResidentOfCondition --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ResidentOfCondition" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyResidentOfCondition : IAny<ResidentOfCondition> {

    private IAny<JurisdictionCode> _jurisdiction;

    private AnyJurisdictionCode _jurisdictionRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyResidentOfCondition()
        : this(jurisdiction: AnyValidJurisdiction()) { }

    private static AnyJurisdictionCode AnyValidJurisdiction() {
        return new AnyJurisdictionCode();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyResidentOfCondition(AnyJurisdictionCode jurisdiction) {
        _jurisdiction       = jurisdiction;
        _jurisdictionRecipe = jurisdiction;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyResidentOfCondition(AnyResidentOfCondition from) {
        _jurisdiction       = from._jurisdiction;
        _jurisdictionRecipe = from._jurisdictionRecipe;
    }

    /// <summary>Pins <c>jurisdiction</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyResidentOfCondition WithJurisdiction(JurisdictionCode value) {
        AnyResidentOfCondition copy = new AnyResidentOfCondition(this);

        copy._jurisdiction = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>jurisdiction</c> is drawn from — <c>AnyValidJurisdiction()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyJurisdictionCode, AnyJurisdictionCode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyResidentOfCondition WithJurisdiction(Func<AnyJurisdictionCode, AnyJurisdictionCode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyResidentOfCondition copy = new AnyResidentOfCondition(this);

        copy._jurisdictionRecipe = configure(_jurisdictionRecipe);
        copy._jurisdiction       = copy._jurisdictionRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ResidentOfCondition" />.</summary>
    public ResidentOfCondition Generate() {
        return new ResidentOfCondition(_jurisdiction.Generate());
    }

}
