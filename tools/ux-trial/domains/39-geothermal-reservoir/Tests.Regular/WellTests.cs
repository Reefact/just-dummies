using GeothermalReservoir.Domain;

namespace GeothermalReservoir.Tests.Regular;

public sealed class WellTests {

    [Fact]
    public void An_injector_adds_its_rate_to_the_reservoir() {
        // Arrange
        Well anyInjector = Arbitrary.Well().WithKind(WellKind.Injector).WithRateCubicMetresPerSecond(0.05).Generate();

        // Act
        double signedRate = anyInjector.SignedRate;

        // Assert
        Assert.Equal(0.05, signedRate);
    }

    [Fact]
    public void A_producer_takes_its_rate_out_of_the_reservoir() {
        // Arrange
        Well anyProducer = Arbitrary.Well().WithKind(WellKind.Producer).WithRateCubicMetresPerSecond(0.05).Generate();

        // Act
        double signedRate = anyProducer.SignedRate;

        // Assert
        Assert.Equal(-0.05, signedRate);
    }

    [Fact]
    public void An_injector_must_state_the_temperature_of_what_it_injects() {
        // Arrange
        AnyWell injectorWithoutTemperature = Arbitrary.Well().WithKind(WellKind.Injector).WithInjectionTemperature((Temperature?)null);

        // Act
        Action build = () => injectorWithoutTemperature.Generate();

        // Assert
        Assert.Throws<ArgumentException>(build);
    }

    [Fact]
    public void A_producer_has_no_injection_temperature() {
        // Arrange
        CellCoordinate anyCompletion  = Arbitrary.CellCoordinate().Generate();
        double         anyRate        = Any.Double().GreaterThan(0.0).LessThanOrEqualTo(Well.MaximumRateCubicMetresPerSecond).Generate();
        Temperature    anyTemperature = Arbitrary.Temperature().Generate();

        // Act
        Action build = () => new Well("PRD-01", WellKind.Producer, anyCompletion, anyRate, anyTemperature);

        // Assert
        Assert.Throws<ArgumentException>(build);
    }

    [Fact]
    public void A_well_id_whose_prefix_contradicts_the_kind_is_refused() {
        // Arrange
        CellCoordinate anyCompletion = Arbitrary.CellCoordinate().Generate();
        double         anyRate       = Any.Double().GreaterThan(0.0).LessThanOrEqualTo(Well.MaximumRateCubicMetresPerSecond).Generate();

        // Act
        Action build = () => new Well("INJ-07", WellKind.Producer, anyCompletion, anyRate, null);

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(build);
        Assert.Equal("id", refusal.ParamName);
    }

    [Fact]
    public void A_well_id_not_shaped_like_INJ_or_PRD_and_two_digits_is_a_format_error() {
        // Arrange
        CellCoordinate anyCompletion = Arbitrary.CellCoordinate().Generate();
        double         anyRate       = Any.Double().GreaterThan(0.0).LessThanOrEqualTo(Well.MaximumRateCubicMetresPerSecond).Generate();

        // Act
        Action build = () => new Well("PRD-7", WellKind.Producer, anyCompletion, anyRate, null);

        // Assert
        Assert.Throws<FormatException>(build);
    }

}
