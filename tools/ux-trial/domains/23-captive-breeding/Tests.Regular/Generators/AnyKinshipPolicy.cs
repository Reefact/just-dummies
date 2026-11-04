// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate KinshipPolicy --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace CaptiveBreeding.Domain;

/// <summary>
///     A generator of arbitrary <see cref="KinshipPolicy" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyKinshipPolicy : IAny<KinshipPolicy> {

    private IAny<KinshipCoefficient> _maximumPairKinship;
    private IAny<int>                _maximumPairingsPerIndividual;

    private AnyKinshipCoefficient _maximumPairKinshipRecipe;
    private AnyInt32              _maximumPairingsPerIndividualRecipe;
    private AnyInt32              _minimumBreedingAgeYearsRecipe;
    private AnyInt32              _maximumBreedingAgeYearsRecipe;

    private int? _minimumBreedingAgeYearsPin;
    private int? _maximumBreedingAgeYearsPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyKinshipPolicy()
        : this(maximumPairKinship:           AnyValidMaximumPairKinship(),
               maximumPairingsPerIndividual: AnyValidMaximumPairingsPerIndividual(),
               minimumBreedingAgeYears:      AnyValidMinimumBreedingAgeYears(),
               maximumBreedingAgeYears:      AnyValidMaximumBreedingAgeYears()) { }

    private static AnyKinshipCoefficient AnyValidMaximumPairKinship() {
        return new AnyKinshipCoefficient().WithValue(v => v.LessThanOrEqualTo(0.25));
    }

    private static AnyInt32 AnyValidMaximumPairingsPerIndividual() {
        return Any.Int32().Positive().LessThanOrEqualTo(10);
    }

    private static AnyInt32 AnyValidMinimumBreedingAgeYears() {
        return Any.Int32().Between(0, 50);
    }

    private static AnyInt32 AnyValidMaximumBreedingAgeYears() {
        return Any.Int32().LessThanOrEqualTo(100);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyKinshipPolicy(AnyKinshipCoefficient maximumPairKinship,
                             AnyInt32              maximumPairingsPerIndividual,
                             AnyInt32              minimumBreedingAgeYears,
                             AnyInt32              maximumBreedingAgeYears) {
        _maximumPairKinship                                = maximumPairKinship;
        _maximumPairKinshipRecipe                          = maximumPairKinship;
        _maximumPairingsPerIndividual                      = maximumPairingsPerIndividual;
        _maximumPairingsPerIndividualRecipe                = maximumPairingsPerIndividual;
        _minimumBreedingAgeYearsRecipe                     = minimumBreedingAgeYears;
        _maximumBreedingAgeYearsRecipe                     = maximumBreedingAgeYears;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyKinshipPolicy(AnyKinshipPolicy from) {
        _maximumPairKinship                                = from._maximumPairKinship;
        _maximumPairKinshipRecipe                          = from._maximumPairKinshipRecipe;
        _maximumPairingsPerIndividual                      = from._maximumPairingsPerIndividual;
        _maximumPairingsPerIndividualRecipe                = from._maximumPairingsPerIndividualRecipe;
        _minimumBreedingAgeYearsRecipe                     = from._minimumBreedingAgeYearsRecipe;
        _minimumBreedingAgeYearsPin                        = from._minimumBreedingAgeYearsPin;
        _maximumBreedingAgeYearsRecipe                     = from._maximumBreedingAgeYearsRecipe;
        _maximumBreedingAgeYearsPin                        = from._maximumBreedingAgeYearsPin;
    }

    /// <summary>Draws <c>minimumBreedingAgeYears</c> and <c>maximumBreedingAgeYears</c>, <c>minimumBreedingAgeYears</c> always before <c>maximumBreedingAgeYears</c> as the constructor requires: together when neither is pinned, one bounded by the other's pin, and two pins as they are, for the constructor to judge.</summary>
    private (int MinimumBreedingAgeYears, int MaximumBreedingAgeYears) MinimumBreedingAgeYearsAndMaximumBreedingAgeYears() {
        if (_minimumBreedingAgeYearsPin is { } minimumBreedingAgeYears) { return (minimumBreedingAgeYears, _maximumBreedingAgeYearsPin ?? _maximumBreedingAgeYearsRecipe.GreaterThan(minimumBreedingAgeYears).Generate()); }
        if (_maximumBreedingAgeYearsPin is { } maximumBreedingAgeYears) { return (_minimumBreedingAgeYearsRecipe.LessThan(maximumBreedingAgeYears).Generate(), maximumBreedingAgeYears); }

        return Any.Pair(_minimumBreedingAgeYearsRecipe, _maximumBreedingAgeYearsRecipe).Having.Item1LessThanItem2().Generate();
    }

    /// <summary>Pins <c>maximumPairKinship</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyKinshipPolicy WithMaximumPairKinship(KinshipCoefficient value) {
        AnyKinshipPolicy copy = new AnyKinshipPolicy(this);

        copy._maximumPairKinship = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>maximumPairKinship</c> is drawn from — <c>AnyValidMaximumPairKinship()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyKinshipCoefficient, AnyKinshipCoefficient&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyKinshipPolicy WithMaximumPairKinship(Func<AnyKinshipCoefficient, AnyKinshipCoefficient> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyKinshipPolicy copy = new AnyKinshipPolicy(this);

        copy._maximumPairKinshipRecipe = configure(_maximumPairKinshipRecipe);
        copy._maximumPairKinship       = copy._maximumPairKinshipRecipe;

        return copy;
    }

    /// <summary>Pins <c>maximumPairingsPerIndividual</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyKinshipPolicy WithMaximumPairingsPerIndividual(int value) {
        AnyKinshipPolicy copy = new AnyKinshipPolicy(this);

        copy._maximumPairingsPerIndividual = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>maximumPairingsPerIndividual</c> is drawn from — <c>AnyValidMaximumPairingsPerIndividual()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyKinshipPolicy WithMaximumPairingsPerIndividual(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyKinshipPolicy copy = new AnyKinshipPolicy(this);

        copy._maximumPairingsPerIndividualRecipe = configure(_maximumPairingsPerIndividualRecipe);
        copy._maximumPairingsPerIndividual       = copy._maximumPairingsPerIndividualRecipe;

        return copy;
    }

    /// <summary>Pins <c>minimumBreedingAgeYears</c> to a fixed value, which bounds <c>maximumBreedingAgeYears</c>; its recipe is kept for a later refinement.</summary>
    public AnyKinshipPolicy WithMinimumBreedingAgeYears(int value) {
        AnyKinshipPolicy copy = new AnyKinshipPolicy(this);

        copy._minimumBreedingAgeYearsPin                        = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>minimumBreedingAgeYears</c> is drawn from — <c>AnyValidMinimumBreedingAgeYears()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>maximumBreedingAgeYears</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyKinshipPolicy WithMinimumBreedingAgeYears(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyKinshipPolicy copy = new AnyKinshipPolicy(this);

        copy._minimumBreedingAgeYearsRecipe                     = configure(_minimumBreedingAgeYearsRecipe);
        copy._minimumBreedingAgeYearsPin                        = null;

        return copy;
    }

    /// <summary>Pins <c>maximumBreedingAgeYears</c> to a fixed value, which bounds <c>minimumBreedingAgeYears</c>; its recipe is kept for a later refinement.</summary>
    public AnyKinshipPolicy WithMaximumBreedingAgeYears(int value) {
        AnyKinshipPolicy copy = new AnyKinshipPolicy(this);

        copy._maximumBreedingAgeYearsPin                        = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>maximumBreedingAgeYears</c> is drawn from — <c>AnyValidMaximumBreedingAgeYears()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>minimumBreedingAgeYears</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyKinshipPolicy WithMaximumBreedingAgeYears(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyKinshipPolicy copy = new AnyKinshipPolicy(this);

        copy._maximumBreedingAgeYearsRecipe                     = configure(_maximumBreedingAgeYearsRecipe);
        copy._maximumBreedingAgeYearsPin                        = null;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="KinshipPolicy" />.</summary>
    public KinshipPolicy Generate() {
        (int minimumBreedingAgeYears, int maximumBreedingAgeYears) = MinimumBreedingAgeYearsAndMaximumBreedingAgeYears();

        return new KinshipPolicy(_maximumPairKinship.Generate(),
                                 _maximumPairingsPerIndividual.Generate(),
                                 minimumBreedingAgeYears,
                                 maximumBreedingAgeYears);
    }

}
