// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate HorseNumber --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace JustDummies.Documentation.UnitTests.Fixtures;

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
        return Any.Int32().Between(1, 20);
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

    /// <summary>Pins <c>value</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyHorseNumber WithValue(int value) {
        AnyHorseNumber copy = new AnyHorseNumber(this);

        copy._value = new FixedValue<int>(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>value</c> is drawn from — <c>AnyValidValue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyHorseNumber WithValue(Func<AnyInt32, AnyInt32> configure) {
        AnyHorseNumber copy = new AnyHorseNumber(this);

        copy._valueRecipe = configure(_valueRecipe);
        copy._value       = copy._valueRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="HorseNumber" />.</summary>
    public HorseNumber Generate() {
        return new HorseNumber(_value.Generate());
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
