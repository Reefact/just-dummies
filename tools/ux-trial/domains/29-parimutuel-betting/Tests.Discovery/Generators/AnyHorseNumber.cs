// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate HorseNumber --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="HorseNumber" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyHorseNumber : IAny<HorseNumber> {

    private IAny<int> _value;

    private AnyInt32 _valueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyHorseNumber()
        : this(value: AnyValidValue()) { }

    private static AnyInt32 AnyValidValue() {
        return Any.Int32().Positive().LessThanOrEqualTo(HorseNumber.Maximum);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyHorseNumber(AnyInt32 value) {
        _value       = value;
        _valueRecipe = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyHorseNumber(AnyHorseNumber from) {
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyHorseNumber WithValue(int value) {
        AnyHorseNumber copy = new AnyHorseNumber(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>value</c> is drawn from — <c>AnyValidValue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyHorseNumber WithValue(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyHorseNumber copy = new AnyHorseNumber(this);

        copy._valueRecipe = configure(_valueRecipe);
        copy._value       = copy._valueRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="HorseNumber" />.</summary>
    public HorseNumber Generate() {
        return new HorseNumber(_value.Generate());
    }

}
