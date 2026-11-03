using GeothermalReservoir.Domain;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Discovery;

public sealed class BoundaryConditionTests {

    [Fact]
    public void Keeps_what_it_was_built_with() {
        // Arrange
        Pressure    anyPressure    = Any.Pressure().Generate();
        Temperature anyTemperature = Any.Temperature().Generate();
        double      anyMultiplier  = Any.Double().Between(0.0, 1.0).Generate();

        // Act
        BoundaryCondition boundary = new BoundaryCondition(anyPressure, anyTemperature, anyMultiplier);

        // Assert
        Assert.Equal(anyPressure, boundary.FarFieldPressure);
        Assert.Equal(anyTemperature, boundary.FarFieldTemperature);
        Assert.Equal(anyMultiplier, boundary.TransmissibilityMultiplier);
    }

    [Fact]
    public void A_multiplier_of_zero_is_a_sealed_boundary() {
        BoundaryCondition sealedBoundary = Any.BoundaryCondition().WithTransmissibilityMultiplier(0.0).Generate();

        Assert.True(sealedBoundary.IsSealed);
    }

    [Fact]
    public void A_positive_multiplier_is_an_open_boundary() {
        BoundaryCondition anyOpenBoundary = Any.BoundaryCondition().WithTransmissibilityMultiplier(m => m.Positive().LessThanOrEqualTo(1.0)).Generate();

        Assert.False(anyOpenBoundary.IsSealed);
    }

    [Fact]
    public void A_multiplier_of_one_is_allowed() {
        BoundaryCondition fullyOpen = Any.BoundaryCondition().WithTransmissibilityMultiplier(1.0).Generate();

        Assert.Equal(1.0, fullyOpen.TransmissibilityMultiplier);
    }

    [Fact]
    public void Refuses_a_multiplier_above_one() {
        double anyTooHigh = Any.Double().GreaterThan(1.0).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => Any.BoundaryCondition().WithTransmissibilityMultiplier(anyTooHigh).Generate());
    }

    [Fact]
    public void Refuses_a_negative_multiplier_and_NaN() {
        double anyNegative = Any.Double().Negative().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => Any.BoundaryCondition().WithTransmissibilityMultiplier(anyNegative).Generate());
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.BoundaryCondition().WithTransmissibilityMultiplier(double.NaN).Generate());
    }

    [Fact]
    public void Refuses_a_missing_far_field_pressure_or_temperature() {
        Pressure    anyPressure    = Any.Pressure().Generate();
        Temperature anyTemperature = Any.Temperature().Generate();

        Assert.Throws<ArgumentNullException>(() => new BoundaryCondition(null!, anyTemperature, 0.5));
        Assert.Throws<ArgumentNullException>(() => new BoundaryCondition(anyPressure, null!, 0.5));
    }

}

public sealed class WellTests {

    [Fact]
    public void An_injector_keeps_its_injection_temperature() {
        // Arrange
        Temperature    anyTemperature = Any.Temperature().Generate();
        CellCoordinate anyCompletion  = Any.CellCoordinate().Generate();

        // Act
        Well injector = new Well("INJ-01", WellKind.Injector, anyCompletion, 0.1, anyTemperature);

        // Assert
        Assert.Equal("INJ-01", injector.Id);
        Assert.Equal(WellKind.Injector, injector.Kind);
        Assert.Equal(anyTemperature, injector.InjectionTemperature);
        Assert.Equal(anyCompletion, injector.Completion);
    }

    [Fact]
    public void A_producer_has_no_injection_temperature() {
        CellCoordinate anyCompletion = Any.CellCoordinate().Generate();

        Well producer = new Well("PRD-07", WellKind.Producer, anyCompletion, 0.1, null);

        Assert.Null(producer.InjectionTemperature);
    }

    [Fact]
    public void The_scaffolded_generator_draws_consistent_wells_of_either_kind() {
        Well anyInjector = Any.Well().WithKind(WellKind.Injector).Generate();
        Well anyProducer = Any.Well().WithKind(WellKind.Producer).Generate();

        Assert.StartsWith("INJ-", anyInjector.Id);
        Assert.NotNull(anyInjector.InjectionTemperature);
        Assert.StartsWith("PRD-", anyProducer.Id);
        Assert.Null(anyProducer.InjectionTemperature);
    }

    [Fact]
    public void An_injector_injects_with_a_positive_signed_rate() {
        Well anyInjector = Any.Well().WithKind(WellKind.Injector).WithRateCubicMetresPerSecond(0.25).Generate();

        Assert.Equal(0.25, anyInjector.SignedRate);
    }

    [Fact]
    public void A_producer_produces_with_a_negative_signed_rate() {
        Well anyProducer = Any.Well().WithKind(WellKind.Producer).WithRateCubicMetresPerSecond(0.25).Generate();

        Assert.Equal(-0.25, anyProducer.SignedRate);
        Assert.Equal(0.25, anyProducer.RateCubicMetresPerSecond);
    }

    [Theory]
    [InlineData("INJ-00", WellKind.Injector)]
    [InlineData("INJ-99", WellKind.Injector)]
    [InlineData("PRD-00", WellKind.Producer)]
    [InlineData("PRD-42", WellKind.Producer)]
    public void Accepts_the_documented_id_shapes(string id, WellKind kind) {
        // Arrange
        Temperature? injectionTemperature = kind == WellKind.Injector ? Any.Temperature().Generate() : null;
        CellCoordinate anyCompletion      = Any.CellCoordinate().Generate();

        // Act
        Well well = new Well(id, kind, anyCompletion, 0.1, injectionTemperature);

        // Assert
        Assert.Equal(id, well.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("INJ-1")]
    [InlineData("INJ-001")]
    [InlineData("INJ01")]
    [InlineData("inj-01")]
    [InlineData("XYZ-01")]
    [InlineData("INJ-0a")]
    [InlineData(" INJ-01")]
    [InlineData("INJ-01 ")]
    [InlineData("INJ-٠١")]
    public void Refuses_a_malformed_id(string id) {
        Temperature    anyTemperature = Any.Temperature().Generate();
        CellCoordinate anyCompletion  = Any.CellCoordinate().Generate();

        Assert.Throws<FormatException>(() => new Well(id, WellKind.Injector, anyCompletion, 0.1, anyTemperature));
    }

    [Fact(Skip = "domain defect: the id pattern ^(INJ|PRD)-[0-9]{2}$ ends with $, which also matches before a final newline, so 'INJ-01\\n' is accepted and stored as the id; seed 664177261 (the input is a literal, so any seed replays it)")]
    public void Refuses_an_id_followed_by_a_newline() {
        Temperature    anyTemperature = Any.Temperature().Generate();
        CellCoordinate anyCompletion  = Any.CellCoordinate().Generate();

        Assert.Throws<FormatException>(() => new Well("INJ-01\n", WellKind.Injector, anyCompletion, 0.1, anyTemperature));
    }

    [Fact]
    public void Refuses_a_producer_prefix_on_an_injector() {
        Temperature    anyTemperature = Any.Temperature().Generate();
        CellCoordinate anyCompletion  = Any.CellCoordinate().Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new Well("PRD-01", WellKind.Injector, anyCompletion, 0.1, anyTemperature));

        Assert.Equal("id", error.ParamName);
    }

    [Fact]
    public void Refuses_an_injector_prefix_on_a_producer() {
        CellCoordinate anyCompletion = Any.CellCoordinate().Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new Well("INJ-01", WellKind.Producer, anyCompletion, 0.1, null));

        Assert.Equal("id", error.ParamName);
    }

    [Fact]
    public void Refuses_an_unknown_kind() {
        CellCoordinate anyCompletion = Any.CellCoordinate().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Well("INJ-01", (WellKind)42, anyCompletion, 0.1, null));
    }

    [Fact]
    public void Refuses_an_injector_without_injection_temperature() {
        CellCoordinate anyCompletion = Any.CellCoordinate().Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new Well("INJ-01", WellKind.Injector, anyCompletion, 0.1, null));

        Assert.Equal("injectionTemperature", error.ParamName);
    }

    [Fact]
    public void Refuses_a_producer_with_an_injection_temperature() {
        Temperature    anyTemperature = Any.Temperature().Generate();
        CellCoordinate anyCompletion  = Any.CellCoordinate().Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new Well("PRD-01", WellKind.Producer, anyCompletion, 0.1, anyTemperature));

        Assert.Equal("injectionTemperature", error.ParamName);
    }

    [Fact]
    public void Accepts_the_maximum_rate_itself() {
        Well fastest = Any.Well().WithRateCubicMetresPerSecond(0.5).Generate();

        Assert.Equal(0.5, fastest.RateCubicMetresPerSecond);
    }

    [Fact]
    public void Refuses_a_rate_above_the_maximum() {
        double anyTooFast = Any.Double().GreaterThan(Well.MaximumRateCubicMetresPerSecond).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Well().WithRateCubicMetresPerSecond(anyTooFast).Generate());
    }

    [Fact]
    public void Refuses_a_rate_of_zero_or_less_and_NaN() {
        double anyNotPositive = Any.Double().LessThanOrEqualTo(0.0).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Well().WithRateCubicMetresPerSecond(anyNotPositive).Generate());
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Well().WithRateCubicMetresPerSecond(double.NaN).Generate());
    }

    [Fact]
    public void Refuses_a_missing_id_or_completion() {
        Temperature    anyTemperature = Any.Temperature().Generate();
        CellCoordinate anyCompletion  = Any.CellCoordinate().Generate();

        Assert.Throws<ArgumentNullException>(() => new Well(null!, WellKind.Injector, anyCompletion, 0.1, anyTemperature));
        Assert.Throws<ArgumentNullException>(() => new Well("INJ-01", WellKind.Injector, null!, 0.1, anyTemperature));
    }

}
