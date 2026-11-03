// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BreedingPlan --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using CaptiveBreeding.Domain;
using JustDummies;

namespace CaptiveBreeding.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="BreedingPlan" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBreedingPlan : IAny<BreedingPlan> {

    private IAny<Pedigree>      _pedigree;
    private IAny<KinshipPolicy> _policy;
    private IAny<DateOnly>      _seasonStart;

    private AnyPedigree      _pedigreeRecipe;
    private AnyKinshipPolicy _policyRecipe;
    private AnyDateOnly      _seasonStartRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBreedingPlan()
        : this(pedigree:    AnyValidPedigree(),
               policy:      AnyValidPolicy(),
               seasonStart: AnyValidSeasonStart()) { }

    private static AnyPedigree AnyValidPedigree() {
        return new AnyPedigree();
    }

    private static AnyKinshipPolicy AnyValidPolicy() {
        return new AnyKinshipPolicy();
    }

    private static AnyDateOnly AnyValidSeasonStart() {
        return Any.DateOnly();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBreedingPlan(AnyPedigree      pedigree,
                            AnyKinshipPolicy policy,
                            AnyDateOnly      seasonStart) {
        _pedigree          = pedigree;
        _pedigreeRecipe    = pedigree;
        _policy            = policy;
        _policyRecipe      = policy;
        _seasonStart       = seasonStart;
        _seasonStartRecipe = seasonStart;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBreedingPlan(AnyBreedingPlan from) {
        _pedigree          = from._pedigree;
        _pedigreeRecipe    = from._pedigreeRecipe;
        _policy            = from._policy;
        _policyRecipe      = from._policyRecipe;
        _seasonStart       = from._seasonStart;
        _seasonStartRecipe = from._seasonStartRecipe;
    }

    /// <summary>Pins <c>pedigree</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBreedingPlan WithPedigree(Pedigree value) {
        AnyBreedingPlan copy = new AnyBreedingPlan(this);

        copy._pedigree = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>pedigree</c> is drawn from — <c>AnyValidPedigree()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPedigree, AnyPedigree&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBreedingPlan WithPedigree(Func<AnyPedigree, AnyPedigree> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBreedingPlan copy = new AnyBreedingPlan(this);

        copy._pedigreeRecipe = configure(_pedigreeRecipe);
        copy._pedigree       = copy._pedigreeRecipe;

        return copy;
    }

    /// <summary>Pins <c>policy</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBreedingPlan WithPolicy(KinshipPolicy value) {
        AnyBreedingPlan copy = new AnyBreedingPlan(this);

        copy._policy = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>policy</c> is drawn from — <c>AnyValidPolicy()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyKinshipPolicy, AnyKinshipPolicy&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBreedingPlan WithPolicy(Func<AnyKinshipPolicy, AnyKinshipPolicy> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBreedingPlan copy = new AnyBreedingPlan(this);

        copy._policyRecipe = configure(_policyRecipe);
        copy._policy       = copy._policyRecipe;

        return copy;
    }

    /// <summary>Pins <c>seasonStart</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBreedingPlan WithSeasonStart(DateOnly value) {
        AnyBreedingPlan copy = new AnyBreedingPlan(this);

        copy._seasonStart = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>seasonStart</c> is drawn from — <c>AnyValidSeasonStart()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBreedingPlan WithSeasonStart(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBreedingPlan copy = new AnyBreedingPlan(this);

        copy._seasonStartRecipe = configure(_seasonStartRecipe);
        copy._seasonStart       = copy._seasonStartRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="BreedingPlan" />.</summary>
    public BreedingPlan Generate() {
        return new BreedingPlan(_pedigree.Generate(),
                                _policy.Generate(),
                                _seasonStart.Generate());
    }

}
