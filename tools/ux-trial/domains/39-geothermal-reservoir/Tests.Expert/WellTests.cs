using GeothermalReservoir.Domain;
using GeothermalReservoir.Tests.Expert.Arbitrary;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Expert;

public sealed class WellTests {

    [Fact]
    public void An_injector_pushes_its_rate_into_the_reservoir() {
        // Arrange
        Well injector = Any.Well().Injector().WithRateCubicMetresPerSecond(0.03).Generate();

        // Act
        double signedRate = injector.SignedRate;

        // Assert
        Assert.Equal(0.03, signedRate);
    }

    [Fact]
    public void A_producer_draws_its_rate_out_of_the_reservoir() {
        // Arrange
        Well producer = Any.Well().Producer().WithRateCubicMetresPerSecond(0.03).Generate();

        // Act
        double signedRate = producer.SignedRate;

        // Assert
        Assert.Equal(-0.03, signedRate);
    }

    [Fact]
    public void A_well_named_as_an_injector_cannot_be_a_producer() {
        // Arrange
        CellCoordinate anyCompletion = Any.CellCoordinate().Generate();
        double         anyRate       = Any.Double().GreaterThan(0).LessThanOrEqualTo(Well.MaximumRateCubicMetresPerSecond).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Well("INJ-03", WellKind.Producer, anyCompletion, anyRate, null));

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("id", argumentException.ParamName);
    }

    [Fact]
    public void A_well_id_follows_the_field_naming_convention() {
        // Arrange
        CellCoordinate anyCompletion = Any.CellCoordinate().Generate();
        double         anyRate       = Any.Double().GreaterThan(0).LessThanOrEqualTo(Well.MaximumRateCubicMetresPerSecond).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Well("PROD-1", WellKind.Producer, anyCompletion, anyRate, null));

        // Assert
        Assert.IsType<FormatException>(refusal);
    }

    [Fact]
    public void An_injector_must_state_what_it_injects() {
        // Arrange
        CellCoordinate anyCompletion = Any.CellCoordinate().Generate();
        double         anyRate       = Any.Double().GreaterThan(0).LessThanOrEqualTo(Well.MaximumRateCubicMetresPerSecond).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Well("INJ-01", WellKind.Injector, anyCompletion, anyRate, null));

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("injectionTemperature", argumentException.ParamName);
    }

    [Fact]
    public void A_producer_injects_nothing_so_has_no_injection_temperature() {
        // Arrange
        CellCoordinate anyCompletion  = Any.CellCoordinate().Generate();
        double         anyRate        = Any.Double().GreaterThan(0).LessThanOrEqualTo(Well.MaximumRateCubicMetresPerSecond).Generate();
        Temperature    anyTemperature = Any.Temperature().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Well("PRD-01", WellKind.Producer, anyCompletion, anyRate, anyTemperature));

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("injectionTemperature", argumentException.ParamName);
    }

}
