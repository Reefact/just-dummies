using ColdChain.Domain;

using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace ColdChain.Tests;

public sealed class ExcursionTests {

    private static readonly TemperatureRange Label = new TemperatureRange(2m, 8m);
    private static readonly DateTimeOffset   Start = new DateTimeOffset(2026, 3, 2, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_cold_excursion_keeps_everything_it_was_built_with() {
        // Arrange
        DateTimeOffset end = Start.AddMinutes(30);

        // Act
        Excursion excursion = new Excursion(ExcursionKind.Cold, Label, Start, end, 1.5m, isOpen: false);

        // Assert
        Assert.Equal(ExcursionKind.Cold, excursion.Kind);
        Assert.Same(Label, excursion.LabelledRange);
        Assert.Equal(Start, excursion.StartedAt);
        Assert.Equal(end, excursion.EndedAt);
        Assert.Equal(1.5m, excursion.ExtremeCelsius);
        Assert.False(excursion.IsOpen);
    }

    [Fact]
    public void A_cold_excursion_goes_strictly_below_the_minimum() {
        // Assert
        Assert.NotNull(new Excursion(ExcursionKind.Cold, Label, Start, Start, 1.9m, false));
        ArgumentException atMinimum = Assert.Throws<ArgumentException>(() => new Excursion(ExcursionKind.Cold, Label, Start, Start, 2.0m, false));
        Assert.Equal("extremeCelsius", atMinimum.ParamName);
        Assert.Throws<ArgumentException>(() => new Excursion(ExcursionKind.Cold, Label, Start, Start, 5m, false));
        Assert.Throws<ArgumentException>(() => new Excursion(ExcursionKind.Cold, Label, Start, Start, 12m, false));
    }

    [Fact]
    public void A_warm_excursion_goes_strictly_above_the_maximum() {
        // Assert
        Assert.NotNull(new Excursion(ExcursionKind.Warm, Label, Start, Start, 8.1m, false));
        ArgumentException atMaximum = Assert.Throws<ArgumentException>(() => new Excursion(ExcursionKind.Warm, Label, Start, Start, 8.0m, false));
        Assert.Equal("extremeCelsius", atMaximum.ParamName);
        Assert.Throws<ArgumentException>(() => new Excursion(ExcursionKind.Warm, Label, Start, Start, 5m, false));
        Assert.Throws<ArgumentException>(() => new Excursion(ExcursionKind.Warm, Label, Start, Start, -3m, false));
    }

    [Fact]
    public void An_excursion_cannot_end_before_it_starts() {
        // Act
        Exception? thrown = Record.Exception(() => new Excursion(ExcursionKind.Cold, Label, Start, Start.AddTicks(-1), 1m, false));

        // Assert
        ArgumentException refusal = Assert.IsType<ArgumentException>(thrown);
        Assert.Equal("endedAt", refusal.ParamName);
    }

    [Fact]
    public void An_excursion_may_start_and_end_at_the_same_instant() {
        // Act
        Excursion excursion = new Excursion(ExcursionKind.Warm, Label, Start, Start, 9m, true);

        // Assert
        Assert.Equal(TimeSpan.Zero, excursion.Duration);
        Assert.Equal(0, excursion.DurationMinutes);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(99)]
    public void An_unknown_kind_is_refused(int kind) {
        // Act
        Exception? thrown = Record.Exception(() => new Excursion((ExcursionKind)kind, Label, Start, Start, 1m, false));

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Fact]
    public void A_missing_label_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new Excursion(ExcursionKind.Cold, null!, Start, Start, 1m, false));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(59, 1)]
    [InlineData(60, 1)]
    [InlineData(61, 2)]
    [InlineData(90, 2)]
    [InlineData(119, 2)]
    [InlineData(120, 2)]
    [InlineData(121, 3)]
    public void A_started_minute_is_charged_so_the_duration_rounds_up(int seconds, int minutes) {
        // Arrange
        Excursion excursion = new AnyExcursion().WithStartedAt(Start).WithEndedAt(Start.AddSeconds(seconds)).Generate();

        // Assert
        Assert.Equal(minutes, excursion.DurationMinutes);
        Assert.Equal(TimeSpan.FromSeconds(seconds), excursion.Duration);
    }

    [Fact]
    public void Even_a_fraction_of_a_second_is_charged_a_full_minute() {
        // Arrange
        Excursion excursion = new AnyExcursion().WithStartedAt(Start).WithEndedAt(Start.AddMilliseconds(1)).Generate();

        // Assert
        Assert.Equal(1, excursion.DurationMinutes);
    }

    [Theory]
    [InlineData(ExcursionKind.Cold, -0.1, true)]
    [InlineData(ExcursionKind.Cold, 0.0, false)]
    [InlineData(ExcursionKind.Cold, 1.9, false)]
    [InlineData(ExcursionKind.Warm, 25.0, false)]
    [InlineData(ExcursionKind.Warm, 25.1, true)]
    [InlineData(ExcursionKind.Warm, 9.0, false)]
    public void An_excursion_breaches_the_hard_limit_when_its_extreme_does(ExcursionKind kind, double extreme, bool breaches) {
        // Arrange
        Excursion excursion = new Excursion(kind, Label, Start, Start.AddMinutes(10), (decimal)extreme, false);

        // Assert
        Assert.Equal(breaches, excursion.BreachesHardLimit);
    }

    [Fact]
    public void Excursions_are_equal_when_everything_but_the_instance_is() {
        // Arrange
        Excursion excursion = new Excursion(ExcursionKind.Warm, Label, Start, Start.AddMinutes(30), 9m, false);
        Excursion same      = new Excursion(ExcursionKind.Warm, new TemperatureRange(2m, 8m), Start, Start.AddMinutes(30), 9m, false);

        // Assert
        Assert.Equal(excursion, same);
        Assert.Equal(excursion.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(excursion, new Excursion(ExcursionKind.Warm, Label, Start, Start.AddMinutes(30), 9m, true));
        Assert.NotEqual(excursion, new Excursion(ExcursionKind.Warm, Label, Start, Start.AddMinutes(31), 9m, false));
        Assert.NotEqual(excursion, new Excursion(ExcursionKind.Warm, Label, Start, Start.AddMinutes(30), 9.1m, false));
        Assert.NotEqual(excursion, new Excursion(ExcursionKind.Warm, new TemperatureRange(2m, 7m), Start, Start.AddMinutes(30), 9m, false));
    }

    [Fact]
    public void The_generator_only_draws_excursions_the_constructor_accepts() {
        // Act
        Excursion anyExcursion = new AnyExcursion().Generate();

        // Assert
        Assert.True(anyExcursion.EndedAt >= anyExcursion.StartedAt);
        Assert.True(anyExcursion.Kind == ExcursionKind.Cold ? anyExcursion.ExtremeCelsius < anyExcursion.LabelledRange.MinCelsius : anyExcursion.ExtremeCelsius > anyExcursion.LabelledRange.MaxCelsius);
    }

}
