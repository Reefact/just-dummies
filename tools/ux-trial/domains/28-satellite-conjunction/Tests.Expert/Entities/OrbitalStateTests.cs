using JustDummies;

using Xunit;

namespace SatelliteConjunction.Domain.Tests.Expert.Entities;

public sealed class OrbitalStateTests {

    private static readonly double CeilingRadiusKm = OrbitalState.EarthRadiusKm + OrbitalState.MaximumAltitudeKm;

    [Fact]
    public void A_state_exposes_what_it_was_built_from() {
        // Arrange
        Epoch              epoch      = Any.Epoch().Generate();
        Vector3            position   = new(7000, 100, -200);
        Vector3            velocity   = new(0, 7.5, 0.5);
        PositionCovariance covariance = Any.PositionCovariance().Generate();

        // Act
        OrbitalState state = new(epoch, position, velocity, covariance);

        // Assert
        Assert.Same(epoch, state.Epoch);
        Assert.Same(position, state.PositionKm);
        Assert.Same(velocity, state.VelocityKmPerSecond);
        Assert.Same(covariance, state.Covariance);
    }

    [Fact]
    public void The_altitude_is_the_distance_to_the_centre_of_the_earth_minus_its_radius() {
        // Arrange
        // 3-4-5 triangle scaled: the radius is exactly 7000 km whatever the axes involved.
        Vector3 position = new(4200, 5600, 0);

        // Act
        OrbitalState state = Any.OrbitalState().WithPositionKm(position).Generate();

        // Assert
        Assert.Equal(7000 - 6378.137, state.AltitudeKm, precision: 9);
    }

    [Fact]
    public void A_position_at_the_ceiling_altitude_is_accepted() {
        // Arrange
        Vector3 position = new(CeilingRadiusKm, 0, 0);

        // Act
        OrbitalState state = Any.OrbitalState().WithPositionKm(position).Generate();

        // Assert
        Assert.Equal(OrbitalState.MaximumAltitudeKm, state.AltitudeKm, precision: 6);
    }

    [Fact]
    public void A_position_just_above_the_ceiling_altitude_is_refused() {
        // Arrange
        Vector3 position = new(Math.BitIncrement(CeilingRadiusKm), 0, 0);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.OrbitalState().WithPositionKm(position).Generate());

        // Assert
        Assert.Equal("positionKm", refusal.ParamName);
    }

    [Fact]
    public void A_position_just_above_the_surface_is_accepted() {
        // Arrange
        Vector3 position = new(Math.BitIncrement(OrbitalState.EarthRadiusKm), 0, 0);

        // Act & Assert
        Assert.NotNull(Any.OrbitalState().WithPositionKm(position).Generate());
    }

    [Fact]
    public void A_position_on_the_surface_is_refused() {
        // Arrange
        Vector3 position = new(0, OrbitalState.EarthRadiusKm, 0);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.OrbitalState().WithPositionKm(position).Generate());
    }

    [Fact]
    public void A_position_at_the_centre_of_the_earth_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.OrbitalState().WithPositionKm(Vector3.Zero).Generate());
    }

    [Fact]
    public void The_direction_of_the_position_does_not_matter_only_its_distance_from_the_centre() {
        // Arrange
        Vector3 aboveTheNorthPole = new(0, 0, 7000);
        Vector3 aboveTheSouthPole = new(0, 0, -7000);

        // Act & Assert
        Assert.Equal(Any.OrbitalState().WithPositionKm(aboveTheNorthPole).Generate().AltitudeKm,
                     Any.OrbitalState().WithPositionKm(aboveTheSouthPole).Generate().AltitudeKm);
    }

    [Fact]
    public void The_fastest_speed_is_12_km_per_second_and_is_accepted() {
        // Arrange
        Vector3 velocity = new(12, 0, 0);

        // Act & Assert
        Assert.NotNull(Any.OrbitalState().WithVelocityKmPerSecond(velocity).Generate());
    }

    [Fact]
    public void A_speed_just_above_12_km_per_second_is_refused() {
        // Arrange
        Vector3 velocity = new(0, Math.BitIncrement(12), 0);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.OrbitalState().WithVelocityKmPerSecond(velocity).Generate());

        // Assert
        Assert.Equal("velocityKmPerSecond", refusal.ParamName);
    }

    [Fact]
    public void A_speed_is_the_magnitude_of_the_velocity_so_a_diagonal_one_is_checked_as_a_whole() {
        // Arrange
        // 7 + 7 + 7 on each axis: 12.12 km/s, though every component is far below 12.
        Vector3 velocity = new(7, 7, 7);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.OrbitalState().WithVelocityKmPerSecond(velocity).Generate());
    }

    [Fact]
    public void A_standing_still_velocity_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.OrbitalState().WithVelocityKmPerSecond(Vector3.Zero).Generate());

        // Assert
        Assert.Equal("velocityKmPerSecond", refusal.ParamName);
    }

    [Fact]
    public void A_very_slow_speed_is_accepted() {
        // Arrange
        // A metre per second.
        Vector3 velocity = new(1e-3, 0, 0);

        // Act & Assert
        Assert.NotNull(Any.OrbitalState().WithVelocityKmPerSecond(velocity).Generate());
    }

    [Fact]
    public void A_state_without_an_epoch_is_refused() {
        // Arrange
        Vector3            position   = new(7000, 0, 0);
        Vector3            velocity   = new(0, 7.5, 0);
        PositionCovariance covariance = Any.PositionCovariance().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OrbitalState(null!, position, velocity, covariance));
    }

    [Fact]
    public void A_state_without_a_position_is_refused() {
        // Arrange
        Epoch              epoch      = Any.Epoch().Generate();
        Vector3            velocity   = new(0, 7.5, 0);
        PositionCovariance covariance = Any.PositionCovariance().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OrbitalState(epoch, null!, velocity, covariance));
    }

    [Fact]
    public void A_state_without_a_velocity_is_refused() {
        // Arrange
        Epoch              epoch      = Any.Epoch().Generate();
        Vector3            position   = new(7000, 0, 0);
        PositionCovariance covariance = Any.PositionCovariance().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OrbitalState(epoch, position, null!, covariance));
    }

    [Fact]
    public void A_state_without_a_covariance_is_refused() {
        // Arrange
        Epoch   epoch    = Any.Epoch().Generate();
        Vector3 position = new(7000, 0, 0);
        Vector3 velocity = new(0, 7.5, 0);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OrbitalState(epoch, position, velocity, null!));
    }

}
