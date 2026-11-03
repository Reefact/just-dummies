// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Nir --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using Clinical.Domain;
using JustDummies;

namespace HealthcareClaims.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Nir" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyNir : IAny<Nir> {

    private IAny<string> _value;

    private IAny<string> _valueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyNir()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        return Any.StringMatching(@"[12]\d{2}(0[1-9]|1[0-2])(\d{2}|2[AB])\d{6}")
                  .As(body => body + Nir.ComputeKey(body).ToString("00"));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyNir(IAny<string> value) {
        _value       = value;
        _valueRecipe = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyNir(AnyNir from) {
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyNir WithValue(string value) {
        AnyNir copy = new AnyNir(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Nir" />.</summary>
    public Nir Generate() {
        return new Nir(_value.Generate());
    }

}
