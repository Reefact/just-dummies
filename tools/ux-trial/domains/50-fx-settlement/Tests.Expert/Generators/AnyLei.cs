// Scaffolded by `any` (JustDummies), then completed by hand: the base now computes the ISO 17442 check digits.

using System;

using FxSettlement.Domain;
using JustDummies;

namespace FxSettlement.Domain.Tests.Expert;

/// <summary>A generator of arbitrary, well-formed <see cref="Lei" /> values.</summary>
public sealed partial class AnyLei : IAny<Lei> {

    private IAny<string> _value;

    public AnyLei()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        // 18 letters or digits, then the two check digits that make the whole code 1 modulo 97.
        return Any.StringMatching("[A-Z0-9]{18}").As(body => body + CheckDigitsOf(body));
    }

    /// <summary>The two ISO 7064 check digits that make <paramref name="body" /> followed by them 1 modulo 97.</summary>
    public static string CheckDigitsOf(string body) {
        int remainder = 0;
        foreach (char c in body + "00") {
            int digitValue = char.IsDigit(c) ? c - '0' : c - 'A' + 10;
            remainder = digitValue >= 10 ? (remainder * 100 + digitValue) % 97 : (remainder * 10 + digitValue) % 97;
        }

        return (98 - remainder).ToString("D2");
    }

    private AnyLei(IAny<string> value) {
        _value = value;
    }

    private AnyLei(AnyLei from) {
        _value = from._value;
    }

    /// <summary>Pins the LEI's value; it is checked by <see cref="Lei" /> itself on every draw.</summary>
    public AnyLei WithValue(string value) {
        AnyLei copy = new AnyLei(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    public Lei Generate() {
        return new Lei(_value.Generate());
    }

}
