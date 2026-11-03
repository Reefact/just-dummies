// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Taxpayer --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Taxpayer" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTaxpayer : IAny<Taxpayer> {

    private IAny<TaxpayerId>                   _id;
    private IAny<DateOnly>                     _dateOfBirth;
    private IAny<JurisdictionCode>             _residence;
    private IAny<int>                          _householdSize;
    private IAny<IEnumerable<IncomeComponent>> _incomes;

    private AnyTaxpayerId       _idRecipe;
    private AnyDateOnly         _dateOfBirthRecipe;
    private AnyJurisdictionCode _residenceRecipe;
    private AnyInt32            _householdSizeRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTaxpayer()
        : this(id:            AnyValidId(),
               dateOfBirth:   AnyValidDateOfBirth(),
               residence:     AnyValidResidence(),
               householdSize: AnyValidHouseholdSize(),
               incomes:       AnyValidIncomes()) { }

    private static AnyTaxpayerId AnyValidId() {
        return new AnyTaxpayerId();
    }

    private static AnyDateOnly AnyValidDateOfBirth() {
        // Born between the domain's earliest date and the end of 2000, so any schedule year (2000 to 2099) postdates the birth.
        return Any.DateOnly().Between(Taxpayer.EarliestDateOfBirth, new DateOnly(2000, 12, 31));
    }

    private static AnyJurisdictionCode AnyValidResidence() {
        return new AnyJurisdictionCode();
    }

    private static AnyInt32 AnyValidHouseholdSize() {
        return Any.Int32().Positive().LessThanOrEqualTo(Taxpayer.MaxHouseholdSize);
    }

    private static IAny<IEnumerable<IncomeComponent>> AnyValidIncomes() {
        return Any.ListOf(new AnyIncomeComponent()).WithCountBetween(0, 5).As(l => (IEnumerable<IncomeComponent>)l);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTaxpayer(AnyTaxpayerId                      id,
                        AnyDateOnly                        dateOfBirth,
                        AnyJurisdictionCode                residence,
                        AnyInt32                           householdSize,
                        IAny<IEnumerable<IncomeComponent>> incomes) {
        _id                  = id;
        _idRecipe            = id;
        _dateOfBirth         = dateOfBirth;
        _dateOfBirthRecipe   = dateOfBirth;
        _residence           = residence;
        _residenceRecipe     = residence;
        _householdSize       = householdSize;
        _householdSizeRecipe = householdSize;
        _incomes             = incomes;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTaxpayer(AnyTaxpayer from) {
        _id                  = from._id;
        _idRecipe            = from._idRecipe;
        _dateOfBirth         = from._dateOfBirth;
        _dateOfBirthRecipe   = from._dateOfBirthRecipe;
        _residence           = from._residence;
        _residenceRecipe     = from._residenceRecipe;
        _householdSize       = from._householdSize;
        _householdSizeRecipe = from._householdSizeRecipe;
        _incomes             = from._incomes;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTaxpayer WithId(TaxpayerId value) {
        AnyTaxpayer copy = new AnyTaxpayer(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTaxpayerId, AnyTaxpayerId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTaxpayer WithId(Func<AnyTaxpayerId, AnyTaxpayerId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTaxpayer copy = new AnyTaxpayer(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>dateOfBirth</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTaxpayer WithDateOfBirth(DateOnly value) {
        AnyTaxpayer copy = new AnyTaxpayer(this);

        copy._dateOfBirth = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>dateOfBirth</c> is drawn from — <c>AnyValidDateOfBirth()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTaxpayer WithDateOfBirth(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTaxpayer copy = new AnyTaxpayer(this);

        copy._dateOfBirthRecipe = configure(_dateOfBirthRecipe);
        copy._dateOfBirth       = copy._dateOfBirthRecipe;

        return copy;
    }

    /// <summary>Pins <c>residence</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTaxpayer WithResidence(JurisdictionCode value) {
        AnyTaxpayer copy = new AnyTaxpayer(this);

        copy._residence = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>residence</c> is drawn from — <c>AnyValidResidence()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyJurisdictionCode, AnyJurisdictionCode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTaxpayer WithResidence(Func<AnyJurisdictionCode, AnyJurisdictionCode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTaxpayer copy = new AnyTaxpayer(this);

        copy._residenceRecipe = configure(_residenceRecipe);
        copy._residence       = copy._residenceRecipe;

        return copy;
    }

    /// <summary>Pins <c>householdSize</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTaxpayer WithHouseholdSize(int value) {
        AnyTaxpayer copy = new AnyTaxpayer(this);

        copy._householdSize = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>householdSize</c> is drawn from — <c>AnyValidHouseholdSize()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTaxpayer WithHouseholdSize(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTaxpayer copy = new AnyTaxpayer(this);

        copy._householdSizeRecipe = configure(_householdSizeRecipe);
        copy._householdSize       = copy._householdSizeRecipe;

        return copy;
    }

    /// <summary>Pins <c>incomes</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type.</summary>
    public AnyTaxpayer WithIncomes(IEnumerable<IncomeComponent> value) {
        AnyTaxpayer copy = new AnyTaxpayer(this);

        copy._incomes = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Taxpayer" />.</summary>
    public Taxpayer Generate() {
        return new Taxpayer(_id.Generate(),
                            _dateOfBirth.Generate(),
                            _residence.Generate(),
                            _householdSize.Generate(),
                            _incomes.Generate());
    }

}
