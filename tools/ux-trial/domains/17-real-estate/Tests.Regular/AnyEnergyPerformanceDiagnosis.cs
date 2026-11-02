// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate EnergyPerformanceDiagnosis --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace RealEstate.Domain;

/// <summary>
///     A generator of arbitrary <see cref="EnergyPerformanceDiagnosis" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyEnergyPerformanceDiagnosis : IAny<EnergyPerformanceDiagnosis> {

    private IAny<int>          _primaryEnergyKwhPerSquareMeterYear;
    private IAny<int>          _emissionsKgCo2PerSquareMeterYear;
    private IAny<DateOnly>     _issuedOn;

    private AnyInt32              _primaryEnergyKwhPerSquareMeterYearRecipe;
    private AnyInt32              _emissionsKgCo2PerSquareMeterYearRecipe;
    private AnyDateOnly           _issuedOnRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyEnergyPerformanceDiagnosis()
        : this(primaryEnergyKwhPerSquareMeterYear: AnyValidPrimaryEnergyKwhPerSquareMeterYear(),
               emissionsKgCo2PerSquareMeterYear:   AnyValidEmissionsKgCo2PerSquareMeterYear(),
               issuedOn:                           AnyValidIssuedOn()) { }

    private static AnyInt32 AnyValidPrimaryEnergyKwhPerSquareMeterYear() {
        return Any.Int32().Between(0, 600);
    }

    private static AnyInt32 AnyValidEmissionsKgCo2PerSquareMeterYear() {
        return Any.Int32().Between(0, 150);
    }

    private static AnyDateOnly AnyValidIssuedOn() {
        return Any.DateOnly().Between(EnergyPerformanceDiagnosis.ReformDate, EnergyPerformanceDiagnosis.LatestIssueDate);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyEnergyPerformanceDiagnosis(AnyInt32              primaryEnergyKwhPerSquareMeterYear,
                                          AnyInt32              emissionsKgCo2PerSquareMeterYear,
                                          AnyDateOnly           issuedOn) {
        _primaryEnergyKwhPerSquareMeterYear       = primaryEnergyKwhPerSquareMeterYear;
        _primaryEnergyKwhPerSquareMeterYearRecipe = primaryEnergyKwhPerSquareMeterYear;
        _emissionsKgCo2PerSquareMeterYear         = emissionsKgCo2PerSquareMeterYear;
        _emissionsKgCo2PerSquareMeterYearRecipe   = emissionsKgCo2PerSquareMeterYear;
        _issuedOn                                 = issuedOn;
        _issuedOnRecipe                           = issuedOn;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyEnergyPerformanceDiagnosis(AnyEnergyPerformanceDiagnosis from) {
        _primaryEnergyKwhPerSquareMeterYear       = from._primaryEnergyKwhPerSquareMeterYear;
        _primaryEnergyKwhPerSquareMeterYearRecipe = from._primaryEnergyKwhPerSquareMeterYearRecipe;
        _emissionsKgCo2PerSquareMeterYear         = from._emissionsKgCo2PerSquareMeterYear;
        _emissionsKgCo2PerSquareMeterYearRecipe   = from._emissionsKgCo2PerSquareMeterYearRecipe;
        _issuedOn                                 = from._issuedOn;
        _issuedOnRecipe                           = from._issuedOnRecipe;
    }

    /// <summary>Pins <c>primaryEnergyKwhPerSquareMeterYear</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEnergyPerformanceDiagnosis WithPrimaryEnergyKwhPerSquareMeterYear(int value) {
        AnyEnergyPerformanceDiagnosis copy = new AnyEnergyPerformanceDiagnosis(this);

        copy._primaryEnergyKwhPerSquareMeterYear = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>primaryEnergyKwhPerSquareMeterYear</c> is drawn from — <c>AnyValidPrimaryEnergyKwhPerSquareMeterYear()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEnergyPerformanceDiagnosis WithPrimaryEnergyKwhPerSquareMeterYear(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEnergyPerformanceDiagnosis copy = new AnyEnergyPerformanceDiagnosis(this);

        copy._primaryEnergyKwhPerSquareMeterYearRecipe = configure(_primaryEnergyKwhPerSquareMeterYearRecipe);
        copy._primaryEnergyKwhPerSquareMeterYear       = copy._primaryEnergyKwhPerSquareMeterYearRecipe;

        return copy;
    }

    /// <summary>Pins <c>emissionsKgCo2PerSquareMeterYear</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEnergyPerformanceDiagnosis WithEmissionsKgCo2PerSquareMeterYear(int value) {
        AnyEnergyPerformanceDiagnosis copy = new AnyEnergyPerformanceDiagnosis(this);

        copy._emissionsKgCo2PerSquareMeterYear = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>emissionsKgCo2PerSquareMeterYear</c> is drawn from — <c>AnyValidEmissionsKgCo2PerSquareMeterYear()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEnergyPerformanceDiagnosis WithEmissionsKgCo2PerSquareMeterYear(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEnergyPerformanceDiagnosis copy = new AnyEnergyPerformanceDiagnosis(this);

        copy._emissionsKgCo2PerSquareMeterYearRecipe = configure(_emissionsKgCo2PerSquareMeterYearRecipe);
        copy._emissionsKgCo2PerSquareMeterYear       = copy._emissionsKgCo2PerSquareMeterYearRecipe;

        return copy;
    }

    /// <summary>Pins <c>issuedOn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEnergyPerformanceDiagnosis WithIssuedOn(DateOnly value) {
        AnyEnergyPerformanceDiagnosis copy = new AnyEnergyPerformanceDiagnosis(this);

        copy._issuedOn = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>issuedOn</c> is drawn from — <c>AnyValidIssuedOn()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEnergyPerformanceDiagnosis WithIssuedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEnergyPerformanceDiagnosis copy = new AnyEnergyPerformanceDiagnosis(this);

        copy._issuedOnRecipe = configure(_issuedOnRecipe);
        copy._issuedOn       = copy._issuedOnRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="EnergyPerformanceDiagnosis" />.</summary>
    public EnergyPerformanceDiagnosis Generate() {
        // The rating is derived from the figures, never drawn: the constructor refuses a declared rating that differs from the computed one.
        int energy    = _primaryEnergyKwhPerSquareMeterYear.Generate();
        int emissions = _emissionsKgCo2PerSquareMeterYear.Generate();

        return new EnergyPerformanceDiagnosis(energy, emissions, EnergyPerformanceDiagnosis.Rate(energy, emissions), _issuedOn.Generate());
    }

}
