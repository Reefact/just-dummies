// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CarPolicy --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace Insurance.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CarPolicy" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCarPolicy : IAny<CarPolicy> {

    private readonly IAny<PolicyNumber>         _number;
    private readonly IAny<Driver>               _driver;
    private readonly IAny<VehicleCategory>      _vehicle;
    private readonly IAny<CoverageLevel>        _level;
    private readonly IAny<CoveragePeriod>       _period;
    private readonly IAny<Deductible>           _deductible;
    private readonly IAny<BonusMalus>           _bonusMalus;
    private readonly IAny<decimal>              _basePremium;
    private readonly IAny<IReadOnlyList<Claim>> _claims;

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
        // Flagged because the claims guard reads the period; the period itself is honoured by AnyCoveragePeriod.
        return new AnyCoveragePeriod();
    }

    private static AnyDeductible AnyValidDeductible() {
        return new AnyDeductible();
    }

    private static AnyBonusMalus AnyValidBonusMalus() {
        return new AnyBonusMalus();
    }

    private static AnyDecimal AnyValidBasePremium() {
        // The guard is a relational pattern: basePremium is <= 0m or > 20_000m.
        return Any.Decimal().GreaterThan(0m).LessThanOrEqualTo(20_000m).WithScale(2);
    }

    private static AnyList<Claim> AnyValidClaims() {
        // Every claim must fall inside the coverage period: a cross-parameter rule that Generate() applies,
        // since this base cannot see the period that will be drawn beside it.
        return Any.ListOf(new AnyClaim()).WithMaxCount(5);
    }

    private AnyCarPolicy(IAny<PolicyNumber>         number,
                         IAny<Driver>               driver,
                         IAny<VehicleCategory>      vehicle,
                         IAny<CoverageLevel>        level,
                         IAny<CoveragePeriod>       period,
                         IAny<Deductible>           deductible,
                         IAny<BonusMalus>           bonusMalus,
                         IAny<decimal>              basePremium,
                         IAny<IReadOnlyList<Claim>> claims) {
        _number      = number;
        _driver      = driver;
        _vehicle     = vehicle;
        _level       = level;
        _period      = period;
        _deductible  = deductible;
        _bonusMalus  = bonusMalus;
        _basePremium = basePremium;
        _claims      = claims;
    }

    /// <summary>Pins <c>number</c> to a fixed value.</summary>
    public AnyCarPolicy WithNumber(PolicyNumber value) {
        return new AnyCarPolicy(new FixedValue<PolicyNumber>(value), _driver, _vehicle, _level, _period, _deductible, _bonusMalus, _basePremium, _claims);
    }

    /// <summary>Draws <c>number</c> from <c>AnyValidNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCarPolicy WithNumber(Func<AnyPolicyNumber, AnyPolicyNumber> configure) {
        return new AnyCarPolicy(configure(AnyValidNumber()), _driver, _vehicle, _level, _period, _deductible, _bonusMalus, _basePremium, _claims);
    }

    /// <summary>Pins <c>driver</c> to a fixed value.</summary>
    public AnyCarPolicy WithDriver(Driver value) {
        return new AnyCarPolicy(_number, new FixedValue<Driver>(value), _vehicle, _level, _period, _deductible, _bonusMalus, _basePremium, _claims);
    }

    /// <summary>Draws <c>driver</c> from <c>AnyValidDriver()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCarPolicy WithDriver(Func<AnyDriver, AnyDriver> configure) {
        return new AnyCarPolicy(_number, configure(AnyValidDriver()), _vehicle, _level, _period, _deductible, _bonusMalus, _basePremium, _claims);
    }

    /// <summary>Pins <c>vehicle</c> to a fixed value.</summary>
    public AnyCarPolicy WithVehicle(VehicleCategory value) {
        return new AnyCarPolicy(_number, _driver, new FixedValue<VehicleCategory>(value), _level, _period, _deductible, _bonusMalus, _basePremium, _claims);
    }

    /// <summary>Draws <c>vehicle</c> from <c>AnyValidVehicle()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCarPolicy WithVehicle(Func<AnyEnum<VehicleCategory>, AnyEnum<VehicleCategory>> configure) {
        return new AnyCarPolicy(_number, _driver, configure(AnyValidVehicle()), _level, _period, _deductible, _bonusMalus, _basePremium, _claims);
    }

    /// <summary>Pins <c>level</c> to a fixed value.</summary>
    public AnyCarPolicy WithLevel(CoverageLevel value) {
        return new AnyCarPolicy(_number, _driver, _vehicle, new FixedValue<CoverageLevel>(value), _period, _deductible, _bonusMalus, _basePremium, _claims);
    }

    /// <summary>Draws <c>level</c> from <c>AnyValidLevel()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCarPolicy WithLevel(Func<AnyEnum<CoverageLevel>, AnyEnum<CoverageLevel>> configure) {
        return new AnyCarPolicy(_number, _driver, _vehicle, configure(AnyValidLevel()), _period, _deductible, _bonusMalus, _basePremium, _claims);
    }

    /// <summary>Pins <c>period</c> to a fixed value.</summary>
    public AnyCarPolicy WithPeriod(CoveragePeriod value) {
        return new AnyCarPolicy(_number, _driver, _vehicle, _level, new FixedValue<CoveragePeriod>(value), _deductible, _bonusMalus, _basePremium, _claims);
    }

    /// <summary>Draws <c>period</c> from <c>AnyValidPeriod()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCarPolicy WithPeriod(Func<AnyCoveragePeriod, AnyCoveragePeriod> configure) {
        return new AnyCarPolicy(_number, _driver, _vehicle, _level, configure(AnyValidPeriod()), _deductible, _bonusMalus, _basePremium, _claims);
    }

    /// <summary>Pins <c>deductible</c> to a fixed value.</summary>
    public AnyCarPolicy WithDeductible(Deductible value) {
        return new AnyCarPolicy(_number, _driver, _vehicle, _level, _period, new FixedValue<Deductible>(value), _bonusMalus, _basePremium, _claims);
    }

    /// <summary>Draws <c>deductible</c> from <c>AnyValidDeductible()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCarPolicy WithDeductible(Func<AnyDeductible, AnyDeductible> configure) {
        return new AnyCarPolicy(_number, _driver, _vehicle, _level, _period, configure(AnyValidDeductible()), _bonusMalus, _basePremium, _claims);
    }

    /// <summary>Pins <c>bonusMalus</c> to a fixed value.</summary>
    public AnyCarPolicy WithBonusMalus(BonusMalus value) {
        return new AnyCarPolicy(_number, _driver, _vehicle, _level, _period, _deductible, new FixedValue<BonusMalus>(value), _basePremium, _claims);
    }

    /// <summary>Draws <c>bonusMalus</c> from <c>AnyValidBonusMalus()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCarPolicy WithBonusMalus(Func<AnyBonusMalus, AnyBonusMalus> configure) {
        return new AnyCarPolicy(_number, _driver, _vehicle, _level, _period, _deductible, configure(AnyValidBonusMalus()), _basePremium, _claims);
    }

    /// <summary>Pins <c>basePremium</c> to a fixed value.</summary>
    public AnyCarPolicy WithBasePremium(decimal value) {
        return new AnyCarPolicy(_number, _driver, _vehicle, _level, _period, _deductible, _bonusMalus, new FixedValue<decimal>(value), _claims);
    }

    /// <summary>Draws <c>basePremium</c> from <c>AnyValidBasePremium()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCarPolicy WithBasePremium(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyCarPolicy(_number, _driver, _vehicle, _level, _period, _deductible, _bonusMalus, configure(AnyValidBasePremium()), _claims);
    }

    /// <summary>Pins <c>claims</c> to a fixed value.</summary>
    public AnyCarPolicy WithClaims(IReadOnlyList<Claim> value) {
        return new AnyCarPolicy(_number, _driver, _vehicle, _level, _period, _deductible, _bonusMalus, _basePremium, new FixedValue<IReadOnlyList<Claim>>(value));
    }

    /// <summary>Draws <c>claims</c> from <c>AnyValidClaims()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCarPolicy WithClaims(Func<AnyList<Claim>, AnyList<Claim>> configure) {
        return new AnyCarPolicy(_number, _driver, _vehicle, _level, _period, _deductible, _bonusMalus, _basePremium, configure(AnyValidClaims()));
    }

    /// <summary>Produces one arbitrary <see cref="CarPolicy" />.</summary>
    public CarPolicy Generate() {
        CoveragePeriod period = _period.Generate();

        return new CarPolicy(_number.Generate(),
                             _driver.Generate(),
                             _vehicle.Generate(),
                             _level.Generate(),
                             period,
                             _deductible.Generate(),
                             _bonusMalus.Generate(),
                             _basePremium.Generate(),
                             ClaimsWithin(period));
    }

    // Hand-written: pinned claims are passed as given (the test chose them); drawn claims are moved into
    // the drawn period, keeping everything else the generator drew.
    private IReadOnlyList<Claim> ClaimsWithin(CoveragePeriod period) {
        IReadOnlyList<Claim> claims = _claims.Generate();
        if (_claims is FixedValue<IReadOnlyList<Claim>>) {
            return claims;
        }

        AnyDateOnly anyDayCovered = Any.DateOnly().Between(period.Start, period.End);

        return claims.Select(claim => new Claim(claim.Id, anyDayCovered.Generate(), claim.Damage, claim.Responsibility)).ToList();
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
