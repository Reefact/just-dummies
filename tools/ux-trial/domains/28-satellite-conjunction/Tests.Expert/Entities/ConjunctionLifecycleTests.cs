using JustDummies;

using Xunit;

namespace SatelliteConjunction.Domain.Tests.Expert.Entities;

// Screened -> Assessed -> (ManeuverPlanned) -> Closed. The level of the risk is what the transitions depend on, so it is
// chosen with Encounter.OfRisk; everything else about the conjunction is a dummy.
public sealed class ConjunctionLifecycleTests {

    // ---- planning a manoeuvre -------------------------------------------------------------------------------------

    [Fact]
    public void A_manoeuvre_is_planned_once_the_latest_assessment_is_high() {
        // Arrange
        Conjunction conjunction = AssessedAt(RiskLevel.High);

        // Act
        conjunction.PlanManeuver();

        // Assert
        Assert.Equal(ConjunctionStatus.ManeuverPlanned, conjunction.Status);
    }

    [Theory]
    [InlineData(RiskLevel.Elevated)]
    [InlineData(RiskLevel.Low)]
    public void No_manoeuvre_is_warranted_below_a_high_risk(RiskLevel level) {
        // Arrange
        Conjunction conjunction = AssessedAt(level);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => conjunction.PlanManeuver());

        // Assert
        Assert.Contains(level.ToString(), refusal.Message);
        Assert.Equal(ConjunctionStatus.Assessed, conjunction.Status);
    }

    [Fact]
    public void A_manoeuvre_cannot_be_planned_before_the_conjunction_is_assessed() {
        // Arrange
        Conjunction screened = Any.Conjunction().Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => screened.PlanManeuver());

        // Assert
        Assert.Contains("Screened", refusal.Message);
        Assert.Equal(ConjunctionStatus.Screened, screened.Status);
    }

    [Fact]
    public void A_manoeuvre_cannot_be_planned_twice() {
        // Arrange
        Conjunction conjunction = AssessedAt(RiskLevel.High);
        conjunction.PlanManeuver();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => conjunction.PlanManeuver());
        Assert.Equal(ConjunctionStatus.ManeuverPlanned, conjunction.Status);
    }

    [Fact]
    public void A_manoeuvre_cannot_be_planned_for_a_closed_conjunction() {
        // Arrange
        Conjunction conjunction = AssessedAt(RiskLevel.Low);
        conjunction.Close(AnyReason());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => conjunction.PlanManeuver());
        Assert.Equal(ConjunctionStatus.Closed, conjunction.Status);
    }

    // ---- closing ------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(RiskLevel.Elevated)]
    [InlineData(RiskLevel.Low)]
    public void An_assessed_conjunction_is_closed_with_its_reason_unless_the_risk_is_high(RiskLevel level) {
        // Arrange
        Conjunction conjunction = AssessedAt(level);
        string      anyReason   = AnyReason();

        // Act
        conjunction.Close(anyReason);

        // Assert
        Assert.Equal(ConjunctionStatus.Closed, conjunction.Status);
        Assert.Equal(anyReason, conjunction.ClosureReason);
    }

    [Fact]
    public void A_conjunction_whose_manoeuvre_is_planned_can_be_closed() {
        // Arrange
        Conjunction conjunction = AssessedAt(RiskLevel.High);
        conjunction.PlanManeuver();
        string anyReason = AnyReason();

        // Act
        conjunction.Close(anyReason);

        // Assert
        Assert.Equal(ConjunctionStatus.Closed, conjunction.Status);
        Assert.Equal(anyReason, conjunction.ClosureReason);
    }

    [Fact]
    public void A_high_risk_conjunction_cannot_be_closed_before_a_manoeuvre_is_planned() {
        // Arrange
        Conjunction conjunction = AssessedAt(RiskLevel.High);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => conjunction.Close(AnyReason()));

        // Assert
        Assert.Contains("manoeuvre", refusal.Message);
        Assert.Equal(ConjunctionStatus.Assessed, conjunction.Status);
        Assert.Null(conjunction.ClosureReason);
    }

    [Fact]
    public void A_conjunction_that_was_never_assessed_cannot_be_closed() {
        // Arrange
        Conjunction screened = Any.Conjunction().Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => screened.Close(AnyReason()));

        // Assert
        Assert.Contains("assessed", refusal.Message);
        Assert.Equal(ConjunctionStatus.Screened, screened.Status);
    }

    [Fact]
    public void A_conjunction_can_only_be_closed_once_and_keeps_its_first_reason() {
        // Arrange
        Conjunction conjunction  = AssessedAt(RiskLevel.Low);
        string      firstReason  = AnyReason();
        conjunction.Close(firstReason);

        // Act
        Assert.Throws<InvalidOperationException>(() => conjunction.Close(AnyReason()));

        // Assert
        Assert.Equal(firstReason, conjunction.ClosureReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t\r\n ")]
    public void A_blank_reason_is_refused_and_the_conjunction_stays_open(string? blank) {
        // Arrange
        Conjunction conjunction = AssessedAt(RiskLevel.Low);

        // Act
        Assert.ThrowsAny<ArgumentException>(() => conjunction.Close(blank!));

        // Assert
        Assert.Equal(ConjunctionStatus.Assessed, conjunction.Status);
        Assert.Null(conjunction.ClosureReason);
    }

    [Fact]
    public void A_reason_is_kept_exactly_as_it_was_given() {
        // Arrange
        Conjunction conjunction = AssessedAt(RiskLevel.Low);

        // Act
        conjunction.Close("  Secondary re-entered on 2026-03-02.  ");

        // Assert
        Assert.Equal("  Secondary re-entered on 2026-03-02.  ", conjunction.ClosureReason);
    }

    // ---- the whole way -----------------------------------------------------------------------------------------

    [Fact]
    public void A_high_risk_conjunction_goes_through_screened_assessed_planned_and_closed_in_that_order() {
        // Arrange
        Conjunction conjunction = Encounter.OfRisk(RiskLevel.High).Build();
        List<ConjunctionStatus> statuses = [conjunction.Status];

        // Act
        conjunction.Assess();
        statuses.Add(conjunction.Status);
        conjunction.PlanManeuver();
        statuses.Add(conjunction.Status);
        conjunction.Close(AnyReason());
        statuses.Add(conjunction.Status);

        // Assert
        Assert.Equal([ConjunctionStatus.Screened, ConjunctionStatus.Assessed, ConjunctionStatus.ManeuverPlanned, ConjunctionStatus.Closed], statuses);
    }

    private static Conjunction AssessedAt(RiskLevel level) {
        Conjunction conjunction = Encounter.OfRisk(level).Build();
        RiskAssessment assessment = conjunction.Assess();

        // The setup must have produced the level the test is about, or the test would be about something else.
        Assert.Equal(level, assessment.Level);

        return conjunction;
    }

    private static string AnyReason() {
        return Any.String().Alpha().WithLengthBetween(1, 60).Generate();
    }

}
