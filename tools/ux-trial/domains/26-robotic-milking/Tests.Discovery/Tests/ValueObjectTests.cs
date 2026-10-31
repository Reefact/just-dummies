using JustDummies;
using JustDummies.Xunit;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Discovery.Tests;

[Reproducible]
public sealed class ValueObjectTests {

    [Fact]
    public void An_animal_id_exposes_its_country_code() {
        // Arrange
        string anyDigits = Any.StringMatching("[0-9]{12}").Generate();

        // Act
        AnimalId id = new AnimalId("FR" + anyDigits);

        // Assert
        Assert.Equal("FR", id.CountryCode);
    }

    [Fact]
    public void An_animal_id_with_a_lower_case_country_code_is_rejected() {
        // Arrange
        string anyDigits = Any.StringMatching("[0-9]{12}").Generate();

        // Act
        Action act = () => new AnimalId("fr" + anyDigits);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Settings_that_stimulate_the_cow_for_less_than_30_seconds_are_rejected() {
        // Arrange
        AnyMilkingSettings settings = Any.MilkingSettings()
                                         .WithCleaningTime(TimeSpan.FromSeconds(5))
                                         .WithLetDownDelay(TimeSpan.FromSeconds(9));

        // Act
        Action act = () => settings.Generate();

        // Assert
        ArgumentException error = Assert.Throws<ArgumentException>(act);
        Assert.Equal("letDownDelay", error.ParamName);
    }

    [Fact]
    public void Settings_that_stimulate_the_cow_for_exactly_30_seconds_are_accepted() {
        // Arrange
        AnyMilkingSettings settings = Any.MilkingSettings()
                                         .WithCleaningTime(TimeSpan.FromSeconds(5))
                                         .WithLetDownDelay(TimeSpan.FromSeconds(10));

        // Act
        MilkingSettings accepted = settings.Generate();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(5), accepted.CleaningTime);
    }

    [Fact]
    public void A_reading_above_the_conductivity_threshold_suggests_mastitis() {
        // Arrange
        AnyMilkFlowReading reading = Any.MilkFlowReading().WithConductivityMsPerCm(7.6m);

        // Act
        bool suggestsMastitis = reading.Generate().SuggestsMastitis;

        // Assert
        Assert.True(suggestsMastitis);
    }

    [Fact]
    public void A_cup_kick_off_must_name_the_quarter_it_concerns() {
        // Arrange
        TimeSpan anyElapsed = Any.TimeSpan().GreaterThanOrEqualTo(TimeSpan.Zero).LessThanOrEqualTo(TimeSpan.FromHours(1)).Generate();

        // Act
        Action act = () => new MilkingAnomaly(AnomalyKind.CupKickOff, null, anyElapsed);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void An_emergency_stop_is_critical_and_diverts_no_milk() {
        // Arrange
        AnyMilkingAnomaly anomaly = Any.MilkingAnomaly().WithKind(AnomalyKind.EmergencyStop);

        // Act
        MilkingAnomaly emergencyStop = anomaly.Generate();

        // Assert
        Assert.True(emergencyStop.IsCritical);
        Assert.False(emergencyStop.DivertsMilk);
        Assert.Null(emergencyStop.Quarter);
    }

}
