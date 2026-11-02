using AgriTech.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests.Expert;

// These tests are about the generators of this project, not about the domain: each draws many times, because a
// generator's promise is a property ("every draw is a value the constructor accepts"), which one draw cannot show.
public sealed class GeneratorTests {

    private const int Draws = 300;

    [Fact]
    public void A_geofence_is_never_drawn_with_its_south_west_corner_north_or_east_of_the_north_east_one() {
        for (int draw = 0; draw < Draws; draw++) {
            Geofence fence = Any.Geofence().Generate();

            Assert.True(fence.SouthWest.Latitude <= fence.NorthEast.Latitude);
            Assert.True(fence.SouthWest.Longitude <= fence.NorthEast.Longitude);
        }
    }

    [Fact]
    public void A_geofence_pinned_on_its_south_west_corner_keeps_it_and_stays_valid() {
        // Arrange
        GeoCoordinate northPole = new GeoCoordinate(90.0, 100.0);

        for (int draw = 0; draw < Draws; draw++) {
            // Act
            Geofence fence = Any.Geofence().WithSouthWest(northPole).Generate();

            // Assert
            Assert.Same(northPole, fence.SouthWest);
            Assert.Equal(90.0, fence.NorthEast.Latitude);
            Assert.InRange(fence.NorthEast.Longitude, 100.0, 180.0);
        }
    }

    [Fact]
    public void A_sensor_id_is_never_drawn_malformed() {
        for (int draw = 0; draw < Draws; draw++) {
            SensorId sensorId = Any.SensorId().Generate();

            Assert.Equal(9, sensorId.Value.Length);
        }
    }

    [Fact]
    public void A_sensor_reading_is_never_drawn_with_a_non_utc_time_stamp() {
        for (int draw = 0; draw < Draws; draw++) {
            Assert.Equal(TimeSpan.Zero, Any.SensorReading().Generate().TakenAt.Offset);
        }
    }

    [Fact]
    public void A_battery_is_drawn_across_the_whole_valid_range_low_and_not_low() {
        bool sawLow    = false;
        bool sawNotLow = false;

        for (int draw = 0; draw < Draws; draw++) {
            BatteryVoltage battery = Any.BatteryVoltage().Generate();

            sawLow    |= battery.IsLow;
            sawNotLow |= !battery.IsLow;
        }

        Assert.True(sawLow);
        Assert.True(sawNotLow);
    }

    [Fact]
    public void A_reading_batch_is_drawn_small_by_default() {
        for (int draw = 0; draw < Draws; draw++) {
            Assert.InRange(Any.ReadingBatch().Generate().Readings.Count, 1, 20);
        }
    }

    [Fact]
    public void An_irrigation_policy_is_drawn_valid() {
        for (int draw = 0; draw < Draws; draw++) {
            IrrigationPolicy policy = Any.IrrigationPolicy().Generate();

            Assert.InRange(policy.DryThresholdPercent, 0.0, 100.0);
            Assert.True(policy.MinimumReadings > 0);
        }
    }

}
