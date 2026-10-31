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

    private readonly IAny<string> _beneficiary;
    private readonly IAny<string> _purpose;
    private readonly IAny<int>    _reportingYear;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRetirementClaim()
        : this(beneficiary:   AnyValidBeneficiary(),
               purpose:       AnyValidPurpose(),
               reportingYear: AnyValidReportingYear()) { }

    private static AnyString AnyValidBeneficiary() {
        // Non-blank, at most RetirementClaim.MaxTextLength characters.
        return Any.String().NotBlank().WithMaxLength(RetirementClaim.MaxTextLength);
    }

    private static AnyString AnyValidPurpose() {
        // Non-blank, at most RetirementClaim.MaxTextLength characters.
        return Any.String().NotBlank().WithMaxLength(RetirementClaim.MaxTextLength);
    }

    private static AnyInt32 AnyValidReportingYear() {
        return Any.Int32().Between(2000, 2100);
    }

    private AnyRetirementClaim(IAny<string> beneficiary,
                               IAny<string> purpose,
                               IAny<int>    reportingYear) {
        _beneficiary   = beneficiary;
        _purpose       = purpose;
        _reportingYear = reportingYear;
    }

    /// <summary>Pins <c>beneficiary</c> to a fixed value.</summary>
    public AnyRetirementClaim WithBeneficiary(string value) {
        return new AnyRetirementClaim(new FixedValue<string>(value), _purpose, _reportingYear);
    }

    /// <summary>Draws <c>beneficiary</c> from <c>AnyValidBeneficiary()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRetirementClaim WithBeneficiary(Func<AnyString, AnyString> configure) {
        return new AnyRetirementClaim(configure(AnyValidBeneficiary()), _purpose, _reportingYear);
    }

    /// <summary>Pins <c>purpose</c> to a fixed value.</summary>
    public AnyRetirementClaim WithPurpose(string value) {
        return new AnyRetirementClaim(_beneficiary, new FixedValue<string>(value), _reportingYear);
    }

    /// <summary>Draws <c>purpose</c> from <c>AnyValidPurpose()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRetirementClaim WithPurpose(Func<AnyString, AnyString> configure) {
        return new AnyRetirementClaim(_beneficiary, configure(AnyValidPurpose()), _reportingYear);
    }

    /// <summary>Pins <c>reportingYear</c> to a fixed value.</summary>
    public AnyRetirementClaim WithReportingYear(int value) {
        return new AnyRetirementClaim(_beneficiary, _purpose, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>reportingYear</c> from <c>AnyValidReportingYear()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRetirementClaim WithReportingYear(Func<AnyInt32, AnyInt32> configure) {
        return new AnyRetirementClaim(_beneficiary, _purpose, configure(AnyValidReportingYear()));
    }

    /// <summary>Produces one arbitrary <see cref="RetirementClaim" />.</summary>
    public RetirementClaim Generate() {
        return new RetirementClaim(_beneficiary.Generate(),
                                   _purpose.Generate(),
                                   _reportingYear.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
