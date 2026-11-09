// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Lei --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using FxSettlement.Domain;
using JustDummies;

namespace FxSettlement.Domain.Tests.Regular;

/// <summary>
///     A generator of arbitrary <see cref="Lei" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLei : IAny<Lei> {

    private IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLei()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        return Any.StringMatching("[A-Z0-9]{18}").As(WithCheckDigits);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyLei(IAny<string> value) {
        _value       = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyLei(AnyLei from) {
        _value       = from._value;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLei WithValue(string value) {
        AnyLei copy = new AnyLei(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    private static string WithCheckDigits(string body) {
        // ISO 7064 MOD 97-10: the two digits that bring the whole code to 1 modulo 97.
        int remainder = 0;
        foreach (char c in body + "00") {
            int digitValue = char.IsDigit(c) ? c - '0' : c - 'A' + 10;
            remainder = digitValue >= 10 ? (remainder * 100 + digitValue) % 97 : (remainder * 10 + digitValue) % 97;
        }

        return body + (98 - remainder).ToString("00");
    }

    /// <summary>Produces one arbitrary <see cref="Lei" />.</summary>
    public Lei Generate() {
        return new Lei(_value.Generate());
    }

}
