using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Discovery;

public sealed class SessionReleaseTests {

    private static readonly TimeSpan AttachedAt = TimeSpan.FromSeconds(60);

    private static MilkingSession CompletedSession() {
        MilkingSession session = Scenarios.Session().Generate();
        Scenarios.AttachAllCups(session);
        foreach (UdderQuarter quarter in session.Animal.MilkableQuarters) {
            session.RecordFlow(Scenarios.Reading(quarter, AttachedAt + TimeSpan.FromMinutes(1), flow: 2m));
            session.RecordFlow(Scenarios.Reading(quarter, AttachedAt + TimeSpan.FromMinutes(2), flow: 0m));
        }

        return session;
    }

    [Fact]
    public void A_cow_is_released_once_milking_is_completed() {
        // Arrange
        MilkingSession anySession = CompletedSession();
        Assert.Equal(SessionPhase.Completed, anySession.Phase);
        DateTimeOffset leavesAt = anySession.StartedAt + TimeSpan.FromMinutes(6);

        // Act
        anySession.Release(leavesAt);

        // Assert
        Assert.Equal(SessionPhase.Released, anySession.Phase);
        Assert.Equal(leavesAt, anySession.ReleasedAt);
    }

    [Fact]
    public void A_cow_is_released_from_the_safe_state() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        anySession.ReportAnomaly(new MilkingAnomaly(AnomalyKind.AnimalAgitation, null, TimeSpan.FromSeconds(20)));

        // Act
        anySession.Release(anySession.StartedAt + TimeSpan.FromSeconds(30));

        // Assert
        Assert.Equal(SessionPhase.Released, anySession.Phase);
    }

    [Fact]
    public void A_cow_whose_every_attachment_failed_is_released_with_an_incomplete_milking() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().WithSettings(s => s.WithMaxAttachAttempts(1)).Generate();
        TimeSpan       attachAt   = Scenarios.PrepareAllTeats(anySession) + Scenarios.LetDownDelay;
        foreach (UdderQuarter quarter in anySession.Animal.MilkableQuarters) {
            anySession.AttachCup(quarter, succeeded: false, attachAt);
        }

        // Act
        anySession.Release(anySession.StartedAt + TimeSpan.FromMinutes(3));

        // Assert
        Assert.Equal(SessionPhase.Released, anySession.Phase);
        Assert.True(anySession.IsIncomplete);
    }

    [Fact]
    public void A_cow_can_leave_at_the_very_instant_the_session_started() {
        // Arrange
        MilkingSession anySession = CompletedSession();

        // Act
        anySession.Release(anySession.StartedAt);

        // Assert
        Assert.Equal(anySession.StartedAt, anySession.ReleasedAt);
    }

    [Fact]
    public void A_cow_cannot_leave_before_the_session_started() {
        // Arrange
        MilkingSession anySession = CompletedSession();

        // Act / Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => anySession.Release(anySession.StartedAt - TimeSpan.FromTicks(1)));

        Assert.Equal("at", refusal.ParamName);
        Assert.Null(anySession.ReleasedAt);
        Assert.Equal(SessionPhase.Completed, anySession.Phase);
    }

    [Theory]
    [InlineData(SessionPhase.Identified)]
    [InlineData(SessionPhase.Preparing)]
    [InlineData(SessionPhase.Attaching)]
    [InlineData(SessionPhase.Milking)]
    public void A_cow_is_not_released_while_the_milking_is_under_way(SessionPhase phase) {
        // Arrange
        MilkingSession underWay = phase switch {
            SessionPhase.Identified => Scenarios.Session().Generate(),
            SessionPhase.Preparing  => PreparingSession(),
            SessionPhase.Attaching  => AttachingSession(),
            _                       => MilkingUnderWay(),
        };

        // Act / Assert
        Assert.Equal(phase, underWay.Phase);
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(() => underWay.Release(underWay.StartedAt + TimeSpan.FromMinutes(10)));

        Assert.Contains(phase.ToString(), refusal.Message);
        Assert.Null(underWay.ReleasedAt);
    }

    [Fact]
    public void A_released_cow_cannot_be_released_again() {
        // Arrange
        MilkingSession anySession = CompletedSession();
        anySession.Release(anySession.StartedAt + TimeSpan.FromMinutes(6));

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(() => anySession.Release(anySession.StartedAt + TimeSpan.FromMinutes(7)));
    }

    [Fact]
    public void Nothing_is_recorded_after_release() {
        // Arrange
        MilkingSession anySession = CompletedSession();
        anySession.Release(anySession.StartedAt + TimeSpan.FromMinutes(6));

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(() => anySession.RecordFlow(Scenarios.Reading(UdderQuarter.LeftFront, TimeSpan.FromMinutes(7), flow: 1m)));
        Assert.Throws<MilkingRefusedException>(() => anySession.AttachCup(UdderQuarter.LeftFront, true, TimeSpan.FromMinutes(7)));
    }

    private static MilkingSession PreparingSession() {
        MilkingSession session = Scenarios.Session().Generate();
        session.PrepareTeat(UdderQuarter.LeftFront, Scenarios.CleaningTime, Scenarios.CleaningTime);

        return session;
    }

    private static MilkingSession AttachingSession() {
        MilkingSession session = Scenarios.Session().Generate();
        Scenarios.PrepareAllTeats(session);

        return session;
    }

    private static MilkingSession MilkingUnderWay() {
        MilkingSession session = Scenarios.Session().Generate();
        Scenarios.AttachAllCups(session);
        session.RecordFlow(Scenarios.Reading(UdderQuarter.LeftFront, AttachedAt + TimeSpan.FromMinutes(1), flow: 2m));

        return session;
    }

}
