// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate StakePool --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using ParimutuelBetting.Domain;

namespace ParimutuelBetting.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="StakePool" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStakePool : IAny<StakePool> {

    private IAny<Race>      _race;
    private IAny<BetType>   _betType;
    private IAny<decimal>   _unitStake;
    private IAny<PoolRules> _rules;

    private AnyRace          _raceRecipe;
    private AnyEnum<BetType> _betTypeRecipe;
    private AnyDecimal       _unitStakeRecipe;
    private AnyPoolRules     _rulesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStakePool()
        : this(race:      AnyValidRace(),
               betType:   AnyValidBetType(),
               unitStake: AnyValidUnitStake(),
               rules:     AnyValidRules()) { }

    private static AnyRace AnyValidRace() {
        // A pool needs a field of at least Arity runners, and a Place pool more runners than the places it pays:
        // four runners is the smallest field every bet type and every PoolRules accepts (Trifecta 3, places paid up to 3).
        // A smaller field is a boundary: a test about it builds the race itself.
        return new AnyRace().WithFieldSize(fieldSize => fieldSize.GreaterThanOrEqualTo(4));
    }

    private static AnyEnum<BetType> AnyValidBetType() {
        return Any.Enum<BetType>();
    }

    private static AnyDecimal AnyValidUnitStake() {
        return Any.Decimal().Positive().LessThanOrEqualTo(100m).WithScale(2);
    }

    private static AnyPoolRules AnyValidRules() {
        return new AnyPoolRules();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyStakePool(AnyRace          race,
                         AnyEnum<BetType> betType,
                         AnyDecimal       unitStake,
                         AnyPoolRules     rules) {
        _race            = race;
        _raceRecipe      = race;
        _betType         = betType;
        _betTypeRecipe   = betType;
        _unitStake       = unitStake;
        _unitStakeRecipe = unitStake;
        _rules           = rules;
        _rulesRecipe     = rules;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyStakePool(AnyStakePool from) {
        _race            = from._race;
        _raceRecipe      = from._raceRecipe;
        _betType         = from._betType;
        _betTypeRecipe   = from._betTypeRecipe;
        _unitStake       = from._unitStake;
        _unitStakeRecipe = from._unitStakeRecipe;
        _rules           = from._rules;
        _rulesRecipe     = from._rulesRecipe;
    }

    /// <summary>Pins <c>race</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStakePool WithRace(Race value) {
        AnyStakePool copy = new AnyStakePool(this);

        copy._race = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>race</c> is drawn from — <c>AnyValidRace()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRace, AnyRace&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStakePool WithRace(Func<AnyRace, AnyRace> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStakePool copy = new AnyStakePool(this);

        copy._raceRecipe = configure(_raceRecipe);
        copy._race       = copy._raceRecipe;

        return copy;
    }

    /// <summary>Pins <c>betType</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStakePool WithBetType(BetType value) {
        AnyStakePool copy = new AnyStakePool(this);

        copy._betType = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>betType</c> is drawn from — <c>AnyValidBetType()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;BetType&gt;, AnyEnum&lt;BetType&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStakePool WithBetType(Func<AnyEnum<BetType>, AnyEnum<BetType>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStakePool copy = new AnyStakePool(this);

        copy._betTypeRecipe = configure(_betTypeRecipe);
        copy._betType       = copy._betTypeRecipe;

        return copy;
    }

    /// <summary>Pins <c>unitStake</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStakePool WithUnitStake(decimal value) {
        AnyStakePool copy = new AnyStakePool(this);

        copy._unitStake = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>unitStake</c> is drawn from — <c>AnyValidUnitStake()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStakePool WithUnitStake(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStakePool copy = new AnyStakePool(this);

        copy._unitStakeRecipe = configure(_unitStakeRecipe);
        copy._unitStake       = copy._unitStakeRecipe;

        return copy;
    }

    /// <summary>Pins <c>rules</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStakePool WithRules(PoolRules value) {
        AnyStakePool copy = new AnyStakePool(this);

        copy._rules = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>rules</c> is drawn from — <c>AnyValidRules()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPoolRules, AnyPoolRules&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStakePool WithRules(Func<AnyPoolRules, AnyPoolRules> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStakePool copy = new AnyStakePool(this);

        copy._rulesRecipe = configure(_rulesRecipe);
        copy._rules       = copy._rulesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="StakePool" />.</summary>
    public StakePool Generate() {
        return new StakePool(_race.Generate(),
                             _betType.Generate(),
                             _unitStake.Generate(),
                             _rules.Generate());
    }

}
