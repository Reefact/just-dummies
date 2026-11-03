using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Discovery;

public sealed class SessionPreparationTests {

    private static readonly TimeSpan Brushing = Scenarios.CleaningTime;   // 10 s

    [Fact]
    public void Brushing_a_teat_prepares_its_quarter_and_starts_the_preparation() {
        // Arrange
        MilkingSession anySession  = Scenarios.Session().Generate();
        UdderQuarter   anyQuarter  = Any.Enum<UdderQuarter>().Generate();

        // Act
        anySession.PrepareTeat(anyQuarter, Brushing, endedAt: Brushing);

        // Assert
        Assert.Equal(QuarterState.Prepared, anySession.QuarterOf(anyQuarter).State);
        Assert.Equal(SessionPhase.Preparing, anySession.Phase);
    }

    [Fact]
    public void The_other_quarters_are_untouched_by_a_brushing() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();

        // Act
        anySession.PrepareTeat(UdderQuarter.LeftFront, Brushing, endedAt: Brushing);

        // Assert
        Assert.All(anySession.Quarters.Where(q => q.Quarter != UdderQuarter.LeftFront), q => Assert.Equal(QuarterState.Pending, q.State));
    }

    [Fact]
    public void Preparing_every_milkable_teat_brings_the_robot_to_the_attaching_phase() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();

        // Act
        Scenarios.PrepareAllTeats(anySession);

        // Assert
        Assert.Equal(SessionPhase.Attaching, anySession.Phase);
        Assert.All(anySession.Quarters, q => Assert.Equal(QuarterState.Prepared, q.State));
    }

    [Fact]
    public void A_cow_with_blind_quarters_is_ready_to_attach_once_her_milkable_teats_are_prepared() {
        // Arrange
        MilkingSession anySession = Scenarios.Session()
                                              .WithAnimal(a => a.WithBlindQuarters([UdderQuarter.LeftFront, UdderQuarter.LeftRear]))
                                              .Generate();

        // Act
        Scenarios.PrepareAllTeats(anySession);

        // Assert
        Assert.Equal(SessionPhase.Attaching, anySession.Phase);
        Assert.Equal(2, anySession.Quarters.Count);
    }

    [Fact]
    public void Teats_may_be_brushed_back_to_back() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();

        // Act: the second brushing starts at the very instant the first one ended.
        anySession.PrepareTeat(UdderQuarter.LeftFront, Brushing, endedAt: Brushing);
        anySession.PrepareTeat(UdderQuarter.RightFront, Brushing, endedAt: Brushing * 2);

        // Assert
        Assert.Equal(QuarterState.Prepared, anySession.QuarterOf(UdderQuarter.RightFront).State);
    }

    [Fact]
    public void Teats_may_be_brushed_with_a_pause_between_them() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();

        // Act
        anySession.PrepareTeat(UdderQuarter.LeftFront, Brushing, endedAt: Brushing);
        anySession.PrepareTeat(UdderQuarter.RightFront, Brushing, endedAt: TimeSpan.FromSeconds(300));

        // Assert
        Assert.Equal(QuarterState.Prepared, anySession.QuarterOf(UdderQuarter.RightFront).State);
    }

    [Fact]
    public void A_brushing_longer_than_the_protocol_is_accepted() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();

        // Act
        anySession.PrepareTeat(UdderQuarter.LeftFront, TimeSpan.FromSeconds(25), endedAt: TimeSpan.FromSeconds(25));

        // Assert
        Assert.Equal(QuarterState.Prepared, anySession.QuarterOf(UdderQuarter.LeftFront).State);
    }

    [Fact]
    public void A_brushing_shorter_than_the_cleaning_time_is_refused_and_the_teat_stays_to_prepare() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => anySession.PrepareTeat(anyQuarter, Brushing - TimeSpan.FromTicks(1), endedAt: Brushing));

        Assert.Equal(QuarterState.Pending, anySession.QuarterOf(anyQuarter).State);
        Assert.Equal(SessionPhase.Identified, anySession.Phase);
    }

    [Fact]
    public void A_brushing_that_would_have_begun_before_the_session_is_refused() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();

        // Act / Assert: a 10 s brushing that ends at 9 s began at -1 s.
        Assert.Throws<MilkingRefusedException>(
            () => anySession.PrepareTeat(anyQuarter, Brushing, endedAt: TimeSpan.FromSeconds(9)));
    }

    [Fact]
    public void A_brushing_overlapping_the_previous_one_is_refused() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        anySession.PrepareTeat(UdderQuarter.LeftFront, Brushing, endedAt: Brushing);

        // Act / Assert: starts at 9.9999999 s, one tick before the first brushing ended.
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(
            () => anySession.PrepareTeat(UdderQuarter.RightFront, Brushing, endedAt: Brushing * 2 - TimeSpan.FromTicks(1)));

        Assert.Contains("overlaps", refusal.Message);
        Assert.Equal(QuarterState.Pending, anySession.QuarterOf(UdderQuarter.RightFront).State);
    }

    [Fact]
    public void A_teat_is_brushed_only_once() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();
        anySession.PrepareTeat(anyQuarter, Brushing, endedAt: Brushing);

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => anySession.PrepareTeat(anyQuarter, Brushing, endedAt: Brushing * 2));
    }

    [Fact]
    public void A_refused_second_brushing_does_not_move_the_end_of_the_preparation() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        anySession.PrepareTeat(UdderQuarter.LeftFront, Brushing, endedAt: Brushing);
        Assert.Throws<MilkingRefusedException>(() => anySession.PrepareTeat(UdderQuarter.LeftFront, Brushing, endedAt: TimeSpan.FromSeconds(500)));

        // Act: the next teat may still follow the first brushing directly.
        anySession.PrepareTeat(UdderQuarter.RightFront, Brushing, endedAt: Brushing * 2);

        // Assert
        Assert.Equal(QuarterState.Prepared, anySession.QuarterOf(UdderQuarter.RightFront).State);
    }

    [Fact]
    public void A_blind_quarter_is_never_brushed() {
        // Arrange
        MilkingSession anySession = Scenarios.Session()
                                              .WithAnimal(a => a.WithBlindQuarters([UdderQuarter.RightRear]))
                                              .Generate();

        // Act / Assert
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(
            () => anySession.PrepareTeat(UdderQuarter.RightRear, Brushing, endedAt: Brushing));

        Assert.Contains("blind", refusal.Message);
    }

    [Fact]
    public void No_teat_is_brushed_once_the_session_has_been_released() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        anySession.ReportAnomaly(new MilkingAnomaly(AnomalyKind.EmergencyStop, null, TimeSpan.FromSeconds(1)));
        anySession.Release(anySession.StartedAt);

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(() => anySession.PrepareTeat(UdderQuarter.LeftFront, Brushing, endedAt: Brushing));
    }

}
