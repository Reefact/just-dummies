// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Scenario --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace LiquidityGap.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Scenario" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyScenario : IAny<Scenario> {

    private IAny<string>  _name;
    private IAny<decimal> _inflowHaircut;
    private IAny<decimal> _outflowMultiplier;
    private IAny<decimal> _depositStableShareHaircut;

    private AnyString  _nameRecipe;
    private AnyDecimal _inflowHaircutRecipe;
    private AnyDecimal _outflowMultiplierRecipe;
    private AnyDecimal _depositStableShareHaircutRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyScenario()
        : this(name:                      AnyValidName(),
               inflowHaircut:             AnyValidInflowHaircut(),
               outflowMultiplier:         AnyValidOutflowMultiplier(),
               depositStableShareHaircut: AnyValidDepositStableShareHaircut()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank().WithMaxLength(64);
    }

    private static AnyDecimal AnyValidInflowHaircut() {
        return Any.Decimal().GreaterThanOrEqualTo(0m).LessThan(1m);
    }

    private static AnyDecimal AnyValidOutflowMultiplier() {
        return Any.Decimal().Between(1m, 3m);
    }

    private static AnyDecimal AnyValidDepositStableShareHaircut() {
        return Any.Decimal().Between(0m, 1m);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyScenario(AnyString  name,
                        AnyDecimal inflowHaircut,
                        AnyDecimal outflowMultiplier,
                        AnyDecimal depositStableShareHaircut) {
        _name                            = name;
        _nameRecipe                      = name;
        _inflowHaircut                   = inflowHaircut;
        _inflowHaircutRecipe             = inflowHaircut;
        _outflowMultiplier               = outflowMultiplier;
        _outflowMultiplierRecipe         = outflowMultiplier;
        _depositStableShareHaircut       = depositStableShareHaircut;
        _depositStableShareHaircutRecipe = depositStableShareHaircut;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyScenario(AnyScenario from) {
        _name                            = from._name;
        _nameRecipe                      = from._nameRecipe;
        _inflowHaircut                   = from._inflowHaircut;
        _inflowHaircutRecipe             = from._inflowHaircutRecipe;
        _outflowMultiplier               = from._outflowMultiplier;
        _outflowMultiplierRecipe         = from._outflowMultiplierRecipe;
        _depositStableShareHaircut       = from._depositStableShareHaircut;
        _depositStableShareHaircutRecipe = from._depositStableShareHaircutRecipe;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyScenario WithName(string value) {
        AnyScenario copy = new AnyScenario(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyScenario WithName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyScenario copy = new AnyScenario(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins <c>inflowHaircut</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyScenario WithInflowHaircut(decimal value) {
        AnyScenario copy = new AnyScenario(this);

        copy._inflowHaircut = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>inflowHaircut</c> is drawn from — <c>AnyValidInflowHaircut()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyScenario WithInflowHaircut(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyScenario copy = new AnyScenario(this);

        copy._inflowHaircutRecipe = configure(_inflowHaircutRecipe);
        copy._inflowHaircut       = copy._inflowHaircutRecipe;

        return copy;
    }

    /// <summary>Pins <c>outflowMultiplier</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyScenario WithOutflowMultiplier(decimal value) {
        AnyScenario copy = new AnyScenario(this);

        copy._outflowMultiplier = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>outflowMultiplier</c> is drawn from — <c>AnyValidOutflowMultiplier()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyScenario WithOutflowMultiplier(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyScenario copy = new AnyScenario(this);

        copy._outflowMultiplierRecipe = configure(_outflowMultiplierRecipe);
        copy._outflowMultiplier       = copy._outflowMultiplierRecipe;

        return copy;
    }

    /// <summary>Pins <c>depositStableShareHaircut</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyScenario WithDepositStableShareHaircut(decimal value) {
        AnyScenario copy = new AnyScenario(this);

        copy._depositStableShareHaircut = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>depositStableShareHaircut</c> is drawn from — <c>AnyValidDepositStableShareHaircut()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyScenario WithDepositStableShareHaircut(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyScenario copy = new AnyScenario(this);

        copy._depositStableShareHaircutRecipe = configure(_depositStableShareHaircutRecipe);
        copy._depositStableShareHaircut       = copy._depositStableShareHaircutRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Scenario" />.</summary>
    public Scenario Generate() {
        return new Scenario(_name.Generate(),
                            _inflowHaircut.Generate(),
                            _outflowMultiplier.Generate(),
                            _depositStableShareHaircut.Generate());
    }

}
