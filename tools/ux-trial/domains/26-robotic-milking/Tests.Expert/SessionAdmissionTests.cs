namespace RoboticMilking.Tests.Expert;

public sealed class SessionAdmissionTests {

    private static readonly IAny<int> AnyBoxNumber = Any.Int32().Between(1, MilkingSession.BoxCount);

    [Fact]
    public void A_cow_milked_less_than_the_minimum_interval_ago_is_sent_out_without_milking() {
        // Arrange
        DateTimeOffset  anyStart    = Recipes.AnySessionStart().Generate();
        Animal          anyCow      = Any.Animal().WithLastMilkedAt(anyStart.AddHours(-7)).Generate();
        MilkingSettings anySettings = Any.MilkingSettings().WithMinimumInterval(TimeSpan.FromHours(8)).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new MilkingSession(anyCow, AnyBoxNumber.Generate(), anyStart, anySettings));

        // Assert
        Assert.IsType<MilkingRefusedException>(refusal);
    }

    [Fact]
    public void A_cow_whose_minimum_interval_has_elapsed_is_admitted() {
        // Arrange
        DateTimeOffset  anyStart    = Recipes.AnySessionStart().Generate();
        Animal          anyCow      = Any.Animal().WithLastMilkedAt(anyStart.AddHours(-8)).Generate();
        MilkingSettings anySettings = Any.MilkingSettings().WithMinimumInterval(TimeSpan.FromHours(8)).Generate();

        // Act
        MilkingSession session = new MilkingSession(anyCow, AnyBoxNumber.Generate(), anyStart, anySettings);

        // Assert
        Assert.Equal(SessionPhase.Identified, session.Phase);
    }

    [Fact]
    public void A_cow_never_milked_by_the_robot_is_admitted() {
        // Arrange
        AnyMilkingSession anyFirstVisit = Any.MilkingSession().WithAnimal(a => a.WithLastMilkedAt((DateTimeOffset?)null));

        // Act
        MilkingSession session = anyFirstVisit.Generate();

        // Assert
        Assert.Equal(SessionPhase.Identified, session.Phase);
    }

    [Fact]
    public void A_last_milking_after_the_session_start_is_an_argument_error() {
        // Arrange
        DateTimeOffset    anyStart      = Recipes.AnySessionStart().Generate();
        AnyMilkingSession anyImpossible = Any.MilkingSession()
                                             .WithStartedAt(anyStart)
                                             .WithAnimal(a => a.WithLastMilkedAt(anyStart.AddMinutes(1)));

        // Act
        Exception? refusal = Record.Exception(() => anyImpossible.Generate());

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("startedAt", argumentError.ParamName);
    }

    [Fact]
    public void A_session_follows_only_the_milkable_quarters() {
        // Arrange
        AnyMilkingSession anySessionOfThreeTeatedCow = Any.MilkingSession()
                                                          .WithAnimal(a => a.WithBlindQuarters([UdderQuarter.RightRear]));

        // Act
        MilkingSession session = anySessionOfThreeTeatedCow.Generate();

        // Assert
        Assert.Equal([UdderQuarter.LeftFront, UdderQuarter.RightFront, UdderQuarter.LeftRear],
                     session.Quarters.Select(q => q.Quarter).Order());
        Assert.All(session.Quarters, q => Assert.Equal(QuarterState.Pending, q.State));
        Assert.Throws<MilkingRefusedException>(() => session.QuarterOf(UdderQuarter.RightRear));
    }

}
