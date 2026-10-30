// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Seat --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GuardMiss.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Seat" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySeat : IAny<Seat> {

    private readonly IAny<char> _letter;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySeat()
        : this(letter: AnyValidLetter()) { }

    private static AnyChar AnyValidLetter() {
        return Any.Char();
    }

    private AnySeat(IAny<char> letter) {
        _letter = letter;
    }

    /// <summary>Pins <c>letter</c> to a fixed value.</summary>
    public AnySeat WithLetter(char value) {
        return new AnySeat(new FixedValue<char>(value));
    }

    /// <summary>Draws <c>letter</c> from <c>AnyValidLetter()</c>, refined by <paramref name="configure" />.</summary>
    public AnySeat WithLetter(Func<AnyChar, AnyChar> configure) {
        return new AnySeat(configure(AnyValidLetter()));
    }

    /// <summary>Produces one arbitrary <see cref="Seat" />.</summary>
    public Seat Generate() {
        return new Seat(_letter.Generate());
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
