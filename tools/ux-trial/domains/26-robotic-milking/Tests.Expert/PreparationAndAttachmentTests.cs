namespace RoboticMilking.Tests.Expert;

public sealed class PreparationAndAttachmentTests {

    [Fact]
    public void Brushing_a_first_teat_starts_the_preparation() {
        // Arrange
        MilkingSession anySession  = Any.MilkingSession().Generate();
        UdderQuarter   anyQuarter  = anySession.AnyMilkableQuarter();
        TimeSpan       anyBrushing = Recipes.CompliantBrushing(anySession.Settings).Generate();

        // Act
        anySession.PrepareTeat(anyQuarter, anyBrushing, anyBrushing);

        // Assert
        Assert.Equal(QuarterState.Prepared, anySession.QuarterOf(anyQuarter).State);
        Assert.Equal(SessionPhase.Preparing, anySession.Phase);
    }

    [Fact]
    public void A_teat_brushed_for_less_than_the_cleaning_time_is_refused() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().WithSettings(s => s.WithCleaningTime(TimeSpan.FromSeconds(12))).Generate();
        UdderQuarter   anyQuarter = anySession.AnyMilkableQuarter();

        // Act
        Exception? refusal = Record.Exception(() => anySession.PrepareTeat(anyQuarter, TimeSpan.FromSeconds(11), TimeSpan.FromSeconds(11)));

        // Assert
        Assert.IsType<MilkingRefusedException>(refusal);
        Assert.Equal(QuarterState.Pending, anySession.QuarterOf(anyQuarter).State);
    }

    [Fact]
    public void A_blind_teat_is_never_brushed() {
        // Arrange
        MilkingSession anySession  = Any.MilkingSession().WithAnimal(a => a.WithBlindQuarters([UdderQuarter.LeftFront])).Generate();
        TimeSpan       anyBrushing = Recipes.CompliantBrushing(anySession.Settings).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anySession.PrepareTeat(UdderQuarter.LeftFront, anyBrushing, anyBrushing));

        // Assert
        Assert.IsType<MilkingRefusedException>(refusal);
    }

    [Fact]
    public void Two_teats_cannot_be_brushed_at_the_same_time() {
        // Arrange: a cow with two milkable quarters at least, so there are two teats to brush.
        MilkingSession anySession = Any.MilkingSession()
                                       .WithAnimal(a => a.WithBlindQuarters(b => b.WithMaxCount(2)))
                                       .WithSettings(s => s.WithCleaningTime(TimeSpan.FromSeconds(10)))
                                       .Generate();
        List<UdderQuarter> anyTwoTeats = Any.ListOf(Any.ElementOf(anySession.Animal.MilkableQuarters)).WithCount(2).Distinct().Generate();
        anySession.PrepareTeat(anyTwoTeats[0], TimeSpan.FromSeconds(10), endedAt: TimeSpan.FromSeconds(20));

        // Act: the second brushing starts at 15 s, before the first one ended.
        Exception? refusal = Record.Exception(() => anySession.PrepareTeat(anyTwoTeats[1], TimeSpan.FromSeconds(10), endedAt: TimeSpan.FromSeconds(25)));

        // Assert
        Assert.IsType<MilkingRefusedException>(refusal);
    }

    [Fact]
    public void A_teat_is_brushed_only_once() {
        // Arrange
        MilkingSession anySession  = Any.MilkingSession().Generate();
        UdderQuarter   anyQuarter  = anySession.AnyMilkableQuarter();
        TimeSpan       anyBrushing = Recipes.CompliantBrushing(anySession.Settings).Generate();
        anySession.PrepareTeat(anyQuarter, anyBrushing, anyBrushing);

        // Act
        Exception? refusal = Record.Exception(() => anySession.PrepareTeat(anyQuarter, anyBrushing, anyBrushing + anyBrushing));

        // Assert
        Assert.IsType<MilkingRefusedException>(refusal);
    }

    [Fact]
    public void No_cup_is_attached_before_every_milkable_teat_is_prepared() {
        // Arrange: two teats at least, one of them still dirty.
        MilkingSession anySession = Any.MilkingSession().WithAnimal(a => a.WithBlindQuarters(b => b.WithMaxCount(2))).Generate();
        List<UdderQuarter> anyTwoTeats = Any.ListOf(Any.ElementOf(anySession.Animal.MilkableQuarters)).WithCount(2).Distinct().Generate();
        TimeSpan anyBrushing = Recipes.CompliantBrushing(anySession.Settings).Generate();
        anySession.PrepareTeat(anyTwoTeats[0], anyBrushing, anyBrushing);
        TimeSpan anyLaterOn = anyBrushing + anySession.Settings.LetDownDelay + Recipes.ShortWait().Generate();

        // Act
        Exception? refusal = Record.Exception(() => anySession.AttachCup(anyTwoTeats[0], succeeded: true, anyLaterOn));

        // Assert
        Assert.IsType<MilkingRefusedException>(refusal);
    }

    [Fact]
    public void No_cup_is_attached_before_milk_let_down() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().WithSettings(s => s.WithLetDownDelay(TimeSpan.FromSeconds(40))).Generate();
        TimeSpan       preparedAt = anySession.PrepareEveryTeat();

        // Act: 39 s after the last brushing, one second too early.
        Exception? refusal = Record.Exception(
            () => anySession.AttachCup(anySession.AnyMilkableQuarter(), succeeded: true, preparedAt + TimeSpan.FromSeconds(39)));

        // Assert
        Assert.IsType<MilkingRefusedException>(refusal);
    }

    [Fact]
    public void Once_every_teat_is_prepared_the_robot_attaches_cups() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();

        // Act
        anySession.PrepareEveryTeat();

        // Assert
        Assert.Equal(SessionPhase.Attaching, anySession.Phase);
    }

    [Fact]
    public void A_failed_attempt_with_attempts_left_lets_the_robot_try_again() {
        // Arrange
        MilkingSession anySession  = Any.MilkingSession().WithSettings(s => s.WithMaxAttachAttempts(3)).Generate();
        TimeSpan       attachFrom  = anySession.PrepareEveryTeat() + anySession.Settings.LetDownDelay;
        UdderQuarter   anyQuarter  = anySession.AnyMilkableQuarter();
        anySession.AttachCup(anyQuarter, succeeded: false, attachFrom);

        // Act
        anySession.AttachCup(anyQuarter, succeeded: true, attachFrom + TimeSpan.FromSeconds(5));

        // Assert
        QuarterMilking quarter = anySession.QuarterOf(anyQuarter);
        Assert.Equal(QuarterState.Attached, quarter.State);
        Assert.Equal(2, quarter.AttachAttempts);
    }

    [Fact]
    public void The_last_allowed_failed_attempt_gives_the_quarter_up() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().WithSettings(s => s.WithMaxAttachAttempts(2)).Generate();
        TimeSpan       attachFrom = anySession.PrepareEveryTeat() + anySession.Settings.LetDownDelay;
        UdderQuarter   anyQuarter = anySession.AnyMilkableQuarter();
        anySession.AttachCup(anyQuarter, succeeded: false, attachFrom);

        // Act
        anySession.AttachCup(anyQuarter, succeeded: false, attachFrom + TimeSpan.FromSeconds(5));

        // Assert
        Assert.Equal(QuarterState.AttachmentFailed, anySession.QuarterOf(anyQuarter).State);
        Assert.True(anySession.IsIncomplete);
    }

}
