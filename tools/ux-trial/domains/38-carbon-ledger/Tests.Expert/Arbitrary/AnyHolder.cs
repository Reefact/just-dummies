// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Holder --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CarbonLedger.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Holder" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyHolder : IAny<Holder> {

    private readonly IAny<string> _holderId;
    private readonly IAny<string> _legalName;
    private readonly IAny<string> _countryCode;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyHolder()
        : this(holderId:    AnyValidHolderId(),
               legalName:   AnyValidLegalName(),
               countryCode: AnyValidCountryCode()) { }

    private static AnyString AnyValidHolderId() {
        // A holder id is 4 to 12 upper-case letters or digits.
        return Any.String().AlphaNumeric().InUpperCase().WithLengthBetween(4, 12);
    }

    private static AnyString AnyValidLegalName() {
        return Any.String().NotBlank().WithMaxLength(200);
    }

    private static AnyString AnyValidCountryCode() {
        // An ISO 3166-1 alpha-2 shape: two upper-case letters.
        return Any.String().Alpha().InUpperCase().WithLength(2);
    }

    private AnyHolder(IAny<string> holderId,
                      IAny<string> legalName,
                      IAny<string> countryCode) {
        _holderId    = holderId;
        _legalName   = legalName;
        _countryCode = countryCode;
    }

    /// <summary>Pins <c>holderId</c> to a fixed value.</summary>
    public AnyHolder WithHolderId(string value) {
        return new AnyHolder(new FixedValue<string>(value), _legalName, _countryCode);
    }

    /// <summary>Draws <c>holderId</c> from <c>AnyValidHolderId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyHolder WithHolderId(Func<AnyString, AnyString> configure) {
        return new AnyHolder(configure(AnyValidHolderId()), _legalName, _countryCode);
    }

    /// <summary>Pins <c>legalName</c> to a fixed value.</summary>
    public AnyHolder WithLegalName(string value) {
        return new AnyHolder(_holderId, new FixedValue<string>(value), _countryCode);
    }

    /// <summary>Draws <c>legalName</c> from <c>AnyValidLegalName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyHolder WithLegalName(Func<AnyString, AnyString> configure) {
        return new AnyHolder(_holderId, configure(AnyValidLegalName()), _countryCode);
    }

    /// <summary>Pins <c>countryCode</c> to a fixed value.</summary>
    public AnyHolder WithCountryCode(string value) {
        return new AnyHolder(_holderId, _legalName, new FixedValue<string>(value));
    }

    /// <summary>Draws <c>countryCode</c> from <c>AnyValidCountryCode()</c>, refined by <paramref name="configure" />.</summary>
    public AnyHolder WithCountryCode(Func<AnyString, AnyString> configure) {
        return new AnyHolder(_holderId, _legalName, configure(AnyValidCountryCode()));
    }

    /// <summary>Produces one arbitrary <see cref="Holder" />.</summary>
    public Holder Generate() {
        return new Holder(_holderId.Generate(),
                          _legalName.Generate(),
                          _countryCode.Generate());
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
