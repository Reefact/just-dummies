using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Discovery;

public sealed class OrbitalStateTests {

    [Fact]
    public void An_orbital_state_keeps_what_it_is_built_from() {
        // Arrange
        Epoch              anyEpoch   = Any.Epoch().Generate();
        Vector3            position   = new Vector3(7000, 0, 0);
        Vector3            velocity   = new Vector3(0, 7.5, 0);
        PositionCovariance covariance = Any.PositionCovariance().Generate();

        // Act
        OrbitalState state = new OrbitalState(anyEpoch, position, velocity, covariance);

        // Assert
        Assert.Same(anyEpoch, state.Epoch);
        Assert.Same(position, state.PositionKm);
        Assert.Same(velocity, state.VelocityKmPerSecond);
        Assert.Same(covariance, state.Covariance);
    }

    [Fact]
    public void The_altitude_is_the_distance_to_the_centre_minus_the_earth_radius() {
        // Arrange
        OrbitalState state = Any.OrbitalState().WithPositionKm(new Vector3(0, 7000, 0)).Generate();

        // Act / Assert
        Assert.Equal(621.863, state.AltitudeKm, 6);
    }

    [Fact]
    public void An_orbital_state_refuses_a_missing_epoch() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.OrbitalState().WithEpoch((Epoch)null!).Generate());

        // Assert
        Assert.Equal("epoch", refusal.ParamName);
    }

    [Fact]
    public void An_orbital_state_refuses_a_missing_position() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.OrbitalState().WithPositionKm((Vector3)null!).Generate());

        // Assert
        Assert.Equal("positionKm", refusal.ParamName);
    }

    [Fact]
    public void An_orbital_state_refuses_a_missing_velocity() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.OrbitalState().WithVelocityKmPerSecond((Vector3)null!).Generate());

        // Assert
        Assert.Equal("velocityKmPerSecond", refusal.ParamName);
    }

    [Fact]
    public void An_orbital_state_refuses_a_missing_covariance() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.OrbitalState().WithCovariance((PositionCovariance)null!).Generate());

        // Assert
        Assert.Equal("covariance", refusal.ParamName);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(6378.0, 0, 0)]
    [InlineData(6378.137, 0, 0)]
    [InlineData(0, -3000, 4000)]
    [InlineData(106_379.0, 0, 0)]
    [InlineData(0, 0, -200_000)]
    public void An_orbital_state_refuses_a_position_on_or_below_the_surface_or_above_the_altitude_ceiling(double x, double y, double z) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.OrbitalState().WithPositionKm(new Vector3(x, y, z)).Generate());

        // Assert
        Assert.Equal("positionKm", refusal.ParamName);
    }

    [Theory]
    [InlineData(6378.2, 0, 0)]
    [InlineData(0, 106_378.0, 0)]
    [InlineData(-3000, -4000, 5000)]
    public void An_orbital_state_accepts_a_position_between_the_surface_and_the_ceiling(double x, double y, double z) {
        // Act
        OrbitalState state = Any.OrbitalState().WithPositionKm(new Vector3(x, y, z)).Generate();

        // Assert
        Assert.Equal(new Vector3(x, y, z), state.PositionKm);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(12.001, 0, 0)]
    [InlineData(0, 8, 9)]
    [InlineData(-13, 0, 0)]
    public void An_orbital_state_refuses_a_speed_of_zero_or_above_twelve_kilometres_per_second(double x, double y, double z) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.OrbitalState().WithVelocityKmPerSecond(new Vector3(x, y, z)).Generate());

        // Assert
        Assert.Equal("velocityKmPerSecond", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.001, 0, 0)]
    [InlineData(12, 0, 0)]
    [InlineData(0, -3, 4)]
    public void An_orbital_state_accepts_a_speed_up_to_twelve_kilometres_per_second(double x, double y, double z) {
        // Act
        OrbitalState state = Any.OrbitalState().WithVelocityKmPerSecond(new Vector3(x, y, z)).Generate();

        // Assert
        Assert.Equal(new Vector3(x, y, z), state.VelocityKmPerSecond);
    }

}
