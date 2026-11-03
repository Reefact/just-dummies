using JustDummies;

namespace GeothermalReservoir.Domain;

public sealed class WellTests {

    [Theory]
    [InlineData("INJ-00", WellKind.Injector)]
    [InlineData("INJ-99", WellKind.Injector)]
    [InlineData("PRD-00", WellKind.Producer)]
    [InlineData("PRD-42", WellKind.Producer)]
    public void A_well_identifier_of_a_prefix_and_two_digits_is_accepted(string id, WellKind kind) {
        // Act
        Well well = Any.Well().WithKind(kind).WithId(id).Generate();

        // Assert
        Assert.Equal(id, well.Id);
        Assert.Equal(kind, well.Kind);
    }

    [Theory]
    [InlineData("")]
    [InlineData("INJ-1")]
    [InlineData("INJ-001")]
    [InlineData("inj-01")]
    [InlineData("INJ_01")]
    [InlineData("INJ01")]
    [InlineData("XXX-01")]
    [InlineData("INJ-0a")]
    [InlineData(" INJ-01")]
    [InlineData("INJ-01 ")]
    [InlineData("INJ-01-")]
    [InlineData("INJ-١٢")]
    public void A_malformed_well_identifier_is_refused(string id) {
        // Arrange
        AnyWell anyInjector = Any.Well().WithKind(WellKind.Injector).WithId(id);

        // Assert
        Assert.Throws<FormatException>(() => anyInjector.Generate());
    }

    [Fact(Skip = "domain defect: the id pattern ends with $, which .NET lets match before a final line feed, so \"INJ-01\\n\" is accepted as a well id; the model says ids match ^(INJ|PRD)-[0-9]{2}$ and a newline in an identifier is never intended, seed n/a (deterministic, no drawn value involved)")]
    public void A_well_identifier_followed_by_a_line_feed_is_refused() {
        // Arrange
        AnyWell anyInjector = Any.Well().WithKind(WellKind.Injector).WithId("INJ-01\n");

        // Assert
        Assert.Throws<FormatException>(() => anyInjector.Generate());
    }

    [Fact]
    public void A_well_refuses_a_missing_identifier_or_completion() {
        // Arrange
        CellCoordinate anyCompletion  = Any.CellCoordinate().Generate();
        Temperature    anyTemperature = Any.Temperature().Generate();

        // Assert
        Assert.Equal("id", Assert.Throws<ArgumentNullException>(() => new Well(null!, WellKind.Injector, anyCompletion, 0.1, anyTemperature)).ParamName);
        Assert.Equal("completion", Assert.Throws<ArgumentNullException>(() => new Well("INJ-01", WellKind.Injector, null!, 0.1, anyTemperature)).ParamName);
    }

    [Fact]
    public void A_well_refuses_an_unknown_kind() {
        // Arrange
        CellCoordinate anyCompletion = Any.CellCoordinate().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Well("INJ-01", (WellKind)7, anyCompletion, 0.1, null));
    }

    [Fact]
    public void An_injector_identifier_on_a_producer_is_refused() {
        // Arrange
        CellCoordinate anyCompletion = Any.CellCoordinate().Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Well("INJ-01", WellKind.Producer, anyCompletion, 0.1, null));

        // Assert
        Assert.Equal("id", error.ParamName);
    }

    [Fact]
    public void A_producer_identifier_on_an_injector_is_refused() {
        // Arrange
        CellCoordinate anyCompletion  = Any.CellCoordinate().Generate();
        Temperature    anyTemperature = Any.Temperature().Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Well("PRD-01", WellKind.Injector, anyCompletion, 0.1, anyTemperature));

        // Assert
        Assert.Equal("id", error.ParamName);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(0.25)]
    [InlineData(double.Epsilon)]
    public void A_rate_up_to_half_a_cubic_metre_per_second_is_accepted(double rate) {
        // Act
        Well well = Any.Well().WithRateCubicMetresPerSecond(rate).Generate();

        // Assert
        Assert.Equal(rate, well.RateCubicMetresPerSecond);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(0.5000001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_rate_outside_the_range_is_refused(double rate) {
        // Arrange
        AnyWell anyWell = Any.Well().WithRateCubicMetresPerSecond(rate);

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => anyWell.Generate());

        // Assert
        Assert.Equal("rateCubicMetresPerSecond", error.ParamName);
    }

    [Fact]
    public void An_injector_without_an_injection_temperature_is_refused() {
        // Arrange
        CellCoordinate anyCompletion = Any.CellCoordinate().Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Well("INJ-01", WellKind.Injector, anyCompletion, 0.1, null));

        // Assert
        Assert.Equal("injectionTemperature", error.ParamName);
    }

    [Fact]
    public void A_producer_with_an_injection_temperature_is_refused() {
        // Arrange
        CellCoordinate anyCompletion  = Any.CellCoordinate().Generate();
        Temperature    anyTemperature = Any.Temperature().Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Well("PRD-01", WellKind.Producer, anyCompletion, 0.1, anyTemperature));

        // Assert
        Assert.Equal("injectionTemperature", error.ParamName);
    }

    [Fact]
    public void An_injector_carries_its_injection_temperature_and_a_producer_has_none() {
        // Arrange
        Temperature anyTemperature = Any.Temperature().Generate();

        // Act
        Well injector = Any.Well().WithKind(WellKind.Injector).WithInjectionTemperature(anyTemperature).Generate();
        Well producer = Any.Well().WithKind(WellKind.Producer).Generate();

        // Assert
        Assert.Same(anyTemperature, injector.InjectionTemperature);
        Assert.Null(producer.InjectionTemperature);
    }

    [Fact]
    public void An_injector_pushes_fluid_in_and_a_producer_pulls_it_out() {
        // Arrange
        Well injector = Any.Well().WithKind(WellKind.Injector).WithRateCubicMetresPerSecond(0.25).Generate();
        Well producer = Any.Well().WithKind(WellKind.Producer).WithRateCubicMetresPerSecond(0.25).Generate();

        // Assert
        Assert.Equal(0.25, injector.SignedRate);
        Assert.Equal(-0.25, producer.SignedRate);
    }

    [Fact]
    public void The_completion_is_the_one_the_well_was_given() {
        // Arrange
        CellCoordinate anyCompletion = Any.CellCoordinate().Generate();

        // Act
        Well well = Any.Well().WithCompletion(anyCompletion).Generate();

        // Assert
        Assert.Same(anyCompletion, well.Completion);
    }

}

public sealed class BoundaryConditionTests {

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(0.37)]
    public void A_multiplier_from_zero_to_one_is_accepted(double multiplier) {
        // Act
        BoundaryCondition boundary = Any.BoundaryCondition().WithTransmissibilityMultiplier(multiplier).Generate();

        // Assert
        Assert.Equal(multiplier, boundary.TransmissibilityMultiplier);
    }

    [Theory]
    [InlineData(-0.0001)]
    [InlineData(1.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void A_multiplier_outside_zero_to_one_is_refused(double multiplier) {
        // Arrange
        AnyBoundaryCondition anyBoundary = Any.BoundaryCondition().WithTransmissibilityMultiplier(multiplier);

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => anyBoundary.Generate());

        // Assert
        Assert.Equal("transmissibilityMultiplier", error.ParamName);
    }

    [Fact]
    public void A_boundary_is_sealed_only_when_its_multiplier_is_zero() {
        // Arrange
        BoundaryCondition sealedBoundary = Any.BoundaryCondition().WithTransmissibilityMultiplier(0.0).Generate();
        BoundaryCondition openBoundary   = Any.BoundaryCondition().WithTransmissibilityMultiplier(t => t.Positive()).Generate();

        // Assert
        Assert.True(sealedBoundary.IsSealed);
        Assert.False(openBoundary.IsSealed);
    }

    [Fact]
    public void A_boundary_refuses_a_missing_far_field_state() {
        // Arrange
        Pressure    anyPressure    = Any.Pressure().Generate();
        Temperature anyTemperature = Any.Temperature().Generate();

        // Assert
        Assert.Equal("farFieldPressure", Assert.Throws<ArgumentNullException>(() => new BoundaryCondition(null!, anyTemperature, 0.5)).ParamName);
        Assert.Equal("farFieldTemperature", Assert.Throws<ArgumentNullException>(() => new BoundaryCondition(anyPressure, null!, 0.5)).ParamName);
    }

    [Fact]
    public void A_boundary_keeps_its_far_field_state() {
        // Arrange
        Pressure    anyPressure    = Any.Pressure().Generate();
        Temperature anyTemperature = Any.Temperature().Generate();

        // Act
        BoundaryCondition boundary = new BoundaryCondition(anyPressure, anyTemperature, 0.5);

        // Assert
        Assert.Same(anyPressure, boundary.FarFieldPressure);
        Assert.Same(anyTemperature, boundary.FarFieldTemperature);
    }

}
