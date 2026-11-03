namespace LiquidityGap.Domain.Tests.Expert.Tests;

/// <summary>Cases the domain handles worse than its neighbours do. Kept, skipped, and recorded in NOTES.expert.md.</summary>
public sealed class DomainDefectTests {

    [Fact(Skip = "domain defect: a null bucket in a ladder raises NullReferenceException instead of ArgumentException, seed n/a (literal inputs)")]
    public void A_ladder_holding_a_missing_bucket_is_refused_as_an_argument_error() {
        // Arrange
        IReadOnlyList<TimeBucket> ladder = [TestBuckets.Bucket(0, 30), null!, TestBuckets.Bucket(30, null)];

        // Act / Assert
        Assert.Throws<ArgumentException>(() => TimeBucket.EnsureLadder(ladder));
    }

    [Fact(Skip = "domain defect: a missing gap raises NullReferenceException instead of an argument error, seed n/a (literal inputs)")]
    public void A_profile_holding_a_missing_gap_is_refused_as_an_argument_error() {
        // Arrange
        IReadOnlyList<BucketGap> gaps = [null!];

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new LiquidityProfile("Baseline", Any.Currency().Generate(), gaps));
    }

    [Fact(Skip = "domain defect: a deposit accepted into a portfolio as of 9999-12-01 can never be projected (ArgumentOutOfRangeException from AddMonths), seed n/a (literal inputs)")]
    public void A_deposit_accepted_by_a_portfolio_can_be_projected_by_it() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Portfolio portfolio = Any.Portfolio().WithReportingCurrency(anyCurrency).WithAsOfDate(new DateOnly(9999, 12, 1)).Generate();
        portfolio.AddDeposit(Any.NonMaturityDeposit().WithCurrency(anyCurrency).WithAssumption(new BehaviouralAssumption(0.5m, 10, 3)).Generate());

        // Act
        Exception? refusal = Record.Exception(() => portfolio.Project(Scenario.Baseline, [TestBuckets.Bucket(0, null)]));

        // Assert
        Assert.Null(refusal);
    }

}
