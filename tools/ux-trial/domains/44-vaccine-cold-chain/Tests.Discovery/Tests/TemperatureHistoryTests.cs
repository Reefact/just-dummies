using AgriTech.Domain;

using ColdChain.Domain;

using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace ColdChain.Tests;

public sealed class TemperatureHistoryTests {

    private static readonly DateTimeOffset Origin = new DateTimeOffset(2026, 3, 2, 6, 0, 0, TimeSpan.Zero);

    private readonly TemperatureRange _label = new TemperatureRange(2m, 8m);

    private DataLogger? _drawnLogger;

    // Drawn on first use, inside the test body: a field initializer would run before the seed scope opens.
    private DataLogger _logger => _drawnLogger ??= new AnyDataLogger().WithRecordingIntervalMinutes(10).Generate();

    private TemperatureHistory NewHistory() {
        return new TemperatureHistory(_logger, _label);
    }

    private LoggerReading ReadingAt(int minute, decimal celsius) {
        return new AnyLoggerReading().WithLogger(_logger.Id).WithTakenAt(Origin.AddMinutes(minute)).WithCelsius(celsius).Generate();
    }

    private TemperatureHistory HistoryOf(params (int Minute, decimal Celsius)[] samples) {
        TemperatureHistory history = NewHistory();
        foreach ((int minute, decimal celsius) in samples) {
            history.Record(ReadingAt(minute, celsius));
        }

        return history;
    }

    [Fact]
    public void A_new_history_holds_no_reading() {
        // Act
        TemperatureHistory history = NewHistory();

        // Assert
        Assert.Empty(history.Readings);
        Assert.Null(history.Last);
        Assert.Empty(history.Excursions);
        Assert.Equal(0, history.MinutesOutsideRange);
        Assert.Equal(0, history.LongestExcursionMinutes);
        Assert.False(history.HasHardLimitBreach);
        Assert.Same(_logger, history.Logger);
        Assert.Same(_label, history.LabelledRange);
    }

    [Fact]
    public void A_logger_and_a_label_are_required() {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new TemperatureHistory(null!, _label));
        Assert.Throws<ArgumentNullException>(() => new TemperatureHistory(_logger, null!));
    }

    [Fact]
    public void Readings_are_kept_in_the_order_they_were_recorded() {
        // Act
        TemperatureHistory history = HistoryOf((0, 5m), (10, 5.5m), (20, 6m));

        // Assert
        Assert.Equal(3, history.Readings.Count);
        Assert.Equal(Origin.AddMinutes(20), history.Last!.TakenAt);
        Assert.Equal(6m, history.Last.Celsius);
    }

    [Fact]
    public void A_reading_from_another_logger_is_refused() {
        // Arrange
        TemperatureHistory history = NewHistory();
        SensorId           anotherId = new AnySensorId().DifferentFrom(_logger.Id).Generate();
        LoggerReading      foreign   = new AnyLoggerReading().WithLogger(anotherId).WithTakenAt(Origin).Generate();

        // Act
        Exception? thrown = Record.Exception(() => history.Record(foreign));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("reading", refusal.ParamName);
        Assert.Empty(history.Readings);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(5)]
    [InlineData(-30)]
    public void A_reading_not_strictly_after_the_last_one_is_refused(int minute) {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 5m), (10, 5m));
        LoggerReading      late    = ReadingAt(minute, 5m);

        // Act
        Exception? thrown = Record.Exception(() => history.Record(late));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal(2, history.Readings.Count);
    }

    [Fact]
    public void A_missing_reading_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => NewHistory().Record(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    [Fact]
    public void Readings_within_the_label_make_no_excursion() {
        // Act
        TemperatureHistory history = HistoryOf((0, 2m), (10, 5m), (20, 8m), (30, 2m));

        // Assert
        Assert.Empty(history.Excursions);
        Assert.Equal(0, history.MinutesOutsideRange);
    }

    [Fact]
    public void An_excursion_runs_from_its_first_reading_outside_to_the_first_reading_back_inside() {
        // Act
        TemperatureHistory history = HistoryOf((0, 5m), (10, 8.1m), (20, 9m), (30, 8.5m), (40, 7m), (50, 5m));

        // Assert
        Excursion excursion = Assert.Single(history.Excursions);
        Assert.Equal(ExcursionKind.Warm, excursion.Kind);
        Assert.Equal(Origin.AddMinutes(10), excursion.StartedAt);
        Assert.Equal(Origin.AddMinutes(40), excursion.EndedAt);
        Assert.Equal(9m, excursion.ExtremeCelsius);
        Assert.False(excursion.IsOpen);
        Assert.Equal(30, history.MinutesOutsideRange);
        Assert.Equal(30, history.LongestExcursionMinutes);
    }

    [Fact]
    public void A_cold_excursion_keeps_the_coldest_reading_as_its_extreme() {
        // Act
        TemperatureHistory history = HistoryOf((0, 5m), (10, 1.9m), (20, -1m), (30, 0.5m), (40, 4m));

        // Assert
        Excursion excursion = Assert.Single(history.Excursions);
        Assert.Equal(ExcursionKind.Cold, excursion.Kind);
        Assert.Equal(-1m, excursion.ExtremeCelsius);
        Assert.Equal(30, excursion.DurationMinutes);
    }

    [Fact]
    public void The_ends_of_the_label_are_inside_it() {
        // Act
        TemperatureHistory history = HistoryOf((0, 2m), (10, 1.9m), (20, 2m), (30, 8m), (40, 8.1m), (50, 8m));

        // Assert
        Assert.Equal(2, history.Excursions.Count);
        Assert.Equal(ExcursionKind.Cold, history.Excursions[0].Kind);
        Assert.Equal(10, history.Excursions[0].DurationMinutes);
        Assert.Equal(ExcursionKind.Warm, history.Excursions[1].Kind);
        Assert.Equal(10, history.Excursions[1].DurationMinutes);
        Assert.Equal(20, history.MinutesOutsideRange);
        Assert.Equal(10, history.LongestExcursionMinutes);
    }

    [Fact]
    public void A_record_ending_outside_the_label_leaves_an_open_excursion_up_to_the_last_reading() {
        // Act
        TemperatureHistory history = HistoryOf((0, 5m), (10, 9m), (20, 10m), (30, 11m));

        // Assert
        Excursion excursion = Assert.Single(history.Excursions);
        Assert.True(excursion.IsOpen);
        Assert.Equal(Origin.AddMinutes(10), excursion.StartedAt);
        Assert.Equal(Origin.AddMinutes(30), excursion.EndedAt);
        Assert.Equal(11m, excursion.ExtremeCelsius);
        Assert.Equal(20, history.MinutesOutsideRange);
    }

    [Fact]
    public void A_single_last_reading_outside_the_label_is_an_open_excursion_of_no_duration() {
        // Act
        TemperatureHistory history = HistoryOf((0, 5m), (10, 9m));

        // Assert
        Excursion excursion = Assert.Single(history.Excursions);
        Assert.True(excursion.IsOpen);
        Assert.Equal(0, excursion.DurationMinutes);
        Assert.Equal(0, history.MinutesOutsideRange);
    }

    [Fact]
    public void A_jump_from_cold_to_warm_closes_one_excursion_and_opens_the_other() {
        // Act
        TemperatureHistory history = HistoryOf((0, 5m), (10, 1m), (20, 9m), (30, 10m), (40, 5m));

        // Assert
        Assert.Equal(2, history.Excursions.Count);
        Excursion cold = history.Excursions[0];
        Excursion warm = history.Excursions[1];
        Assert.Equal(ExcursionKind.Cold, cold.Kind);
        Assert.Equal(Origin.AddMinutes(10), cold.StartedAt);
        Assert.Equal(Origin.AddMinutes(20), cold.EndedAt);
        Assert.False(cold.IsOpen);
        Assert.Equal(ExcursionKind.Warm, warm.Kind);
        Assert.Equal(Origin.AddMinutes(20), warm.StartedAt);
        Assert.Equal(Origin.AddMinutes(40), warm.EndedAt);
        Assert.Equal(10m, warm.ExtremeCelsius);
        Assert.Equal(30, history.MinutesOutsideRange);
        Assert.Equal(20, history.LongestExcursionMinutes);
    }

    [Fact]
    public void Excursions_add_up_and_the_longest_is_reported_apart() {
        // Act
        TemperatureHistory history = HistoryOf((0, 5m), (10, 9m), (20, 5m), (30, 1m), (40, 1m), (50, 1m), (60, 5m), (70, 9m), (80, 5m));

        // Assert
        Assert.Equal(3, history.Excursions.Count);
        Assert.Equal(10 + 30 + 10, history.MinutesOutsideRange);
        Assert.Equal(30, history.LongestExcursionMinutes);
    }

    [Fact]
    public void A_started_minute_is_charged_to_the_minutes_outside_the_label() {
        // Arrange
        TemperatureHistory history = NewHistory();
        history.Record(new AnyLoggerReading().WithLogger(_logger.Id).WithTakenAt(Origin).WithCelsius(5m).Generate());
        history.Record(new AnyLoggerReading().WithLogger(_logger.Id).WithTakenAt(Origin.AddSeconds(10)).WithCelsius(9m).Generate());
        history.Record(new AnyLoggerReading().WithLogger(_logger.Id).WithTakenAt(Origin.AddSeconds(100)).WithCelsius(5m).Generate());

        // Assert
        Assert.Equal(2, history.MinutesOutsideRange);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(-0.1, true)]
    [InlineData(25, false)]
    [InlineData(25.1, true)]
    public void A_reading_beyond_a_hard_limit_is_reported_whatever_the_rest_of_the_record(double celsius, bool breach) {
        // Act
        TemperatureHistory history = HistoryOf((0, 5m), (10, (decimal)celsius), (20, 5m));

        // Assert
        Assert.Equal(breach, history.HasHardLimitBreach);
    }

    [Fact]
    public void Excursions_are_computed_against_the_label_the_history_was_given() {
        // Arrange
        TemperatureHistory narrow = new TemperatureHistory(_logger, new TemperatureRange(4m, 6m));
        narrow.Record(ReadingAt(0, 5m));
        narrow.Record(ReadingAt(10, 7m));
        narrow.Record(ReadingAt(20, 5m));

        // Assert
        Excursion excursion = Assert.Single(narrow.Excursions);
        Assert.Equal(ExcursionKind.Warm, excursion.Kind);
        Assert.Equal(10, narrow.MinutesOutsideRange);
    }

    [Fact]
    public void An_empty_history_covers_nothing() {
        // Assert
        Assert.False(NewHistory().Covers(Origin, Origin.AddMinutes(60)));
    }

    [Fact]
    public void A_period_ending_before_it_starts_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => NewHistory().Covers(Origin.AddMinutes(60), Origin));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("to", refusal.ParamName);
    }

    [Fact]
    public void A_record_without_a_silence_longer_than_two_intervals_covers_the_period() {
        // Arrange: the logger records every 10 minutes, so 20 minutes of silence is still continuous.
        TemperatureHistory history = HistoryOf((0, 5m), (20, 5m), (40, 5m), (60, 5m));

        // Assert
        Assert.True(history.Covers(Origin, Origin.AddMinutes(60)));
    }

    [Fact]
    public void A_gap_of_more_than_two_intervals_between_readings_breaks_the_record() {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 5m), (10, 5m), (31, 5m), (40, 5m));

        // Assert
        Assert.False(history.Covers(Origin, Origin.AddMinutes(40)));
    }

    [Fact]
    public void A_gap_of_exactly_two_intervals_is_still_continuous() {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 5m), (10, 5m), (30, 5m), (40, 5m));

        // Assert
        Assert.True(history.Covers(Origin, Origin.AddMinutes(40)));
    }

    [Theory]
    [InlineData(-21, true)]
    [InlineData(-20, true)]
    [InlineData(0, true)]
    [InlineData(20, true)]
    [InlineData(21, false)]
    public void The_first_reading_must_not_come_later_than_two_intervals_after_the_start(int firstReadingMinute, bool covered) {
        // Arrange
        TemperatureHistory history = NewHistory();
        for (int minute = firstReadingMinute; minute < 60; minute += 10) {
            history.Record(ReadingAt(minute, 5m));
        }
        history.Record(ReadingAt(60, 5m));

        // Assert
        Assert.Equal(covered, history.Covers(Origin, Origin.AddMinutes(60)));
    }

    [Theory]
    [InlineData(80, true)]
    [InlineData(81, false)]
    [InlineData(60, true)]
    [InlineData(40, true)]
    [InlineData(39, true)]
    public void The_last_reading_must_come_within_two_intervals_of_the_end(int endMinute, bool covered) {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 5m), (20, 5m), (40, 5m), (60, 5m));

        // Assert
        Assert.Equal(covered, history.Covers(Origin, Origin.AddMinutes(endMinute)));
    }

    [Fact]
    public void A_period_of_no_length_is_covered_by_a_reading_close_enough() {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 5m));

        // Assert
        Assert.True(history.Covers(Origin, Origin));
        Assert.True(history.Covers(Origin.AddMinutes(20), Origin.AddMinutes(20)));
        Assert.False(history.Covers(Origin.AddMinutes(21), Origin.AddMinutes(21)));
    }

    [Fact]
    public void The_silence_allowed_follows_the_logger_interval() {
        // Arrange: a logger recording every 60 minutes allows 120 minutes of silence.
        DataLogger slowLogger = new AnyDataLogger().WithRecordingIntervalMinutes(60).Generate();

        TemperatureHistory continuous = new TemperatureHistory(slowLogger, _label);
        continuous.Record(new AnyLoggerReading().WithLogger(slowLogger.Id).WithTakenAt(Origin).WithCelsius(5m).Generate());
        continuous.Record(new AnyLoggerReading().WithLogger(slowLogger.Id).WithTakenAt(Origin.AddMinutes(120)).WithCelsius(5m).Generate());

        TemperatureHistory broken = new TemperatureHistory(slowLogger, _label);
        broken.Record(new AnyLoggerReading().WithLogger(slowLogger.Id).WithTakenAt(Origin).WithCelsius(5m).Generate());
        broken.Record(new AnyLoggerReading().WithLogger(slowLogger.Id).WithTakenAt(Origin.AddMinutes(121)).WithCelsius(5m).Generate());

        // Assert
        Assert.True(continuous.Covers(Origin, Origin.AddMinutes(120)));
        Assert.False(broken.Covers(Origin, Origin.AddMinutes(121)));
    }

}
