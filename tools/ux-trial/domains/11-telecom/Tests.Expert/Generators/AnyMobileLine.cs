// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MobileLine --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using Telecom.Domain;

namespace Telecom.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="MobileLine" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMobileLine : IAny<MobileLine> {

    private IAny<Msisdn>   _number;
    private IAny<Imei>     _handset;
    private IAny<Plan>     _plan;
    private IAny<DateOnly> _activatedOn;

    private AnyMsisdn   _numberRecipe;
    private AnyImei     _handsetRecipe;
    private AnyPlan     _planRecipe;
    private AnyDateOnly _activatedOnRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMobileLine()
        : this(number:      AnyValidNumber(),
               handset:     AnyValidHandset(),
               plan:        AnyValidPlan(),
               activatedOn: AnyValidActivatedOn()) { }

    private static AnyMsisdn AnyValidNumber() {
        return new AnyMsisdn();
    }

    private static AnyImei AnyValidHandset() {
        return new AnyImei();
    }

    private static AnyPlan AnyValidPlan() {
        return new AnyPlan();
    }

    private static AnyDateOnly AnyValidActivatedOn() {
        return Any.DateOnly().AfterOrEqualTo(new DateOnly(2000, 1, 1));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMobileLine(AnyMsisdn   number,
                          AnyImei     handset,
                          AnyPlan     plan,
                          AnyDateOnly activatedOn) {
        _number            = number;
        _numberRecipe      = number;
        _handset           = handset;
        _handsetRecipe     = handset;
        _plan              = plan;
        _planRecipe        = plan;
        _activatedOn       = activatedOn;
        _activatedOnRecipe = activatedOn;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMobileLine(AnyMobileLine from) {
        _number            = from._number;
        _numberRecipe      = from._numberRecipe;
        _handset           = from._handset;
        _handsetRecipe     = from._handsetRecipe;
        _plan              = from._plan;
        _planRecipe        = from._planRecipe;
        _activatedOn       = from._activatedOn;
        _activatedOnRecipe = from._activatedOnRecipe;
    }

    /// <summary>Pins <c>number</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMobileLine WithNumber(Msisdn value) {
        AnyMobileLine copy = new AnyMobileLine(this);

        copy._number = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>number</c> is drawn from — <c>AnyValidNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMsisdn, AnyMsisdn&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMobileLine WithNumber(Func<AnyMsisdn, AnyMsisdn> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMobileLine copy = new AnyMobileLine(this);

        copy._numberRecipe = configure(_numberRecipe);
        copy._number       = copy._numberRecipe;

        return copy;
    }

    /// <summary>Pins <c>handset</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMobileLine WithHandset(Imei value) {
        AnyMobileLine copy = new AnyMobileLine(this);

        copy._handset = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>handset</c> is drawn from — <c>AnyValidHandset()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyImei, AnyImei&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMobileLine WithHandset(Func<AnyImei, AnyImei> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMobileLine copy = new AnyMobileLine(this);

        copy._handsetRecipe = configure(_handsetRecipe);
        copy._handset       = copy._handsetRecipe;

        return copy;
    }

    /// <summary>Pins <c>plan</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMobileLine WithPlan(Plan value) {
        AnyMobileLine copy = new AnyMobileLine(this);

        copy._plan = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>plan</c> is drawn from — <c>AnyValidPlan()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPlan, AnyPlan&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMobileLine WithPlan(Func<AnyPlan, AnyPlan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMobileLine copy = new AnyMobileLine(this);

        copy._planRecipe = configure(_planRecipe);
        copy._plan       = copy._planRecipe;

        return copy;
    }

    /// <summary>Pins <c>activatedOn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMobileLine WithActivatedOn(DateOnly value) {
        AnyMobileLine copy = new AnyMobileLine(this);

        copy._activatedOn = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>activatedOn</c> is drawn from — <c>AnyValidActivatedOn()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMobileLine WithActivatedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMobileLine copy = new AnyMobileLine(this);

        copy._activatedOnRecipe = configure(_activatedOnRecipe);
        copy._activatedOn       = copy._activatedOnRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="MobileLine" />.</summary>
    public MobileLine Generate() {
        return new MobileLine(_number.Generate(),
                              _handset.Generate(),
                              _plan.Generate(),
                              _activatedOn.Generate());
    }

}
