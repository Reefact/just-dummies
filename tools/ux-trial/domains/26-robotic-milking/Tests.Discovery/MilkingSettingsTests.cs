using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Discovery;

public sealed class MilkingSettingsTests {

    public static TheoryData<decimal> VacuumInRange => new() { 38m, 38.01m, 44m, 49.99m, 50m };
    public static TheoryData<decimal> VacuumOutOfRange => new() { 0m, 37.99m, 50.01m, 1000m };
    public static TheoryData<decimal> ThresholdInRange => new() { 0.1m, 0.11m, 0.3m, 0.49m, 0.5m };
    public static TheoryData<decimal> ThresholdOutOfRange => new() { 0m, 0.09m, 0.51m, 5m };

    [Fact]
    public void Settings_keep_the_values_they_were_built_with() {
        // Act
        MilkingSettings settings = new MilkingSettings(cleaningTime: TimeSpan.FromSeconds(12),
                                                       letDownDelay: TimeSpan.FromSeconds(40),
                                                       vacuumKpa: 42.5m,
                                                       detachFlowThreshold: 0.25m,
                                                       maxAttachAttempts: 3,
                                                       minimumInterval: TimeSpan.FromHours(6));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(12), settings.CleaningTime);
        Assert.Equal(TimeSpan.FromSeconds(40), settings.LetDownDelay);
        Assert.Equal(42.5m, settings.VacuumKpa);
        Assert.Equal(0.25m, settings.DetachFlowThreshold);
        Assert.Equal(3, settings.MaxAttachAttempts);
        Assert.Equal(TimeSpan.FromHours(6), settings.MinimumInterval);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(5.001)]
    [InlineData(17)]
    [InlineData(30)]
    public void A_cleaning_time_from_five_to_thirty_seconds_is_accepted(double seconds) {
        // Act
        MilkingSettings anySettings = Any.MilkingSettings().WithCleaningTime(TimeSpan.FromSeconds(seconds)).Generate();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(seconds), anySettings.CleaningTime);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4.999)]
    [InlineData(30.001)]
    [InlineData(31)]
    [InlineData(-10)]
    public void A_cleaning_time_outside_five_to_thirty_seconds_is_refused(double seconds) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.MilkingSettings().WithCleaningTime(TimeSpan.FromSeconds(seconds)).Generate());

        Assert.Equal("cleaningTime", refusal.ParamName);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(45)]
    [InlineData(89.999)]
    [InlineData(90)]
    public void A_let_down_delay_up_to_ninety_seconds_is_accepted(double seconds) {
        MilkingSettings anySettings = Any.MilkingSettings().WithLetDownDelay(TimeSpan.FromSeconds(seconds)).Generate();

        Assert.Equal(TimeSpan.FromSeconds(seconds), anySettings.LetDownDelay);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(-30)]
    [InlineData(90.001)]
    [InlineData(300)]
    public void A_let_down_delay_below_zero_or_above_ninety_seconds_is_refused(double seconds) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.MilkingSettings().WithLetDownDelay(TimeSpan.FromSeconds(seconds)).Generate());

        Assert.Equal("letDownDelay", refusal.ParamName);
    }

    [Theory]
    [InlineData(5, 10)]      // 4 x 5 + 10 = 30 exactly
    [InlineData(5, 90)]
    [InlineData(7.5, 0)]     // 4 x 7.5 = 30 exactly, no delay needed
    [InlineData(10, 0)]
    [InlineData(30, 0)]
    public void Four_brushings_plus_the_delay_reaching_thirty_seconds_stimulate_enough(double cleaningSeconds, double delaySeconds) {
        MilkingSettings anySettings = Any.MilkingSettings()
                                         .WithCleaningTime(TimeSpan.FromSeconds(cleaningSeconds))
                                         .WithLetDownDelay(TimeSpan.FromSeconds(delaySeconds))
                                         .Generate();

        Assert.Equal(TimeSpan.FromSeconds(cleaningSeconds), anySettings.CleaningTime);
        Assert.Equal(TimeSpan.FromSeconds(delaySeconds), anySettings.LetDownDelay);
    }

    [Theory]
    [InlineData(5, 9.999)]   // 29.999 s
    [InlineData(5, 0)]
    [InlineData(7.4, 0)]     // 29.6 s
    [InlineData(7, 1)]       // 29 s
    public void Four_brushings_plus_the_delay_under_thirty_seconds_are_refused_as_too_short_for_let_down(double cleaningSeconds, double delaySeconds) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.MilkingSettings()
                     .WithCleaningTime(TimeSpan.FromSeconds(cleaningSeconds))
                     .WithLetDownDelay(TimeSpan.FromSeconds(delaySeconds))
                     .Generate());

        Assert.IsNotType<ArgumentOutOfRangeException>(refusal);
        Assert.Equal("letDownDelay", refusal.ParamName);
    }

    [Theory]
    [MemberData(nameof(VacuumInRange))]
    public void A_vacuum_from_38_to_50_kpa_is_accepted(decimal vacuum) {
        MilkingSettings anySettings = Any.MilkingSettings().WithVacuumKpa(vacuum).Generate();

        Assert.Equal(vacuum, anySettings.VacuumKpa);
    }

    [Theory]
    [MemberData(nameof(VacuumOutOfRange))]
    public void A_vacuum_outside_38_to_50_kpa_is_refused(decimal vacuum) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.MilkingSettings().WithVacuumKpa(vacuum).Generate());

        Assert.Equal("vacuumKpa", refusal.ParamName);
    }

    [Theory]
    [MemberData(nameof(ThresholdInRange))]
    public void A_detach_threshold_from_point_one_to_point_five_is_accepted(decimal threshold) {
        MilkingSettings anySettings = Any.MilkingSettings().WithDetachFlowThreshold(threshold).Generate();

        Assert.Equal(threshold, anySettings.DetachFlowThreshold);
    }

    [Theory]
    [MemberData(nameof(ThresholdOutOfRange))]
    public void A_detach_threshold_outside_point_one_to_point_five_is_refused(decimal threshold) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.MilkingSettings().WithDetachFlowThreshold(threshold).Generate());

        Assert.Equal("detachFlowThreshold", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void One_to_five_attachment_attempts_are_accepted(int attempts) {
        MilkingSettings anySettings = Any.MilkingSettings().WithMaxAttachAttempts(attempts).Generate();

        Assert.Equal(attempts, anySettings.MaxAttachAttempts);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(int.MaxValue)]
    public void Zero_or_more_than_five_attachment_attempts_are_refused(int attempts) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.MilkingSettings().WithMaxAttachAttempts(attempts).Generate());

        Assert.Equal("maxAttachAttempts", refusal.ParamName);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(4.0001)]
    [InlineData(8)]
    [InlineData(12)]
    public void A_minimum_interval_from_four_to_twelve_hours_is_accepted(double hours) {
        MilkingSettings anySettings = Any.MilkingSettings().WithMinimumInterval(TimeSpan.FromHours(hours)).Generate();

        Assert.Equal(TimeSpan.FromHours(hours), anySettings.MinimumInterval);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3.9999)]
    [InlineData(12.0001)]
    [InlineData(24)]
    public void A_minimum_interval_outside_four_to_twelve_hours_is_refused(double hours) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.MilkingSettings().WithMinimumInterval(TimeSpan.FromHours(hours)).Generate());

        Assert.Equal("minimumInterval", refusal.ParamName);
    }

}
