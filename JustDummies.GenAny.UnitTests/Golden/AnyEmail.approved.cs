// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Email --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Email" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyEmail : IAny<Email> {

    private IAny<string> _value;

    private AnyString _valueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyEmail()
        : this(value: AnyValidValue()) { }

    private static AnyString AnyValidValue() {
        return Any.String().NonEmpty();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyEmail(AnyString value) {
        _value       = value;
        _valueRecipe = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyEmail(AnyEmail from) {
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
    }

    /// <summary>Pins <c>value</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyEmail WithValue(string value) {
        AnyEmail copy = new AnyEmail(this);

        copy._value = new FixedValue<string>(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>value</c> is drawn from — <c>AnyValidValue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyEmail WithValue(Func<AnyString, AnyString> configure) {
        AnyEmail copy = new AnyEmail(this);

        copy._valueRecipe = configure(_valueRecipe);
        copy._value       = copy._valueRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Email" />.</summary>
    public Email Generate() {
        return Email.Create(_value.Generate());
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
