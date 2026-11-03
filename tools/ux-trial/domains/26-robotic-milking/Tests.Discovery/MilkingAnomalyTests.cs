using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Discovery;

public sealed class MilkingAnomalyTests {

    public static TheoryData<AnomalyKind> QuarterLevelKinds => new() { AnomalyKind.CupKickOff, AnomalyKind.HighConductivity, AnomalyKind.BloodInMilk };
    public static TheoryData<AnomalyKind> SessionLevelKinds => new() { AnomalyKind.AnimalAgitation, AnomalyKind.VacuumLoss, AnomalyKind.EmergencyStop };

    [Theory]
    [MemberData(nameof(QuarterLevelKinds))]
    public void A_quarter_level_anomaly_keeps_the_quarter_it_names(AnomalyKind kind) {
        // Act
        MilkingAnomaly anomaly = new MilkingAnomaly(kind, UdderQuarter.LeftRear, TimeSpan.FromSeconds(75));

        // Assert
        Assert.Equal(kind, anomaly.Kind);
        Assert.Equal(UdderQuarter.LeftRear, anomaly.Quarter);
        Assert.Equal(TimeSpan.FromSeconds(75), anomaly.Elapsed);
        Assert.True(MilkingAnomaly.IsQuarterLevel(kind));
    }

    [Theory]
    [MemberData(nameof(QuarterLevelKinds))]
    public void A_quarter_level_anomaly_without_a_quarter_is_refused(AnomalyKind kind) {
        // Arrange
        TimeSpan anyElapsed = Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromHours(1)).Generate();

        // Act / Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new MilkingAnomaly(kind, quarter: null, anyElapsed));

        Assert.Equal("quarter", refusal.ParamName);
    }

    [Theory]
    [MemberData(nameof(SessionLevelKinds))]
    public void A_session_level_anomaly_has_no_quarter(AnomalyKind kind) {
        // Act
        MilkingAnomaly anyAnomaly = Any.MilkingAnomaly().WithKind(kind).Generate();

        // Assert
        Assert.Equal(kind, anyAnomaly.Kind);
        Assert.Null(anyAnomaly.Quarter);
        Assert.False(MilkingAnomaly.IsQuarterLevel(kind));
    }

    [Theory]
    [MemberData(nameof(SessionLevelKinds))]
    public void A_session_level_anomaly_naming_a_quarter_is_refused(AnomalyKind kind) {
        // Arrange
        UdderQuarter anyQuarter = Any.Enum<UdderQuarter>().Generate();

        // Act / Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new MilkingAnomaly(kind, anyQuarter, TimeSpan.Zero));

        Assert.Equal("quarter", refusal.ParamName);
    }

    [Theory]
    [InlineData(AnomalyKind.AnimalAgitation, true)]
    [InlineData(AnomalyKind.VacuumLoss, true)]
    [InlineData(AnomalyKind.EmergencyStop, true)]
    [InlineData(AnomalyKind.CupKickOff, false)]
    [InlineData(AnomalyKind.HighConductivity, false)]
    [InlineData(AnomalyKind.BloodInMilk, false)]
    public void Only_the_session_level_anomalies_are_critical(AnomalyKind kind, bool critical) {
        MilkingAnomaly anyAnomaly = Any.MilkingAnomaly().WithKind(kind).Generate();

        Assert.Equal(critical, anyAnomaly.IsCritical);
    }

    [Theory]
    [InlineData(AnomalyKind.HighConductivity, true)]
    [InlineData(AnomalyKind.BloodInMilk, true)]
    [InlineData(AnomalyKind.CupKickOff, false)]
    [InlineData(AnomalyKind.AnimalAgitation, false)]
    [InlineData(AnomalyKind.VacuumLoss, false)]
    [InlineData(AnomalyKind.EmergencyStop, false)]
    public void Only_a_conductivity_or_blood_anomaly_diverts_the_milk(AnomalyKind kind, bool diverts) {
        MilkingAnomaly anyAnomaly = Any.MilkingAnomaly().WithKind(kind).Generate();

        Assert.Equal(diverts, anyAnomaly.DivertsMilk);
    }

    [Fact]
    public void An_anomaly_at_time_zero_is_accepted() {
        MilkingAnomaly anyAnomaly = Any.MilkingAnomaly().WithElapsed(TimeSpan.Zero).Generate();

        Assert.Equal(TimeSpan.Zero, anyAnomaly.Elapsed);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.001)]
    public void An_anomaly_reported_before_the_session_started_is_refused(double seconds) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.MilkingAnomaly().WithElapsed(TimeSpan.FromSeconds(seconds)).Generate());

        Assert.Equal("elapsed", refusal.ParamName);
    }

    [Fact]
    public void An_unknown_kind_is_refused() {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.MilkingAnomaly().WithKind((AnomalyKind)99).Generate());

        Assert.Equal("kind", refusal.ParamName);
    }

    [Fact]
    public void An_unknown_quarter_is_refused() {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MilkingAnomaly(AnomalyKind.BloodInMilk, (UdderQuarter)99, TimeSpan.Zero));

        Assert.Equal("quarter", refusal.ParamName);
    }

}
