using GeothermalReservoir.Domain;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Discovery;

public sealed class WellTests {

    [Fact]
    public void A_producer_draws_fluid_out_of_the_reservoir() {
        // Arrange
        Well producer = Any.Well().WithKind(WellKind.Producer).WithRateCubicMetresPerSecond(0.2).Generate();

        // Act
        double signedRate = producer.SignedRate;

        // Assert
        Assert.Equal(-0.2, signedRate);
    }

    [Fact]
    public void An_injector_pushes_fluid_into_the_reservoir() {
        // Arrange
        Well injector = Any.Well().WithKind(WellKind.Injector).WithRateCubicMetresPerSecond(0.2).Generate();

        // Act
        double signedRate = injector.SignedRate;

        // Assert
        Assert.Equal(0.2, signedRate);
    }

    [Fact]
    public void A_well_named_as_an_injector_cannot_be_declared_a_producer() {
        // Arrange
        CellCoordinate anyCompletion = Any.CellCoordinate().Generate();
        double         anyRate       = Any.Double().GreaterThan(0.0).LessThanOrEqualTo(Well.MaximumRateCubicMetresPerSecond).Generate();

        // Act
        Exception? error = Record.Exception(() => new Well("INJ-01", WellKind.Producer, anyCompletion, anyRate, null));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_well_id_outside_the_naming_convention_is_rejected() {
        // Arrange
        CellCoordinate anyCompletion = Any.CellCoordinate().Generate();
        double         anyRate       = Any.Double().GreaterThan(0.0).LessThanOrEqualTo(Well.MaximumRateCubicMetresPerSecond).Generate();

        // Act
        Exception? error = Record.Exception(() => new Well("WELL-1", WellKind.Producer, anyCompletion, anyRate, null));

        // Assert
        Assert.IsType<FormatException>(error);
    }

    [Fact]
    public void An_injector_must_state_the_temperature_of_what_it_injects() {
        // Arrange
        CellCoordinate anyCompletion = Any.CellCoordinate().Generate();
        double         anyRate       = Any.Double().GreaterThan(0.0).LessThanOrEqualTo(Well.MaximumRateCubicMetresPerSecond).Generate();

        // Act
        Exception? error = Record.Exception(() => new Well("INJ-01", WellKind.Injector, anyCompletion, anyRate, null));

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(error);
        Assert.Equal("injectionTemperature", argumentError.ParamName);
    }

    [Fact]
    public void A_producer_has_no_injection_temperature() {
        // Arrange
        CellCoordinate anyCompletion           = Any.CellCoordinate().Generate();
        double         anyRate                 = Any.Double().GreaterThan(0.0).LessThanOrEqualTo(Well.MaximumRateCubicMetresPerSecond).Generate();
        Temperature    anyInjectionTemperature = Any.Temperature().Generate();

        // Act
        Exception? error = Record.Exception(() => new Well("PRD-01", WellKind.Producer, anyCompletion, anyRate, anyInjectionTemperature));

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(error);
        Assert.Equal("injectionTemperature", argumentError.ParamName);
    }

}
