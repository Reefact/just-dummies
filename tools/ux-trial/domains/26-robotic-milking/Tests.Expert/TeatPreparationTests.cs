using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Expert;

public sealed class TeatPreparationTests {

    private static readonly TimeSpan OneTick = TimeSpan.FromTicks(1);

    [Fact]
    public void Brushing_one_teat_of_a_full_udder_prepares_that_quarter_and_starts_the_preparation() {
        // Arrange
        MilkingSession session  = Milking.SessionOfAFullUdder().Generate();
        TimeSpan       cleaning = session.Settings.CleaningTime;

        // Act
        session.PrepareTeat(UdderQuarter.LeftRear, cleaning, cleaning);

        // Assert
        Assert.Equal(QuarterState.Prepared, session.QuarterOf(UdderQuarter.LeftRear).State);
        Assert.Equal(3, session.Quarters.Count(quarter => quarter.State == QuarterState.Pending));
        Assert.Equal(SessionPhase.Preparing, session.Phase);
    }

    [Fact]
    public void Brushing_the_only_milkable_teat_completes_the_preparation() {
        // Arrange
        UdderQuarter   anyMilkableQuarter = Any.Enum<UdderQuarter>().Generate();
        MilkingSession session            = Any.MilkingSession()
                                               .WithAnimal(cow => cow.WithBlindQuarters(Enum.GetValues<UdderQuarter>().Where(quarter => quarter != anyMilkableQuarter)))
                                               .Generate();
        TimeSpan       cleaning           = session.Settings.CleaningTime;

        // Act
        session.PrepareTeat(anyMilkableQuarter, cleaning, cleaning);

        // Assert
        Assert.Equal(SessionPhase.Attaching, session.Phase);
    }

    [Fact]
    public void Brushing_every_milkable_teat_in_turn_leaves_the_robot_ready_to_attach() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();

        // Act
        Milking.PrepareEveryTeat(anySession);

        // Assert
        Assert.All(anySession.Quarters, quarter => Assert.Equal(QuarterState.Prepared, quarter.State));
        Assert.Equal(SessionPhase.Attaching, anySession.Phase);
    }

    [Fact]
    public void A_teat_brushed_for_exactly_the_cleaning_time_is_prepared() {
        // Arrange
        MilkingSession session  = Any.MilkingSession().Generate();
        UdderQuarter   quarter  = Milking.MilkableQuartersOf(session)[0];
        TimeSpan       cleaning = session.Settings.CleaningTime;

        // Act
        session.PrepareTeat(quarter, cleaning, cleaning);

        // Assert
        Assert.Equal(QuarterState.Prepared, session.QuarterOf(quarter).State);
    }

    [Fact]
    public void A_teat_brushed_longer_than_the_cleaning_time_is_prepared() {
        // Arrange
        MilkingSession session  = Any.MilkingSession().Generate();
        UdderQuarter   quarter  = Milking.MilkableQuartersOf(session)[0];
        TimeSpan       brushing = session.Settings.CleaningTime + TimeSpan.FromSeconds(5);

        // Act
        session.PrepareTeat(quarter, brushing, brushing);

        // Assert
        Assert.Equal(QuarterState.Prepared, session.QuarterOf(quarter).State);
    }

    [Fact]
    public void A_teat_brushed_one_tick_less_than_the_cleaning_time_is_refused_and_stays_unprepared() {
        // Arrange
        MilkingSession session  = Any.MilkingSession().Generate();
        UdderQuarter   quarter  = Milking.MilkableQuartersOf(session)[0];
        TimeSpan       brushing = session.Settings.CleaningTime - OneTick;

        // Act
        Assert.Throws<MilkingRefusedException>(() => session.PrepareTeat(quarter, brushing, session.Settings.CleaningTime));

        // Assert
        Assert.Equal(QuarterState.Pending, session.QuarterOf(quarter).State);
        Assert.Equal(SessionPhase.Identified, session.Phase);
    }

    [Fact]
    public void A_second_brushing_that_begins_before_the_first_one_ended_is_refused_and_leaves_its_teat_unprepared() {
        // Arrange
        MilkingSession session  = Milking.SessionOfAFullUdder().Generate();
        TimeSpan       cleaning = session.Settings.CleaningTime;
        session.PrepareTeat(UdderQuarter.LeftFront, cleaning, cleaning);

        // Act
        Assert.Throws<MilkingRefusedException>(() => session.PrepareTeat(UdderQuarter.RightFront, cleaning, cleaning + cleaning - OneTick));

        // Assert
        Assert.Equal(QuarterState.Pending, session.QuarterOf(UdderQuarter.RightFront).State);
    }

    [Fact]
    public void A_second_brushing_that_begins_the_instant_the_first_one_ended_is_prepared() {
        // Arrange
        MilkingSession session  = Milking.SessionOfAFullUdder().Generate();
        TimeSpan       cleaning = session.Settings.CleaningTime;
        session.PrepareTeat(UdderQuarter.LeftFront, cleaning, cleaning);

        // Act
        session.PrepareTeat(UdderQuarter.RightFront, cleaning, cleaning + cleaning);

        // Assert
        Assert.Equal(QuarterState.Prepared, session.QuarterOf(UdderQuarter.RightFront).State);
    }

    [Fact]
    public void A_second_brushing_after_the_arm_has_waited_is_prepared() {
        // Arrange
        MilkingSession session  = Milking.SessionOfAFullUdder().Generate();
        TimeSpan       cleaning = session.Settings.CleaningTime;
        session.PrepareTeat(UdderQuarter.LeftFront, cleaning, cleaning);

        // Act
        session.PrepareTeat(UdderQuarter.RightFront, cleaning, cleaning + cleaning + TimeSpan.FromSeconds(12));

        // Assert
        Assert.Equal(QuarterState.Prepared, session.QuarterOf(UdderQuarter.RightFront).State);
    }

    [Fact]
    public void A_first_brushing_cannot_have_begun_before_the_session_started() {
        // Arrange
        MilkingSession session  = Any.MilkingSession().Generate();
        UdderQuarter   quarter  = Milking.MilkableQuartersOf(session)[0];
        TimeSpan       cleaning = session.Settings.CleaningTime;

        // Act & Assert
        Assert.Throws<MilkingRefusedException>(() => session.PrepareTeat(quarter, cleaning, cleaning - OneTick));
    }

    [Fact]
    public void A_teat_is_prepared_once() {
        // Arrange
        MilkingSession session  = Milking.SessionOfAFullUdder().Generate();
        TimeSpan       cleaning = session.Settings.CleaningTime;
        session.PrepareTeat(UdderQuarter.RightFront, cleaning, cleaning);

        // Act & Assert
        Assert.Throws<MilkingRefusedException>(() => session.PrepareTeat(UdderQuarter.RightFront, cleaning, cleaning + cleaning));
    }

    [Fact]
    public void A_blind_quarter_is_never_prepared() {
        // Arrange
        MilkingSession session      = Any.MilkingSession().WithAnimal(cow => cow.WithBlindQuarters(blind => blind.NonEmpty())).Generate();
        UdderQuarter   blindQuarter = session.Animal.BlindQuarters.First();
        TimeSpan       cleaning     = session.Settings.CleaningTime;

        // Act
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(() => session.PrepareTeat(blindQuarter, cleaning, cleaning));

        // Assert
        Assert.Contains(blindQuarter.ToString(), refusal.Message);
    }

    [Fact]
    public void A_blind_quarter_has_no_milking_to_look_at() {
        // Arrange
        MilkingSession session      = Any.MilkingSession().WithAnimal(cow => cow.WithBlindQuarters(blind => blind.NonEmpty())).Generate();
        UdderQuarter   blindQuarter = session.Animal.BlindQuarters.First();

        // Act & Assert
        Assert.Throws<MilkingRefusedException>(() => session.QuarterOf(blindQuarter));
    }

}
