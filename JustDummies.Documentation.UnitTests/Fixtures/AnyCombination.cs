// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Combination --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace JustDummies.Documentation.UnitTests.Fixtures;

/// <summary>
///     A generator of arbitrary <see cref="Combination" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCombination : IAny<Combination> {

    private IAny<IReadOnlyList<HorseNumber>> _horses;

    private AnyList<HorseNumber> _horsesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCombination()
        : this(horses: AnyValidHorses()) { }

    private static AnyList<HorseNumber> AnyValidHorses() {
        return Any.ListOf(new AnyHorseNumber()).NonEmpty();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCombination(AnyList<HorseNumber> horses) {
        _horses       = horses;
        _horsesRecipe = horses;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCombination(AnyCombination from) {
        _horses       = from._horses;
        _horsesRecipe = from._horsesRecipe;
    }

    /// <summary>Pins <c>horses</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyCombination WithHorses(IReadOnlyList<HorseNumber> value) {
        AnyCombination copy = new AnyCombination(this);

        copy._horses = new FixedValue<IReadOnlyList<HorseNumber>>(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>horses</c> is drawn from — <c>AnyValidHorses()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyCombination WithHorses(Func<AnyList<HorseNumber>, AnyList<HorseNumber>> configure) {
        AnyCombination copy = new AnyCombination(this);

        copy._horsesRecipe = configure(_horsesRecipe);
        copy._horses       = copy._horsesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Combination" />.</summary>
    public Combination Generate() {
        return new Combination(_horses.Generate());
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
