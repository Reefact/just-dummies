// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ContainerId --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using VesselStowage.Domain;

namespace VesselStowage.Domain.Tests.Generators;

/// <summary>
///     A generator of arbitrary <see cref="ContainerId" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyContainerId : IAny<ContainerId> {

    private IAny<string> _value;

    private IAny<string> _valueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyContainerId()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        // ISO 6346: owner code (3 letters), category U/J/Z, 6-digit serial, then the check digit computed from the first ten.
        return Any.StringMatching("[A-Z]{3}[UJZ][0-9]{6}").As(firstTen => firstTen + ContainerId.ComputeCheckDigit(firstTen));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyContainerId(IAny<string> value) {
        _value       = value;
        _valueRecipe = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyContainerId(AnyContainerId from) {
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyContainerId WithValue(string value) {
        AnyContainerId copy = new AnyContainerId(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ContainerId" />.</summary>
    public ContainerId Generate() {
        return new ContainerId(_value.Generate());
    }

}
