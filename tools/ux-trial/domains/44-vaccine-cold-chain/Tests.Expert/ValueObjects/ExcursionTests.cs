using ColdChain.Domain;
using ColdChain.Tests.Expert.Generators;
using ColdChain.Tests.Expert.Support;
using JustDummies;
using Warehouse.Domain;
using Xunit;

namespace ColdChain.Tests.Expert.ValueObjects;

public sealed class ExcursionTests {

    private static readonly TemperatureRange Label     = new TemperatureRange(2m, 8m);
    private static readonly DateTimeOffset   StartedAt = new DateTimeOffset(2026, 3, 9, 8, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("1.9")]
    [InlineData("-3")]
    public void A_cold_excursion_goes_below_the_minimum_of_the_label(string extreme) {
        // Act
        Excursion excursion = Any.Excursion().WithKind(ExcursionKind.Cold).WithLabelledRange(Label).WithExtremeCelsius(Celsius.Of(extreme)).Generate();

        // Assert
        Assert.Equal(Celsius.Of(extreme), excursion.ExtremeCelsius);
    }

    [Theory]
    [InlineData("2")]
    [InlineData("5")]
    [InlineData("9")]
    public void A_cold_excursion_that_does_not_go_strictly_below_the_minimum_is_refused(string extreme) {
        // Arrange
        AnyExcursion excursions = Any.Excursion().WithKind(ExcursionKind.Cold).WithLabelledRange(Label).WithExtremeCelsius(Celsius.Of(extreme));

        // Assert
        Assert.Throws<ArgumentException>(() => excursions.Generate());
    }

    [Theory]
    [InlineData("8.1")]
    [InlineData("30")]
    public void A_warm_excursion_goes_above_the_maximum_of_the_label(string extreme) {
        // Act
        Excursion excursion = Any.Excursion().WithKind(ExcursionKind.Warm).WithLabelledRange(Label).WithExtremeCelsius(Celsius.Of(extreme)).Generate();

        // Assert
        Assert.Equal(Celsius.Of(extreme), excursion.ExtremeCelsius);
    }

    [Theory]
    [InlineData("8")]
    [InlineData("5")]
    [InlineData("1")]
    public void A_warm_excursion_that_does_not_go_strictly_above_the_maximum_is_refused(string extreme) {
        // Arrange
        AnyExcursion excursions = Any.Excursion().WithKind(ExcursionKind.Warm).WithLabelledRange(Label).WithExtremeCelsius(Celsius.Of(extreme));

        // Assert
        Assert.Throws<ArgumentException>(() => excursions.Generate());
    }

    [Fact]
    public void An_excursion_of_an_unknown_kind_is_refused() {
        // Arrange
        AnyExcursion excursions = Any.Excursion().WithKind((ExcursionKind)99);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => excursions.Generate());
    }

    [Fact]
    public void An_excursion_cannot_end_before_it_starts() {
        // Arrange
        AnyExcursion excursions = Any.Excursion().WithStartedAt(StartedAt).WithDuration(TimeSpan.FromSeconds(-1));

        // Assert
        Assert.Throws<ArgumentException>(() => excursions.Generate());
    }

    [Fact]
    public void An_excursion_may_end_at_the_instant_it_starts() {
        // Act
        Excursion excursion = Any.Excursion().WithStartedAt(StartedAt).WithDuration(TimeSpan.Zero).Generate();

        // Assert
        Assert.Equal(excursion.StartedAt, excursion.EndedAt);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(59, 1)]
    [InlineData(60, 1)]
    [InlineData(61, 2)]
    [InlineData(600, 10)]
    [InlineData(601, 11)]
    public void A_started_minute_is_charged_so_the_duration_is_rounded_up_to_whole_minutes(int seconds, int expectedMinutes) {
        // Arrange
        Excursion excursion = Any.Excursion().WithStartedAt(StartedAt).WithDuration(TimeSpan.FromSeconds(seconds)).Generate();

        // Assert
        Assert.Equal(expectedMinutes, excursion.DurationMinutes);
    }

    [Theory]
    [InlineData("-0.1", true)]
    [InlineData("0", false)]
    [InlineData("1", false)]
    public void A_cold_excursion_breaches_the_hard_limit_only_by_going_below_zero(string extreme, bool expectedBreach) {
        // Arrange
        Excursion excursion = Any.Excursion().WithKind(ExcursionKind.Cold).WithLabelledRange(Label).WithExtremeCelsius(Celsius.Of(extreme)).Generate();

        // Assert
        Assert.Equal(expectedBreach, excursion.BreachesHardLimit);
    }

    [Theory]
    [InlineData("25", false)]
    [InlineData("25.1", true)]
    [InlineData("12", false)]
    public void A_warm_excursion_breaches_the_hard_limit_only_by_going_above_25_degrees(string extreme, bool expectedBreach) {
        // Arrange
        Excursion excursion = Any.Excursion().WithKind(ExcursionKind.Warm).WithLabelledRange(Label).WithExtremeCelsius(Celsius.Of(extreme)).Generate();

        // Assert
        Assert.Equal(expectedBreach, excursion.BreachesHardLimit);
    }

    [Fact]
    public void An_excursion_still_open_when_the_record_ended_says_so() {
        // Arrange
        Excursion excursion = Any.Excursion().WithIsOpen(true).Generate();

        // Assert
        Assert.True(excursion.IsOpen);
    }

    [Fact]
    public void Two_excursions_described_the_same_way_are_equal_and_an_open_one_differs_from_a_closed_one() {
        // Arrange
        AnyExcursion same = Any.Excursion().WithKind(ExcursionKind.Cold).WithLabelledRange(Label).WithStartedAt(StartedAt).WithDuration(TimeSpan.FromMinutes(10)).WithExtremeCelsius(1m);

        // Assert
        Assert.Equal(same.WithIsOpen(false).Generate(), same.WithIsOpen(false).Generate());
        Assert.NotEqual(same.WithIsOpen(false).Generate(), same.WithIsOpen(true).Generate());
    }

}
