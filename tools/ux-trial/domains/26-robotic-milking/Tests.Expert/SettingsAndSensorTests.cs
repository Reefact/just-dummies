namespace RoboticMilking.Tests.Expert;

public sealed class SettingsAndSensorTests {

    [Fact]
    public void Settings_that_stimulate_the_cow_for_less_than_30_seconds_are_refused() {
        // Arrange: 4 x 5 s of brushing plus a 9 s delay is 29 s of stimulation.
        AnyMilkingSettings anyTooShortStimulation = Any.MilkingSettings()
                                                       .WithCleaningTime(TimeSpan.FromSeconds(5))
                                                       .WithLetDownDelay(TimeSpan.FromSeconds(9));

        // Act
        Exception? refusal = Record.Exception(() => anyTooShortStimulation.Generate());

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("letDownDelay", argumentError.ParamName);
    }

    [Fact]
    public void Brushing_alone_can_provide_the_whole_let_down_stimulation() {
        // Arrange: 4 x 8 s of brushing is 32 s, so no delay is needed.
        AnyMilkingSettings anyNoDelaySettings = Any.MilkingSettings()
                                                   .WithCleaningTime(TimeSpan.FromSeconds(8))
                                                   .WithLetDownDelay(TimeSpan.Zero);

        // Act
        MilkingSettings settings = anyNoDelaySettings.Generate();

        // Assert
        Assert.Equal(TimeSpan.Zero, settings.LetDownDelay);
    }

    [Fact]
    public void A_conductivity_above_7_5_mS_per_cm_suggests_mastitis() {
        // Arrange
        MilkFlowReading anyReading = Any.MilkFlowReading().WithConductivityMsPerCm(7.6m).Generate();

        // Act
        bool flagged = anyReading.SuggestsMastitis;

        // Assert
        Assert.True(flagged);
    }

    [Fact]
    public void A_conductivity_of_7_5_mS_per_cm_does_not_suggest_mastitis() {
        // Arrange
        MilkFlowReading anyReading = Any.MilkFlowReading().WithConductivityMsPerCm(7.5m).Generate();

        // Act
        bool flagged = anyReading.SuggestsMastitis;

        // Assert
        Assert.False(flagged);
    }

    [Theory]
    [InlineData(AnomalyKind.CupKickOff)]
    [InlineData(AnomalyKind.HighConductivity)]
    [InlineData(AnomalyKind.BloodInMilk)]
    public void A_quarter_level_anomaly_must_name_its_quarter(AnomalyKind kind) {
        // Arrange
        TimeSpan anyElapsed = Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromMinutes(20)).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new MilkingAnomaly(kind, quarter: null, anyElapsed));

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("quarter", argumentError.ParamName);
    }

    [Theory]
    [InlineData(AnomalyKind.AnimalAgitation)]
    [InlineData(AnomalyKind.VacuumLoss)]
    [InlineData(AnomalyKind.EmergencyStop)]
    public void A_session_level_anomaly_cannot_name_a_quarter(AnomalyKind kind) {
        // Arrange
        UdderQuarter anyQuarter = Any.Enum<UdderQuarter>().Generate();
        TimeSpan     anyElapsed = Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromMinutes(20)).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new MilkingAnomaly(kind, anyQuarter, anyElapsed));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

}
