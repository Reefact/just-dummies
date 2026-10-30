namespace Insurance.Domain;

/// <summary>A one-year (at most) car insurance policy and the claims declared against it.</summary>
public sealed class CarPolicy {

    public const decimal YoungDriverSurcharge = 1.50m;

    private readonly List<Claim> _claims;

    public CarPolicy(PolicyNumber    number,
                     Driver          driver,
                     VehicleCategory vehicle,
                     CoverageLevel   level,
                     CoveragePeriod  period,
                     Deductible      deductible,
                     BonusMalus      bonusMalus,
                     decimal         basePremium,
                     IReadOnlyList<Claim> claims) {
        ArgumentNullException.ThrowIfNull(number);
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(deductible);
        ArgumentNullException.ThrowIfNull(bonusMalus);
        ArgumentNullException.ThrowIfNull(claims);
        if (basePremium is <= 0m or > 20_000m) {
            throw new ArgumentOutOfRangeException(nameof(basePremium), basePremium, "A base premium is between 0 and 20,000.");
        }
        foreach (Claim claim in claims) {
            if (!period.Contains(claim.OccurredOn)) {
                throw new ArgumentException($"Claim {claim.Id} occurred on {claim.OccurredOn}, outside the coverage period.", nameof(claims));
            }
        }

        Number      = number;
        Driver      = driver;
        Vehicle     = vehicle;
        Level       = level;
        Period      = period;
        Deductible  = deductible;
        BonusMalus  = bonusMalus;
        BasePremium = basePremium;
        _claims     = [.. claims];
    }

    public PolicyNumber    Number      { get; }
    public Driver          Driver      { get; }
    public VehicleCategory Vehicle     { get; }
    public CoverageLevel   Level       { get; }
    public CoveragePeriod  Period      { get; }
    public Deductible      Deductible  { get; }
    public BonusMalus      BonusMalus  { get; }
    public decimal         BasePremium { get; }

    public IReadOnlyList<Claim> Claims => _claims;

    /// <summary>Base premium weighted by the bonus-malus coefficient, plus the young-driver surcharge.</summary>
    public decimal AnnualPremium() {
        decimal premium = BasePremium * (decimal)BonusMalus.Coefficient;
        if (Driver.IsYoungDriver) {
            premium *= YoungDriverSurcharge;
        }

        return decimal.Round(premium, 2, MidpointRounding.AwayFromZero);
    }

    public void DeclareClaim(Claim claim) {
        ArgumentNullException.ThrowIfNull(claim);
        if (!Period.Contains(claim.OccurredOn)) {
            throw new InvalidOperationException($"Policy {Number} does not cover {claim.OccurredOn}.");
        }

        _claims.Add(claim);
    }

    /// <summary>What this policy pays for the policyholder's own damage: only a comprehensive policy does.</summary>
    public decimal OwnDamagePayout(Claim claim) {
        ArgumentNullException.ThrowIfNull(claim);
        if (Level != CoverageLevel.Comprehensive) {
            return 0m;
        }

        decimal uncovered = claim.Damage * (1m - claim.InsuredShare);

        return Math.Max(0m, uncovered - Deductible.Amount);
    }

    public CarPolicy Renew() {
        int responsible = _claims.Count(claim => claim.Responsibility == Responsibility.Full);
        int partial     = _claims.Count(claim => claim.Responsibility == Responsibility.Partial);

        return new CarPolicy(Number, Driver, Vehicle, Level, Period.Next(), Deductible,
                             BonusMalus.AfterYear(responsible, partial), BasePremium, []);
    }

}
