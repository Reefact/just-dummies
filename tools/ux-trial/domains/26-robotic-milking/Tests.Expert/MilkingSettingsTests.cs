using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Expert;

public sealed class MilkingSettingsTests {

    private static readonly TimeSpan OneTick = TimeSpan.FromTicks(1);

    /// <summary>
    ///     Valid settings with the named parameters replaced: what a test is about is spelled out, and the other
    ///     parameters are whatever a valid draw gave them.
    /// </summary>
    private static MilkingSettings Settings(TimeSpan? cleaningTime         = null,
                                            TimeSpan? letDownDelay         = null,
                                            decimal?  vacuumKpa            = null,
                                            decimal?  detachFlowThreshold  = null,
                                            int?      maxAttachAttempts    = null,
                                            TimeSpan? minimumInterval      = null) {
        MilkingSettings anyValid = Any.MilkingSettings().Generate();

        return new MilkingSettings(cleaningTime        ?? anyValid.CleaningTime,
                                   letDownDelay        ?? anyValid.LetDownDelay,
                                   vacuumKpa           ?? anyValid.VacuumKpa,
                                   detachFlowThreshold ?? anyValid.DetachFlowThreshold,
                                   maxAttachAttempts   ?? anyValid.MaxAttachAttempts,
                                   minimumInterval     ?? anyValid.MinimumInterval);
    }

    public static TheoryData<TimeSpan?, TimeSpan?, decimal?, decimal?, int?, TimeSpan?> BoundariesOfEachParameter() {
        return new TheoryData<TimeSpan?, TimeSpan?, decimal?, decimal?, int?, TimeSpan?> {
            { TimeSpan.FromSeconds(5),  TimeSpan.FromSeconds(10), null, null, null, null },
            { TimeSpan.FromSeconds(30), null,                      null, null, null, null },
            { TimeSpan.FromSeconds(30), TimeSpan.Zero,             null, null, null, null },
            { TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(90), null, null, null, null },
            { null,                      null,                      38m,  null, null, null },
            { null,                      null,                      50m,  null, null, null },
            { null,                      null,                      null, 0.1m, null, null },
            { null,                      null,                      null, 0.5m, null, null },
            { null,                      null,                      null, null, 1,    null },
            { null,                      null,                      null, null, 5,    null },
            { null,                      null,                      null, null, null, TimeSpan.FromHours(4) },
            { null,                      null,                      null, null, null, TimeSpan.FromHours(12) },
        };
    }

    [Theory]
    [MemberData(nameof(BoundariesOfEachParameter))]
    public void Every_documented_boundary_is_accepted_and_kept(TimeSpan? cleaningTime, TimeSpan? letDownDelay, decimal? vacuumKpa, decimal? detachFlowThreshold, int? maxAttachAttempts, TimeSpan? minimumInterval) {
        // Act
        MilkingSettings settings = Settings(cleaningTime, letDownDelay, vacuumKpa, detachFlowThreshold, maxAttachAttempts, minimumInterval);

        // Assert
        Assert.Equal(cleaningTime        ?? settings.CleaningTime,        settings.CleaningTime);
        Assert.Equal(letDownDelay        ?? settings.LetDownDelay,        settings.LetDownDelay);
        Assert.Equal(vacuumKpa           ?? settings.VacuumKpa,           settings.VacuumKpa);
        Assert.Equal(detachFlowThreshold ?? settings.DetachFlowThreshold, settings.DetachFlowThreshold);
        Assert.Equal(maxAttachAttempts   ?? settings.MaxAttachAttempts,   settings.MaxAttachAttempts);
        Assert.Equal(minimumInterval     ?? settings.MinimumInterval,     settings.MinimumInterval);
    }

    [Theory]
    [InlineData("cleaningTime", 4.9999999)]
    [InlineData("cleaningTime", 30.0000001)]
    [InlineData("letDownDelay", -0.0000001)]
    [InlineData("letDownDelay", 90.0000001)]
    public void A_duration_just_outside_its_range_in_seconds_is_refused(string parameter, double seconds) {
        // Arrange
        TimeSpan outside = TimeSpan.FromTicks((long)Math.Round(seconds * TimeSpan.TicksPerSecond));
        // A cleaning of 30 s alone stimulates enough, so the cross-field rule never answers in place of the range.
        TimeSpan? cleaningTime = parameter == "cleaningTime" ? outside : TimeSpan.FromSeconds(30);
        TimeSpan? letDownDelay = parameter == "letDownDelay" ? outside : TimeSpan.FromSeconds(30);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Settings(cleaningTime, letDownDelay));

        // Assert
        Assert.Equal(parameter, refusal.ParamName);
    }

    [Theory]
    [InlineData(37.99)]
    [InlineData(50.01)]
    public void A_vacuum_outside_38_to_50_kPa_is_refused(double kpa) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Settings(vacuumKpa: (decimal)kpa));

        // Assert
        Assert.Equal("vacuumKpa", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.09)]
    [InlineData(0.51)]
    public void A_detach_threshold_outside_a_tenth_to_half_a_kilo_a_minute_is_refused(double threshold) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Settings(detachFlowThreshold: (decimal)threshold));

        // Assert
        Assert.Equal("detachFlowThreshold", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void An_attempt_allowance_outside_one_to_five_is_refused(int attempts) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Settings(maxAttachAttempts: attempts));

        // Assert
        Assert.Equal("maxAttachAttempts", refusal.ParamName);
    }

    [Fact]
    public void An_interval_just_under_four_hours_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Settings(minimumInterval: TimeSpan.FromHours(4) - OneTick));

        // Assert
        Assert.Equal("minimumInterval", refusal.ParamName);
    }

    [Fact]
    public void An_interval_just_over_twelve_hours_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Settings(minimumInterval: TimeSpan.FromHours(12) + OneTick));

        // Assert
        Assert.Equal("minimumInterval", refusal.ParamName);
    }

    [Theory]
    [InlineData(5, 10)]    // 4 x 5 s + 10 s: exactly the 30 s
    [InlineData(7.5, 0)]   // 4 x 7.5 s: exactly the 30 s, no delay needed
    [InlineData(6, 6)]
    [InlineData(30, 0)]
    public void Four_cleanings_and_the_delay_that_stimulate_at_least_thirty_seconds_are_accepted(double cleaningSeconds, double delaySeconds) {
        // Act
        MilkingSettings settings = Settings(cleaningTime: TimeSpan.FromSeconds(cleaningSeconds), letDownDelay: TimeSpan.FromSeconds(delaySeconds));

        // Assert
        Assert.True(settings.CleaningTime * 4 + settings.LetDownDelay >= MilkingSettings.MinimumLetDownStimulation);
    }

    [Theory]
    [InlineData(5, 9.9999999)]
    [InlineData(7.4999999, 0)]
    [InlineData(5, 0)]
    [InlineData(6, 5.9999999)]
    public void A_stimulation_even_one_tick_short_of_thirty_seconds_is_refused(double cleaningSeconds, double delaySeconds) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Settings(cleaningTime: TimeSpan.FromTicks((long)Math.Round(cleaningSeconds * TimeSpan.TicksPerSecond)),
                           letDownDelay: TimeSpan.FromTicks((long)Math.Round(delaySeconds * TimeSpan.TicksPerSecond))));

        // Assert
        Assert.Equal("letDownDelay", refusal.ParamName);
    }

    [Fact]
    public void The_minimum_stimulation_is_thirty_seconds() {
        // Assert
        Assert.Equal(TimeSpan.FromSeconds(30), MilkingSettings.MinimumLetDownStimulation);
    }

}
