// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RemittanceInformation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Payments.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RemittanceInformation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRemittanceInformation : IAny<RemittanceInformation> {

    private readonly IAny<string> _text;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRemittanceInformation()
        : this(text: AnyValidText()) { }

    private static AnyString AnyValidText() {
        return Any.String().WithChars(SepaCharacters).NotBlank().WithMaxLength(RemittanceInformation.MaximumLength);
    }

    private AnyRemittanceInformation(IAny<string> text) {
        _text = text;
    }

    /// <summary>Pins <c>text</c> to a fixed value.</summary>
    public AnyRemittanceInformation WithText(string value) {
        return new AnyRemittanceInformation(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>text</c> from <c>AnyValidText()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRemittanceInformation WithText(Func<AnyString, AnyString> configure) {
        return new AnyRemittanceInformation(configure(AnyValidText()));
    }

    /// <summary>Produces one arbitrary <see cref="RemittanceInformation" />.</summary>
    public RemittanceInformation Generate() {
        return new RemittanceInformation(_text.Generate());
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
