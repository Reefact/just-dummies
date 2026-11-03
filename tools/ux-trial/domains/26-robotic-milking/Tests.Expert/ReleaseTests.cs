using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Expert;

public sealed class ReleaseTests {

    private static readonly TimeSpan OneTick = TimeSpan.FromTicks(1);

    public static TheoryData<SessionPhase> PhasesWhereTheCowCannotLeave() {
        return new TheoryData<SessionPhase> { SessionPhase.Identified, SessionPhase.Preparing, SessionPhase.Attaching, SessionPhase.Milking };
    }

    public static TheoryData<SessionPhase> PhasesAlongTheProtocol() {
        return new TheoryData<SessionPhase> {
            SessionPhase.Identified, SessionPhase.Preparing, SessionPhase.Attaching, SessionPhase.Milking, SessionPhase.Completed, SessionPhase.SafeState
        };
    }

    [Theory]
    [MemberData(nameof(PhasesAlongTheProtocol))]
    public void The_phase_follows_the_milking_through_its_stages(SessionPhase phase) {
        // Arrange
        MilkingSession session = Milking.SessionOfAFullUdder().Generate();

        // Act
        Milking.DriveTo(session, phase);

        // Assert
        Assert.Equal(phase, session.Phase);
    }

    [Theory]
    [InlineData(SessionPhase.Completed)]
    [InlineData(SessionPhase.SafeState)]
    public void A_cow_leaves_once_the_milking_is_completed_or_the_box_is_safe(SessionPhase phase) {
        // Arrange
        MilkingSession session = Milking.SessionOfAFullUdder().Generate();
        Milking.DriveTo(session, phase);
        DateTimeOffset leavingAt = session.StartedAt.AddMinutes(9);

        // Act
        session.Release(leavingAt);

        // Assert
        Assert.Equal(SessionPhase.Released, session.Phase);
        Assert.Equal(leavingAt, session.ReleasedAt);
    }

    [Theory]
    [MemberData(nameof(PhasesWhereTheCowCannotLeave))]
    public void A_cow_does_not_leave_while_a_cup_may_still_be_on_or_the_milking_not_begun(SessionPhase phase) {
        // Arrange
        MilkingSession session = Milking.SessionOfAFullUdder().Generate();
        Milking.DriveTo(session, phase);

        // Act
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(() => session.Release(session.StartedAt.AddMinutes(9)));

        // Assert
        Assert.Contains(phase.ToString(), refusal.Message);
        Assert.Equal(phase, session.Phase);
        Assert.Null(session.ReleasedAt);
    }

    [Fact]
    public void A_cow_can_leave_at_the_very_moment_the_session_started() {
        // Arrange
        MilkingSession session = Any.MilkingSession().Generate();
        session.ReportAnomaly(Any.MilkingAnomaly().WithKind(AnomalyKind.EmergencyStop).Generate());

        // Act
        session.Release(session.StartedAt);

        // Assert
        Assert.Equal(session.StartedAt, session.ReleasedAt);
    }

    [Fact]
    public void A_cow_cannot_leave_before_the_session_started_and_is_still_in_the_box() {
        // Arrange
        MilkingSession session = Any.MilkingSession().Generate();
        session.ReportAnomaly(Any.MilkingAnomaly().WithKind(AnomalyKind.EmergencyStop).Generate());

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => session.Release(session.StartedAt - OneTick));

        // Assert
        Assert.Equal("at", refusal.ParamName);
        Assert.Equal(SessionPhase.SafeState, session.Phase);
        Assert.Null(session.ReleasedAt);
    }

    [Fact]
    public void A_cow_that_has_left_cannot_be_released_again() {
        // Arrange
        MilkingSession session = Milking.SessionOfAFullUdder().Generate();
        Milking.DriveTo(session, SessionPhase.Completed);
        session.Release(session.StartedAt.AddMinutes(9));

        // Act & Assert
        Assert.Throws<MilkingRefusedException>(() => session.Release(session.StartedAt.AddMinutes(10)));
        Assert.Equal(session.StartedAt.AddMinutes(9), session.ReleasedAt);
    }

    [Fact]
    public void Once_the_cow_has_left_no_more_milking_operation_is_allowed() {
        // Arrange
        MilkingSession session  = Milking.SessionOfAFullUdder().Generate();
        TimeSpan       attached = Milking.DriveTo(session, SessionPhase.Completed);
        session.Release(session.StartedAt.AddMinutes(9));
        TimeSpan cleaning = session.Settings.CleaningTime;

        // Act & Assert
        Assert.Throws<MilkingRefusedException>(() => session.PrepareTeat(UdderQuarter.LeftFront, cleaning, cleaning));
        Assert.Throws<MilkingRefusedException>(() => session.AttachCup(UdderQuarter.LeftFront, succeeded: true, attached));
        Assert.Throws<MilkingRefusedException>(() => session.RecordFlow(Milking.HealthyReading(UdderQuarter.LeftFront, attached + TimeSpan.FromMinutes(5), 4m)));
    }

}
