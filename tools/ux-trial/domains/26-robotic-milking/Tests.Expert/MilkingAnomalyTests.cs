using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Expert;

public sealed class MilkingAnomalyTests {

    public static TheoryData<AnomalyKind> QuarterLevelKinds() {
        return new TheoryData<AnomalyKind> { AnomalyKind.CupKickOff, AnomalyKind.HighConductivity, AnomalyKind.BloodInMilk };
    }

    public static TheoryData<AnomalyKind> SessionLevelKinds() {
        return new TheoryData<AnomalyKind> { AnomalyKind.AnimalAgitation, AnomalyKind.VacuumLoss, AnomalyKind.EmergencyStop };
    }

    [Theory]
    [MemberData(nameof(QuarterLevelKinds))]
    public void A_quarter_level_anomaly_keeps_the_quarter_it_concerns(AnomalyKind kind) {
        // Act
        MilkingAnomaly anomaly = Any.MilkingAnomaly().WithKind(kind).WithQuarter(UdderQuarter.LeftRear).Generate();

        // Assert
        Assert.Equal(kind, anomaly.Kind);
        Assert.Equal(UdderQuarter.LeftRear, anomaly.Quarter);
        Assert.True(MilkingAnomaly.IsQuarterLevel(kind));
    }

    [Theory]
    [MemberData(nameof(SessionLevelKinds))]
    public void A_session_level_anomaly_concerns_no_quarter(AnomalyKind kind) {
        // Act
        MilkingAnomaly anomaly = Any.MilkingAnomaly().WithKind(kind).Generate();

        // Assert
        Assert.Null(anomaly.Quarter);
        Assert.False(MilkingAnomaly.IsQuarterLevel(kind));
    }

    [Theory]
    [MemberData(nameof(QuarterLevelKinds))]
    public void A_quarter_level_anomaly_without_a_quarter_is_refused(AnomalyKind kind) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.MilkingAnomaly().WithKind(kind).WithoutQuarter().Generate());

        // Assert
        Assert.Equal("quarter", refusal.ParamName);
        Assert.Contains(kind.ToString(), refusal.Message);
    }

    [Theory]
    [MemberData(nameof(SessionLevelKinds))]
    public void A_session_level_anomaly_naming_a_quarter_is_refused(AnomalyKind kind) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.MilkingAnomaly().WithKind(kind).WithQuarter(quarter => quarter).Generate());

        // Assert
        Assert.Equal("quarter", refusal.ParamName);
        Assert.Contains(kind.ToString(), refusal.Message);
    }

    [Theory]
    [InlineData(AnomalyKind.CupKickOff, false, false)]
    [InlineData(AnomalyKind.HighConductivity, false, true)]
    [InlineData(AnomalyKind.BloodInMilk, false, true)]
    [InlineData(AnomalyKind.AnimalAgitation, true, false)]
    [InlineData(AnomalyKind.VacuumLoss, true, false)]
    [InlineData(AnomalyKind.EmergencyStop, true, false)]
    public void Only_the_whole_session_kinds_are_critical_and_only_the_milk_quality_kinds_divert(AnomalyKind kind, bool isCritical, bool divertsMilk) {
        // Act
        MilkingAnomaly anomaly = Any.MilkingAnomaly().WithKind(kind).Generate();

        // Assert
        Assert.Equal(isCritical, anomaly.IsCritical);
        Assert.Equal(divertsMilk, anomaly.DivertsMilk);
    }

    [Fact]
    public void An_anomaly_at_the_very_start_of_the_session_is_accepted() {
        // Act
        MilkingAnomaly anomaly = Any.MilkingAnomaly().WithElapsed(TimeSpan.Zero).Generate();

        // Assert
        Assert.Equal(TimeSpan.Zero, anomaly.Elapsed);
    }

    [Fact]
    public void An_anomaly_before_the_start_of_the_session_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.MilkingAnomaly().WithElapsed(TimeSpan.FromTicks(-1)).Generate());

        // Assert
        Assert.Equal("elapsed", refusal.ParamName);
    }

    [Fact]
    public void A_kind_the_robot_does_not_know_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.MilkingAnomaly().WithKind((AnomalyKind)99).Generate());

        // Assert
        Assert.Equal("kind", refusal.ParamName);
    }

    [Fact]
    public void A_quarter_the_udder_does_not_have_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.MilkingAnomaly().WithKind(AnomalyKind.CupKickOff).WithQuarter((UdderQuarter?)42).Generate());

        // Assert
        Assert.Equal("quarter", refusal.ParamName);
    }

}
