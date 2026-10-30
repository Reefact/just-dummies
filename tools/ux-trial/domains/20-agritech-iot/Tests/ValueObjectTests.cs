using AgriTech.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace AgriTech.Tests;

[Reproducible]
public sealed class ValueObjectTests {

    private static readonly AnyDouble AnyLatitude = Any.Double().Between(-90.0, 90.0);

    [Fact]
    public void Longitude_180_is_refused_because_it_is_written_minus_180() {
        double anyLatitude = AnyLatitude.Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(anyLatitude, 180.0));
    }

    [Fact]
    public void Longitude_minus_180_is_accepted() {
        double anyLatitude = AnyLatitude.Generate();

        GeoCoordinate antimeridian = new GeoCoordinate(anyLatitude, -180.0);

        Assert.Equal(-180.0, antimeridian.Longitude);
    }

    [Theory]
    [InlineData(-90.000001)]
    [InlineData(90.000001)]
    public void Latitude_beyond_the_poles_is_refused(double latitude) {
        double anyLongitude = Any.Double().GreaterThanOrEqualTo(-180.0).LessThan(180.0).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(latitude, anyLongitude));
    }

    [Fact]
    public void A_geofence_contains_its_own_corners() {
        GeoCoordinate southWest = new GeoCoordinate(48.80, 2.20);
        GeoCoordinate northEast = new GeoCoordinate(48.90, 2.40);
        Geofence fence = new Geofence(southWest, northEast);

        Assert.True(fence.Contains(southWest));
        Assert.True(fence.Contains(northEast));
    }

    [Fact]
    public void A_reading_time_stamped_off_UTC_is_refused() {
        // Arrange
        AnySensorReading anyReading = new AnySensorReading();
        DateTimeOffset anyLocalInstant = Any.DateTimeOffset().WithOffset(TimeSpan.FromHours(2)).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => anyReading.WithTakenAt(anyLocalInstant).Generate());
    }

    [Fact]
    public void A_battery_at_3_volts_is_low() {
        BatteryVoltage battery = new BatteryVoltage((Half)3.0f);

        Assert.True(battery.IsLow);
    }

    [Fact]
    public void A_lower_case_sensor_id_is_refused() {
        Assert.Throws<ArgumentException>(() => new SensorId("AG-00a1f3"));
    }

    [Fact]
    public void Humidity_above_100_percent_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RelativeHumidity(100.1));
    }

}
