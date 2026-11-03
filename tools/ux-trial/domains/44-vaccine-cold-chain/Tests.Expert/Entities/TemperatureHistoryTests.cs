using ColdChain.Domain;
using ColdChain.Tests.Expert.Generators;
using ColdChain.Tests.Expert.Support;
using JustDummies;
using Warehouse.Domain;
using Xunit;

namespace ColdChain.Tests.Expert.Entities;

public sealed class TemperatureHistoryTests {

    private static readonly TemperatureRange Label = new TemperatureRange(2m, 8m);
    private static readonly DateTimeOffset   Start = new DateTimeOffset(2026, 3, 9, 8, 0, 0, TimeSpan.Zero);

    // A logger recording every 5 minutes, hence tolerating a silence of 10.
    private static TemperatureHistory NewHistory() {
        return new TemperatureHistory(Any.DataLogger().WithRecordingIntervalMinutes(5).Generate(), Label);
    }

    private static LoggerReading ReadingAt(TemperatureHistory history, int minutesAfterStart, decimal celsius) {
        return Any.LoggerReading().WithLogger(history.Logger.Id).WithTakenAt(Start.AddMinutes(minutesAfterStart)).WithCelsius(celsius).Generate();
    }

    private static TemperatureHistory HistoryOf(params (int Minute, decimal Celsius)[] readings) {
        TemperatureHistory history = NewHistory();

        foreach ((int minute, decimal celsius) in readings) {
            history.Record(ReadingAt(history, minute, celsius));
        }

        return history;
    }

    [Fact]
    public void A_history_without_readings_shows_nothing() {
        // Arrange
        TemperatureHistory history = NewHistory();

        // Assert
        Assert.Empty(history.Readings);
        Assert.Null(history.Last);
        Assert.Empty(history.Excursions);
        Assert.Equal(0, history.MinutesOutsideRange);
        Assert.Equal(0, history.LongestExcursionMinutes);
        Assert.False(history.HasHardLimitBreach);
    }

    [Fact]
    public void Readings_are_kept_in_the_order_they_were_recorded_and_the_last_one_is_the_latest() {
        // Arrange
        TemperatureHistory history = NewHistory();
        LoggerReading      first   = ReadingAt(history, 0, 5m);
        LoggerReading      second  = ReadingAt(history, 5, 5.5m);

        // Act
        history.Record(first);
        history.Record(second);

        // Assert
        Assert.Equal([first, second], history.Readings);
        Assert.Same(second, history.Last);
    }

    [Fact]
    public void A_reading_of_another_logger_is_refused() {
        // Arrange
        TemperatureHistory history  = NewHistory();
        LoggerReading      anyStranger = Any.LoggerReading().WithTakenAt(Start).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => history.Record(anyStranger));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(0)]
    [InlineData(-5)]
    public void A_reading_that_does_not_come_strictly_after_the_last_one_is_refused(int minutesAfterStart) {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 5m), (5, 5m));
        LoggerReading      late    = ReadingAt(history, minutesAfterStart, 5m);

        // Assert
        Assert.Throws<InvalidOperationException>(() => history.Record(late));
    }

    [Fact]
    public void A_missing_reading_is_refused() {
        // Arrange
        TemperatureHistory history = NewHistory();

        // Assert
        Assert.Throws<ArgumentNullException>(() => history.Record(null!));
    }

    [Fact]
    public void Readings_inside_the_label_including_its_two_ends_are_no_excursion() {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 2m), (5, 5m), (10, 8m), (15, 2m));

        // Assert
        Assert.Empty(history.Excursions);
        Assert.Equal(0, history.MinutesOutsideRange);
    }

    [Fact]
    public void An_excursion_runs_from_the_first_reading_outside_the_label_to_the_next_one_back_inside() {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 5m), (5, 1.5m), (10, 1m), (15, 5m));

        // Act
        Excursion excursion = Assert.Single(history.Excursions);

        // Assert
        Assert.Equal(ExcursionKind.Cold, excursion.Kind);
        Assert.Equal(Start.AddMinutes(5), excursion.StartedAt);
        Assert.Equal(Start.AddMinutes(15), excursion.EndedAt);
        Assert.Equal(1m, excursion.ExtremeCelsius);
        Assert.False(excursion.IsOpen);
        Assert.Equal(10, history.MinutesOutsideRange);
        Assert.Equal(10, history.LongestExcursionMinutes);
    }

    [Fact]
    public void A_warm_excursion_keeps_the_warmest_reading_as_its_extreme() {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 5m), (5, 9m), (10, 12.5m), (15, 10m), (20, 5m));

        // Act
        Excursion excursion = Assert.Single(history.Excursions);

        // Assert
        Assert.Equal(ExcursionKind.Warm, excursion.Kind);
        Assert.Equal(12.5m, excursion.ExtremeCelsius);
        Assert.Equal(15, excursion.DurationMinutes);
    }

    [Fact]
    public void An_excursion_on_the_edge_of_the_label_starts_at_the_first_reading_beyond_it() {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 8m), (5, 8.1m), (10, 8m));

        // Act
        Excursion excursion = Assert.Single(history.Excursions);

        // Assert
        Assert.Equal(5, excursion.DurationMinutes);
        Assert.Equal(ExcursionKind.Warm, excursion.Kind);
    }

    [Fact]
    public void A_record_ending_outside_the_label_ends_with_an_open_excursion_closed_on_the_last_reading() {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 5m), (5, 9m), (10, 10m));

        // Act
        Excursion excursion = Assert.Single(history.Excursions);

        // Assert
        Assert.True(excursion.IsOpen);
        Assert.Equal(Start.AddMinutes(5), excursion.StartedAt);
        Assert.Equal(Start.AddMinutes(10), excursion.EndedAt);
        Assert.Equal(5, history.MinutesOutsideRange);
    }

    [Fact]
    public void A_last_reading_outside_the_label_on_its_own_is_an_open_excursion_of_no_duration() {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 5m), (5, 1m));

        // Act
        Excursion excursion = Assert.Single(history.Excursions);

        // Assert
        Assert.True(excursion.IsOpen);
        Assert.Equal(0, excursion.DurationMinutes);
        Assert.Equal(0, history.MinutesOutsideRange);
    }

    [Fact]
    public void A_jump_from_cold_to_warm_closes_the_cold_excursion_and_opens_the_warm_one_at_the_same_reading() {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 5m), (5, 1m), (10, 9m), (15, 5m));

        // Act
        IReadOnlyList<Excursion> excursions = history.Excursions;

        // Assert
        Assert.Equal(2, excursions.Count);
        Assert.Equal(ExcursionKind.Cold, excursions[0].Kind);
        Assert.Equal(Start.AddMinutes(10), excursions[0].EndedAt);
        Assert.Equal(ExcursionKind.Warm, excursions[1].Kind);
        Assert.Equal(Start.AddMinutes(10), excursions[1].StartedAt);
        Assert.Equal(Start.AddMinutes(15), excursions[1].EndedAt);
        Assert.Equal(10, history.MinutesOutsideRange);
    }

    [Fact]
    public void Separate_excursions_add_up_while_the_longest_stands_alone() {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 5m), (5, 9m), (10, 5m), (15, 5m), (20, 1m), (25, 1m), (30, 1m), (35, 5m));

        // Assert
        Assert.Equal(2, history.Excursions.Count);
        Assert.Equal(5 + 15, history.MinutesOutsideRange);
        Assert.Equal(15, history.LongestExcursionMinutes);
    }

    [Fact]
    public void An_excursion_charges_every_minute_it_started() {
        // Arrange
        TemperatureHistory history = NewHistory();

        // Act
        history.Record(Any.LoggerReading().WithLogger(history.Logger.Id).WithTakenAt(Start).WithCelsius(5m).Generate());
        history.Record(Any.LoggerReading().WithLogger(history.Logger.Id).WithTakenAt(Start.AddSeconds(30)).WithCelsius(9m).Generate());
        history.Record(Any.LoggerReading().WithLogger(history.Logger.Id).WithTakenAt(Start.AddSeconds(30 + 61)).WithCelsius(5m).Generate());

        // Assert
        Assert.Equal(2, history.MinutesOutsideRange);
    }

    [Theory]
    [InlineData("-0.1", true)]
    [InlineData("0", false)]
    [InlineData("25", false)]
    [InlineData("25.1", true)]
    public void A_reading_below_zero_or_above_25_degrees_is_a_hard_limit_breach_and_zero_and_25_are_not(string celsius, bool expectedBreach) {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 5m), (5, Celsius.Of(celsius)));

        // Assert
        Assert.Equal(expectedBreach, history.HasHardLimitBreach);
    }

    [Fact]
    public void A_record_without_readings_covers_nothing() {
        // Assert
        Assert.False(NewHistory().Covers(Start, Start.AddMinutes(30)));
    }

    [Fact]
    public void A_record_covers_a_period_when_its_readings_never_stay_silent_longer_than_two_intervals() {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 5m), (10, 5m), (20, 5m), (30, 5m));

        // Assert
        Assert.True(history.Covers(Start, Start.AddMinutes(30)));
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public void The_first_reading_may_come_up_to_two_intervals_after_the_start_of_the_period(int firstReadingMinute, bool expectedCovered) {
        // Arrange
        TemperatureHistory history = HistoryOf((firstReadingMinute, 5m), (firstReadingMinute + 5, 5m));

        // Assert
        Assert.Equal(expectedCovered, history.Covers(Start, Start.AddMinutes(firstReadingMinute + 5)));
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public void The_last_reading_may_come_up_to_two_intervals_before_the_end_of_the_period(int minutesBeforeEnd, bool expectedCovered) {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 5m), (5, 5m));

        // Assert
        Assert.Equal(expectedCovered, history.Covers(Start, Start.AddMinutes(5 + minutesBeforeEnd)));
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public void Two_consecutive_readings_may_be_up_to_two_intervals_apart(int gapMinutes, bool expectedCovered) {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 5m), (gapMinutes, 5m), (gapMinutes + 5, 5m));

        // Assert
        Assert.Equal(expectedCovered, history.Covers(Start, Start.AddMinutes(gapMinutes + 5)));
    }

    [Fact]
    public void A_period_cannot_end_before_it_starts() {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 5m));

        // Assert
        Assert.Throws<ArgumentException>(() => history.Covers(Start.AddMinutes(10), Start));
    }

    [Fact]
    public void An_instantaneous_period_is_covered_by_a_reading_close_enough_to_it() {
        // Arrange
        TemperatureHistory history = HistoryOf((0, 5m));

        // Assert
        Assert.True(history.Covers(Start, Start));
    }

    [Fact]
    public void A_missing_logger_or_label_is_refused() {
        // Arrange
        DataLogger anyLogger = Any.DataLogger().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new TemperatureHistory(null!, Label));
        Assert.Throws<ArgumentNullException>(() => new TemperatureHistory(anyLogger, null!));
    }

}
