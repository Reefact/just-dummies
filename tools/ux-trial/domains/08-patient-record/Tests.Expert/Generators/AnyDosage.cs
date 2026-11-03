// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Dosage --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using Clinical.Domain;
using JustDummies;

namespace Clinical.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Dosage" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDosage : IAny<Dosage> {

    private IAny<decimal>  _amount;
    private IAny<DoseUnit> _unit;
    private IAny<int>      _timesPerDay;

    private AnyDecimal        _amountRecipe;
    private AnyEnum<DoseUnit> _unitRecipe;
    private AnyInt32          _timesPerDayRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDosage()
        : this(amount:      AnyValidAmount(),
               unit:        AnyValidUnit(),
               timesPerDay: AnyValidTimesPerDay()) { }

    private static AnyDecimal AnyValidAmount() {
        return Any.Decimal().Positive().LessThanOrEqualTo(10000m).WithScale(3);
    }

    private static AnyEnum<DoseUnit> AnyValidUnit() {
        return Any.Enum<DoseUnit>();
    }

    private static AnyInt32 AnyValidTimesPerDay() {
        return Any.Int32().Positive().LessThanOrEqualTo(6);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyDosage(AnyDecimal        amount,
                      AnyEnum<DoseUnit> unit,
                      AnyInt32          timesPerDay) {
        _amount            = amount;
        _amountRecipe      = amount;
        _unit              = unit;
        _unitRecipe        = unit;
        _timesPerDay       = timesPerDay;
        _timesPerDayRecipe = timesPerDay;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyDosage(AnyDosage from) {
        _amount            = from._amount;
        _amountRecipe      = from._amountRecipe;
        _unit              = from._unit;
        _unitRecipe        = from._unitRecipe;
        _timesPerDay       = from._timesPerDay;
        _timesPerDayRecipe = from._timesPerDayRecipe;
    }

    /// <summary>Pins <c>amount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDosage WithAmount(decimal value) {
        AnyDosage copy = new AnyDosage(this);

        copy._amount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>amount</c> is drawn from — <c>AnyValidAmount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDosage WithAmount(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDosage copy = new AnyDosage(this);

        copy._amountRecipe = configure(_amountRecipe);
        copy._amount       = copy._amountRecipe;

        return copy;
    }

    /// <summary>Pins <c>unit</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDosage WithUnit(DoseUnit value) {
        AnyDosage copy = new AnyDosage(this);

        copy._unit = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>unit</c> is drawn from — <c>AnyValidUnit()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;DoseUnit&gt;, AnyEnum&lt;DoseUnit&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDosage WithUnit(Func<AnyEnum<DoseUnit>, AnyEnum<DoseUnit>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDosage copy = new AnyDosage(this);

        copy._unitRecipe = configure(_unitRecipe);
        copy._unit       = copy._unitRecipe;

        return copy;
    }

    /// <summary>Pins <c>timesPerDay</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDosage WithTimesPerDay(int value) {
        AnyDosage copy = new AnyDosage(this);

        copy._timesPerDay = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>timesPerDay</c> is drawn from — <c>AnyValidTimesPerDay()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDosage WithTimesPerDay(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDosage copy = new AnyDosage(this);

        copy._timesPerDayRecipe = configure(_timesPerDayRecipe);
        copy._timesPerDay       = copy._timesPerDayRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Dosage" />.</summary>
    public Dosage Generate() {
        return new Dosage(_amount.Generate(),
                          _unit.Generate(),
                          _timesPerDay.Generate());
    }

}
