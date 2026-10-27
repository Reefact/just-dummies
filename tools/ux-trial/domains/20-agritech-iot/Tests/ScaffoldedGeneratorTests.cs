using AgriTech.Domain;
using JustDummies;
using Xunit;

namespace AgriTech.Tests;

/// <summary>Harness: each scaffolded generator, as resolved by hand, must never trip its type's constructor.</summary>
public sealed class ScaffoldedGeneratorTests {

    private const int Draws = 500;

    private static void DrawRepeatedly<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            generator.Generate();
        }
    }

    [Fact]
    public void Scaffolded_AnyGeoCoordinate_always_builds_a_valid_GeoCoordinate() => DrawRepeatedly(new AnyGeoCoordinate());

    [Fact]
    public void Scaffolded_AnyRelativeHumidity_always_builds_a_valid_RelativeHumidity() => DrawRepeatedly(new AnyRelativeHumidity());

    [Fact]
    public void Scaffolded_AnySensorId_always_builds_a_valid_SensorId() => DrawRepeatedly(new AnySensorId());

    [Fact]
    public void Scaffolded_AnyBatteryVoltage_always_builds_a_valid_BatteryVoltage() => DrawRepeatedly(new AnyBatteryVoltage());

    [Fact]
    public void Scaffolded_AnySensorReading_always_builds_a_valid_SensorReading() => DrawRepeatedly(new AnySensorReading());

    [Fact]
    public void Scaffolded_AnyGeofence_always_builds_a_valid_Geofence() => DrawRepeatedly(new AnyGeofence());

    [Fact]
    public void Scaffolded_AnyReadingBatch_always_builds_a_valid_ReadingBatch() => DrawRepeatedly(new AnyReadingBatch());

    [Fact]
    public void Scaffolded_AnyIrrigationPolicy_always_builds_a_valid_IrrigationPolicy() => DrawRepeatedly(new AnyIrrigationPolicy());

}
