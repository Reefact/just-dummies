// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Randomisation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using ClinicalTrial.Domain;
using JustDummies;

namespace ClinicalTrial.Domain.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Randomisation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRandomisation : IAny<Randomisation> {

    private IAny<int>          _number;
    private IAny<DateOnly>     _date;
    private IAny<TreatmentArm> _arm;

    private AnyInt32              _numberRecipe;
    private AnyDateOnly           _dateRecipe;
    private AnyEnum<TreatmentArm> _armRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRandomisation()
        : this(number: AnyValidNumber(),
               date:   AnyValidDate(),
               arm:    AnyValidArm()) { }

    private static AnyInt32 AnyValidNumber() {
        return Any.Int32().Positive().LessThanOrEqualTo(9999);
    }

    private static AnyDateOnly AnyValidDate() {
        return Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2030, 12, 31));
    }

    private static AnyEnum<TreatmentArm> AnyValidArm() {
        return Any.Enum<TreatmentArm>();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRandomisation(AnyInt32              number,
                             AnyDateOnly           date,
                             AnyEnum<TreatmentArm> arm) {
        _number       = number;
        _numberRecipe = number;
        _date         = date;
        _dateRecipe   = date;
        _arm          = arm;
        _armRecipe    = arm;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRandomisation(AnyRandomisation from) {
        _number       = from._number;
        _numberRecipe = from._numberRecipe;
        _date         = from._date;
        _dateRecipe   = from._dateRecipe;
        _arm          = from._arm;
        _armRecipe    = from._armRecipe;
    }

    /// <summary>Pins <c>number</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRandomisation WithNumber(int value) {
        AnyRandomisation copy = new AnyRandomisation(this);

        copy._number = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>number</c> is drawn from — <c>AnyValidNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRandomisation WithNumber(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRandomisation copy = new AnyRandomisation(this);

        copy._numberRecipe = configure(_numberRecipe);
        copy._number       = copy._numberRecipe;

        return copy;
    }

    /// <summary>Pins <c>date</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRandomisation WithDate(DateOnly value) {
        AnyRandomisation copy = new AnyRandomisation(this);

        copy._date = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>date</c> is drawn from — <c>AnyValidDate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRandomisation WithDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRandomisation copy = new AnyRandomisation(this);

        copy._dateRecipe = configure(_dateRecipe);
        copy._date       = copy._dateRecipe;

        return copy;
    }

    /// <summary>Pins <c>arm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRandomisation WithArm(TreatmentArm value) {
        AnyRandomisation copy = new AnyRandomisation(this);

        copy._arm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>arm</c> is drawn from — <c>AnyValidArm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;TreatmentArm&gt;, AnyEnum&lt;TreatmentArm&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRandomisation WithArm(Func<AnyEnum<TreatmentArm>, AnyEnum<TreatmentArm>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRandomisation copy = new AnyRandomisation(this);

        copy._armRecipe = configure(_armRecipe);
        copy._arm       = copy._armRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Randomisation" />.</summary>
    public Randomisation Generate() {
        return new Randomisation(_number.Generate(),
                                 _date.Generate(),
                                 _arm.Generate());
    }

}
