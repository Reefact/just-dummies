// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CarPolicy --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace Insurance.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CarPolicy" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCarPolicy : IAny<CarPolicy> {

    private IAny<PolicyNumber>         _number;
    private IAny<Driver>               _driver;
    private IAny<VehicleCategory>      _vehicle;
    private IAny<CoverageLevel>        _level;
    private IAny<CoveragePeriod>       _period;
    private IAny<Deductible>           _deductible;
    private IAny<BonusMalus>           _bonusMalus;
    private IAny<decimal>              _basePremium;
    private IAny<IReadOnlyList<Claim>> _claims;

    private AnyPolicyNumber          _numberRecipe;
    private AnyDriver                _driverRecipe;
    private AnyEnum<VehicleCategory> _vehicleRecipe;
    private AnyEnum<CoverageLevel>   _levelRecipe;
    private AnyCoveragePeriod        _periodRecipe;
    private AnyDeductible            _deductibleRecipe;
    private AnyBonusMalus            _bonusMalusRecipe;
    private AnyDecimal               _basePremiumRecipe;
    private AnyList<Claim>           _claimsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCarPolicy()
        : this(number:      AnyValidNumber(),
               driver:      AnyValidDriver(),
               vehicle:     AnyValidVehicle(),
               level:       AnyValidLevel(),
               period:      AnyValidPeriod(),
               deductible:  AnyValidDeductible(),
               bonusMalus:  AnyValidBonusMalus(),
               basePremium: AnyValidBasePremium(),
               claims:      AnyValidClaims()) { }

    private static AnyPolicyNumber AnyValidNumber() {
        return new AnyPolicyNumber();
    }

    private static AnyDriver AnyValidDriver() {
        return new AnyDriver();
    }

    private static AnyEnum<VehicleCategory> AnyValidVehicle() {
        return Any.Enum<VehicleCategory>();
    }

    private static AnyEnum<CoverageLevel> AnyValidLevel() {
        return Any.Enum<CoverageLevel>();
    }

    private static AnyCoveragePeriod AnyValidPeriod() {
        return new AnyCoveragePeriod();
    }

    private static AnyDeductible AnyValidDeductible() {
        return new AnyDeductible();
    }

    private static AnyBonusMalus AnyValidBonusMalus() {
        return new AnyBonusMalus();
    }

    private static AnyDecimal AnyValidBasePremium() {
        return Any.Decimal().Positive().LessThanOrEqualTo(20000m);
    }

    private static AnyList<Claim> AnyValidClaims() {
        // A claim must fall inside the policy period, which this recipe cannot see: a fresh policy has none,
        // and a test that needs claims pins them from its own period (see ClaimsIn in the tests).
        return Any.ListOf(new AnyClaim()).WithCount(0);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCarPolicy(AnyPolicyNumber          number,
                         AnyDriver                driver,
                         AnyEnum<VehicleCategory> vehicle,
                         AnyEnum<CoverageLevel>   level,
                         AnyCoveragePeriod        period,
                         AnyDeductible            deductible,
                         AnyBonusMalus            bonusMalus,
                         AnyDecimal               basePremium,
                         AnyList<Claim>           claims) {
        _number            = number;
        _numberRecipe      = number;
        _driver            = driver;
        _driverRecipe      = driver;
        _vehicle           = vehicle;
        _vehicleRecipe     = vehicle;
        _level             = level;
        _levelRecipe       = level;
        _period            = period;
        _periodRecipe      = period;
        _deductible        = deductible;
        _deductibleRecipe  = deductible;
        _bonusMalus        = bonusMalus;
        _bonusMalusRecipe  = bonusMalus;
        _basePremium       = basePremium;
        _basePremiumRecipe = basePremium;
        _claims            = claims;
        _claimsRecipe      = claims;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCarPolicy(AnyCarPolicy from) {
        _number            = from._number;
        _numberRecipe      = from._numberRecipe;
        _driver            = from._driver;
        _driverRecipe      = from._driverRecipe;
        _vehicle           = from._vehicle;
        _vehicleRecipe     = from._vehicleRecipe;
        _level             = from._level;
        _levelRecipe       = from._levelRecipe;
        _period            = from._period;
        _periodRecipe      = from._periodRecipe;
        _deductible        = from._deductible;
        _deductibleRecipe  = from._deductibleRecipe;
        _bonusMalus        = from._bonusMalus;
        _bonusMalusRecipe  = from._bonusMalusRecipe;
        _basePremium       = from._basePremium;
        _basePremiumRecipe = from._basePremiumRecipe;
        _claims            = from._claims;
        _claimsRecipe      = from._claimsRecipe;
    }

    /// <summary>Pins <c>number</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCarPolicy WithNumber(PolicyNumber value) {
        AnyCarPolicy copy = new AnyCarPolicy(this);

        copy._number = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>number</c> is drawn from — <c>AnyValidNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPolicyNumber, AnyPolicyNumber&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCarPolicy WithNumber(Func<AnyPolicyNumber, AnyPolicyNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCarPolicy copy = new AnyCarPolicy(this);

        copy._numberRecipe = configure(_numberRecipe);
        copy._number       = copy._numberRecipe;

        return copy;
    }

    /// <summary>Pins <c>driver</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCarPolicy WithDriver(Driver value) {
        AnyCarPolicy copy = new AnyCarPolicy(this);

        copy._driver = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>driver</c> is drawn from — <c>AnyValidDriver()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDriver, AnyDriver&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCarPolicy WithDriver(Func<AnyDriver, AnyDriver> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCarPolicy copy = new AnyCarPolicy(this);

        copy._driverRecipe = configure(_driverRecipe);
        copy._driver       = copy._driverRecipe;

        return copy;
    }

    /// <summary>Pins <c>vehicle</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCarPolicy WithVehicle(VehicleCategory value) {
        AnyCarPolicy copy = new AnyCarPolicy(this);

        copy._vehicle = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>vehicle</c> is drawn from — <c>AnyValidVehicle()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;VehicleCategory&gt;, AnyEnum&lt;VehicleCategory&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCarPolicy WithVehicle(Func<AnyEnum<VehicleCategory>, AnyEnum<VehicleCategory>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCarPolicy copy = new AnyCarPolicy(this);

        copy._vehicleRecipe = configure(_vehicleRecipe);
        copy._vehicle       = copy._vehicleRecipe;

        return copy;
    }

    /// <summary>Pins <c>level</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCarPolicy WithLevel(CoverageLevel value) {
        AnyCarPolicy copy = new AnyCarPolicy(this);

        copy._level = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>level</c> is drawn from — <c>AnyValidLevel()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;CoverageLevel&gt;, AnyEnum&lt;CoverageLevel&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCarPolicy WithLevel(Func<AnyEnum<CoverageLevel>, AnyEnum<CoverageLevel>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCarPolicy copy = new AnyCarPolicy(this);

        copy._levelRecipe = configure(_levelRecipe);
        copy._level       = copy._levelRecipe;

        return copy;
    }

    /// <summary>Pins <c>period</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCarPolicy WithPeriod(CoveragePeriod value) {
        AnyCarPolicy copy = new AnyCarPolicy(this);

        copy._period = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>period</c> is drawn from — <c>AnyValidPeriod()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCoveragePeriod, AnyCoveragePeriod&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCarPolicy WithPeriod(Func<AnyCoveragePeriod, AnyCoveragePeriod> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCarPolicy copy = new AnyCarPolicy(this);

        copy._periodRecipe = configure(_periodRecipe);
        copy._period       = copy._periodRecipe;

        return copy;
    }

    /// <summary>Pins <c>deductible</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCarPolicy WithDeductible(Deductible value) {
        AnyCarPolicy copy = new AnyCarPolicy(this);

        copy._deductible = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>deductible</c> is drawn from — <c>AnyValidDeductible()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDeductible, AnyDeductible&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCarPolicy WithDeductible(Func<AnyDeductible, AnyDeductible> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCarPolicy copy = new AnyCarPolicy(this);

        copy._deductibleRecipe = configure(_deductibleRecipe);
        copy._deductible       = copy._deductibleRecipe;

        return copy;
    }

    /// <summary>Pins <c>bonusMalus</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCarPolicy WithBonusMalus(BonusMalus value) {
        AnyCarPolicy copy = new AnyCarPolicy(this);

        copy._bonusMalus = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>bonusMalus</c> is drawn from — <c>AnyValidBonusMalus()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBonusMalus, AnyBonusMalus&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCarPolicy WithBonusMalus(Func<AnyBonusMalus, AnyBonusMalus> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCarPolicy copy = new AnyCarPolicy(this);

        copy._bonusMalusRecipe = configure(_bonusMalusRecipe);
        copy._bonusMalus       = copy._bonusMalusRecipe;

        return copy;
    }

    /// <summary>Pins <c>basePremium</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCarPolicy WithBasePremium(decimal value) {
        AnyCarPolicy copy = new AnyCarPolicy(this);

        copy._basePremium = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>basePremium</c> is drawn from — <c>AnyValidBasePremium()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCarPolicy WithBasePremium(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCarPolicy copy = new AnyCarPolicy(this);

        copy._basePremiumRecipe = configure(_basePremiumRecipe);
        copy._basePremium       = copy._basePremiumRecipe;

        return copy;
    }

    /// <summary>Pins <c>claims</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCarPolicy WithClaims(IReadOnlyList<Claim> value) {
        AnyCarPolicy copy = new AnyCarPolicy(this);

        copy._claims = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>claims</c> is drawn from — <c>AnyValidClaims()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;Claim&gt;, AnyList&lt;Claim&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCarPolicy WithClaims(Func<AnyList<Claim>, AnyList<Claim>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCarPolicy copy = new AnyCarPolicy(this);

        copy._claimsRecipe = configure(_claimsRecipe);
        copy._claims       = copy._claimsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CarPolicy" />.</summary>
    public CarPolicy Generate() {
        return new CarPolicy(_number.Generate(),
                             _driver.Generate(),
                             _vehicle.Generate(),
                             _level.Generate(),
                             _period.Generate(),
                             _deductible.Generate(),
                             _bonusMalus.Generate(),
                             _basePremium.Generate(),
                             _claims.Generate());
    }

}
