// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RetirementClaim --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CarbonLedger.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RetirementClaim" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRetirementClaim : IAny<RetirementClaim> {

    private IAny<string> _beneficiary;
    private IAny<string> _purpose;
    private IAny<int>    _reportingYear;

    private AnyString _beneficiaryRecipe;
    private AnyString _purposeRecipe;
    private AnyInt32  _reportingYearRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRetirementClaim()
        : this(beneficiary:   AnyValidBeneficiary(),
               purpose:       AnyValidPurpose(),
               reportingYear: AnyValidReportingYear()) { }

    private static AnyString AnyValidBeneficiary() {
        return Any.String().NotBlank().WithMaxLength(RetirementClaim.MaxTextLength);
    }

    private static AnyString AnyValidPurpose() {
        return Any.String().NotBlank().WithMaxLength(RetirementClaim.MaxTextLength);
    }

    private static AnyInt32 AnyValidReportingYear() {
        return Any.Int32().Between(RetirementClaim.MinReportingYear, RetirementClaim.MaxReportingYear);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRetirementClaim(AnyString beneficiary,
                               AnyString purpose,
                               AnyInt32  reportingYear) {
        _beneficiary         = beneficiary;
        _beneficiaryRecipe   = beneficiary;
        _purpose             = purpose;
        _purposeRecipe       = purpose;
        _reportingYear       = reportingYear;
        _reportingYearRecipe = reportingYear;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRetirementClaim(AnyRetirementClaim from) {
        _beneficiary         = from._beneficiary;
        _beneficiaryRecipe   = from._beneficiaryRecipe;
        _purpose             = from._purpose;
        _purposeRecipe       = from._purposeRecipe;
        _reportingYear       = from._reportingYear;
        _reportingYearRecipe = from._reportingYearRecipe;
    }

    /// <summary>Pins <c>beneficiary</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRetirementClaim WithBeneficiary(string value) {
        AnyRetirementClaim copy = new AnyRetirementClaim(this);

        copy._beneficiary = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>beneficiary</c> is drawn from — <c>AnyValidBeneficiary()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRetirementClaim WithBeneficiary(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRetirementClaim copy = new AnyRetirementClaim(this);

        copy._beneficiaryRecipe = configure(_beneficiaryRecipe);
        copy._beneficiary       = copy._beneficiaryRecipe;

        return copy;
    }

    /// <summary>Pins <c>purpose</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRetirementClaim WithPurpose(string value) {
        AnyRetirementClaim copy = new AnyRetirementClaim(this);

        copy._purpose = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>purpose</c> is drawn from — <c>AnyValidPurpose()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRetirementClaim WithPurpose(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRetirementClaim copy = new AnyRetirementClaim(this);

        copy._purposeRecipe = configure(_purposeRecipe);
        copy._purpose       = copy._purposeRecipe;

        return copy;
    }

    /// <summary>Pins <c>reportingYear</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRetirementClaim WithReportingYear(int value) {
        AnyRetirementClaim copy = new AnyRetirementClaim(this);

        copy._reportingYear = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>reportingYear</c> is drawn from — <c>AnyValidReportingYear()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRetirementClaim WithReportingYear(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRetirementClaim copy = new AnyRetirementClaim(this);

        copy._reportingYearRecipe = configure(_reportingYearRecipe);
        copy._reportingYear       = copy._reportingYearRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="RetirementClaim" />.</summary>
    public RetirementClaim Generate() {
        return new RetirementClaim(_beneficiary.Generate(),
                                   _purpose.Generate(),
                                   _reportingYear.Generate());
    }

}
