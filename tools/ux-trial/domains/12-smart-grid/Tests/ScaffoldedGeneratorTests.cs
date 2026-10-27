using JustDummies;
using JustDummies.Xunit;

using SmartGrid.Domain;

using Xunit;

namespace SmartGrid.Tests;

/// <summary>Harness checks: each scaffolded (then hand-resolved) generator builds a valid value on every draw.</summary>
public sealed class ScaffoldedGeneratorTests {

    private const int Draws = 500;

    private static void DrawRepeatedly<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            Assert.NotNull(generator.Generate());
        }
    }

    [Fact, Reproducible]
    public void Scaffolded_AnyDeliveryPointId_always_builds_a_valid_DeliveryPointId() {
        DrawRepeatedly(new AnyDeliveryPointId());
    }

    [Fact, Reproducible]
    public void Scaffolded_AnyMeterIndex_always_builds_a_valid_MeterIndex() {
        DrawRepeatedly(new AnyMeterIndex());
    }

    [Fact, Reproducible]
    public void Scaffolded_AnyMeterReading_always_builds_a_valid_MeterReading() {
        DrawRepeatedly(new AnyMeterReading());
    }

    [Fact, Reproducible]
    public void Scaffolded_AnyLoadCurvePoint_always_builds_a_valid_LoadCurvePoint() {
        DrawRepeatedly(new AnyLoadCurvePoint());
    }

    [Fact, Reproducible]
    public void Scaffolded_AnyOffPeakWindow_always_builds_a_valid_OffPeakWindow() {
        DrawRepeatedly(new AnyOffPeakWindow());
    }

    [Fact, Reproducible]
    public void Scaffolded_AnyDailyLoadCurve_always_builds_a_valid_DailyLoadCurve() {
        DrawRepeatedly(new AnyDailyLoadCurve());
    }

    [Fact, Reproducible]
    public void Scaffolded_AnyConsumptionPeriod_always_builds_a_valid_ConsumptionPeriod() {
        DrawRepeatedly(new AnyConsumptionPeriod());
    }

    [Fact, Reproducible]
    public void Scaffolded_AnyMeter_always_builds_a_valid_Meter() {
        DrawRepeatedly(Any.Meter());
    }

}
