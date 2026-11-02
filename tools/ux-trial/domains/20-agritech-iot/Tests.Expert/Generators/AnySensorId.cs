// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SensorId --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using AgriTech.Domain;
using JustDummies;

namespace AgriTech.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="SensorId" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySensorId : IAny<SensorId> {

    private IAny<string> _value;

    private AnyPattern _valueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySensorId()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        // A sensor id is "AG-" and six upper-case hexadecimal digits (SensorId.cs); the scaffolded
        // NotBlank().WithLength(9) honoured the length only and would be refused on almost every draw.
        return Any.StringMatching("AG-[0-9A-F]{6}");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySensorId(AnyPattern value) {
        _value       = value;
        _valueRecipe = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySensorId(AnySensorId from) {
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySensorId WithValue(string value) {
        AnySensorId copy = new AnySensorId(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>value</c> is drawn from — <c>AnyValidValue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySensorId WithValue(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySensorId copy = new AnySensorId(this);

        copy._valueRecipe = configure(_valueRecipe);
        copy._value       = copy._valueRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="SensorId" />.</summary>
    public SensorId Generate() {
        return new SensorId(_value.Generate());
    }

}
