// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Combination --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Combination" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCombination : IAny<Combination> {

    private IAny<BetType>                    _betType;
    private IAny<IReadOnlyList<HorseNumber>> _horses;

    private AnyEnum<BetType>     _betTypeRecipe;
    private AnyList<HorseNumber> _horsesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCombination()
        : this(betType: AnyValidBetType(),
               horses:  AnyValidHorses()) { }

    private static AnyEnum<BetType> AnyValidBetType() {
        return Any.Enum<BetType>();
    }

    private static AnyList<HorseNumber> AnyValidHorses() {
        return Any.ListOf(new AnyHorseNumber()).Distinct();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCombination(AnyEnum<BetType>     betType,
                           AnyList<HorseNumber> horses) {
        _betType       = betType;
        _betTypeRecipe = betType;
        _horses        = horses;
        _horsesRecipe  = horses;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCombination(AnyCombination from) {
        _betType       = from._betType;
        _betTypeRecipe = from._betTypeRecipe;
        _horses        = from._horses;
        _horsesRecipe  = from._horsesRecipe;
    }

    /// <summary>Pins <c>betType</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCombination WithBetType(BetType value) {
        AnyCombination copy = new AnyCombination(this);

        copy._betType = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>betType</c> is drawn from — <c>AnyValidBetType()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;BetType&gt;, AnyEnum&lt;BetType&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCombination WithBetType(Func<AnyEnum<BetType>, AnyEnum<BetType>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCombination copy = new AnyCombination(this);

        copy._betTypeRecipe = configure(_betTypeRecipe);
        copy._betType       = copy._betTypeRecipe;

        return copy;
    }

    /// <summary>Pins <c>horses</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCombination WithHorses(IReadOnlyList<HorseNumber> value) {
        AnyCombination copy = new AnyCombination(this);

        copy._horses = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>horses</c> is drawn from — <c>AnyValidHorses()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;HorseNumber&gt;, AnyList&lt;HorseNumber&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCombination WithHorses(Func<AnyList<HorseNumber>, AnyList<HorseNumber>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCombination copy = new AnyCombination(this);

        copy._horsesRecipe = configure(_horsesRecipe);
        copy._horses       = copy._horsesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Combination" />.</summary>
    public Combination Generate() {
        BetType betType = _betType.Generate();

        // A drawn list gets the arity of the bet type drawn with it; a pinned one is taken as given.
        IAny<IReadOnlyList<HorseNumber>> horses = ReferenceEquals(_horses, _horsesRecipe) ? _horsesRecipe.WithCount(betType.Arity()) : _horses;

        return new Combination(betType, horses.Generate());
    }

}
