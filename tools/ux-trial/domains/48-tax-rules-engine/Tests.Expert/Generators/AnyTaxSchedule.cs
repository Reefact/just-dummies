// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TaxSchedule --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TaxSchedule" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTaxSchedule : IAny<TaxSchedule> {

    private IAny<JurisdictionCode> _jurisdiction;
    private IAny<int>              _year;

    private AnyJurisdictionCode _jurisdictionRecipe;
    private AnyInt32            _yearRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTaxSchedule()
        : this(jurisdiction: AnyValidJurisdiction(),
               year:         AnyValidYear()) { }

    private static AnyJurisdictionCode AnyValidJurisdiction() {
        return new AnyJurisdictionCode();
    }

    private static AnyInt32 AnyValidYear() {
        return Any.Int32().Between(TaxSchedule.FirstYear, TaxSchedule.LastYear);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTaxSchedule(AnyJurisdictionCode jurisdiction,
                           AnyInt32            year) {
        _jurisdiction       = jurisdiction;
        _jurisdictionRecipe = jurisdiction;
        _year               = year;
        _yearRecipe         = year;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTaxSchedule(AnyTaxSchedule from) {
        _jurisdiction       = from._jurisdiction;
        _jurisdictionRecipe = from._jurisdictionRecipe;
        _year               = from._year;
        _yearRecipe         = from._yearRecipe;
    }

    /// <summary>Pins <c>jurisdiction</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTaxSchedule WithJurisdiction(JurisdictionCode value) {
        AnyTaxSchedule copy = new AnyTaxSchedule(this);

        copy._jurisdiction = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>jurisdiction</c> is drawn from — <c>AnyValidJurisdiction()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyJurisdictionCode, AnyJurisdictionCode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTaxSchedule WithJurisdiction(Func<AnyJurisdictionCode, AnyJurisdictionCode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTaxSchedule copy = new AnyTaxSchedule(this);

        copy._jurisdictionRecipe = configure(_jurisdictionRecipe);
        copy._jurisdiction       = copy._jurisdictionRecipe;

        return copy;
    }

    /// <summary>Pins <c>year</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTaxSchedule WithYear(int value) {
        AnyTaxSchedule copy = new AnyTaxSchedule(this);

        copy._year = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>year</c> is drawn from — <c>AnyValidYear()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTaxSchedule WithYear(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTaxSchedule copy = new AnyTaxSchedule(this);

        copy._yearRecipe = configure(_yearRecipe);
        copy._year       = copy._yearRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TaxSchedule" />.</summary>
    public TaxSchedule Generate() {
        return new TaxSchedule(_jurisdiction.Generate(),
                               _year.Generate());
    }

}
