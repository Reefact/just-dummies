using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Regular;

public sealed class OrbitalStateTests {

    [Fact]
    public void A_state_keeps_what_it_was_built_from() {
        // Arrange
        Epoch              anyEpoch      = Any.Epoch().Generate();
        Vector3            anyPosition   = Any.OrbitalState().Generate().PositionKm;
        Vector3            anyVelocity   = Any.OrbitalState().Generate().VelocityKmPerSecond;
        PositionCovariance anyCovariance = Any.PositionCovariance().Generate();

        // Act
        OrbitalState state = new(anyEpoch, anyPosition, anyVelocity, anyCovariance);

        // Assert
        Assert.Same(anyEpoch, state.Epoch);
        Assert.Same(anyPosition, state.PositionKm);
        Assert.Same(anyVelocity, state.VelocityKmPerSecond);
        Assert.Same(anyCovariance, state.Covariance);
    }

    [Fact]
    public void An_arbitrary_state_is_always_accepted() {
        // Arrange / Act
        OrbitalState anyState = Any.OrbitalState().Generate();

        // Assert
        Assert.True(anyState.AltitudeKm > 0);
    }

    [Fact]
    public void The_altitude_is_the_distance_to_the_centre_minus_the_earth_radius() {
        // Arrange
        Vector3 position = new(0, 0, 7_000);

        // Act
        OrbitalState state = Any.OrbitalState().WithPositionKm(position).Generate();

        // Assert
        Assert.Equal(7_000 - OrbitalState.EarthRadiusKm, state.AltitudeKm, 9);
    }

    [Fact]
    public void A_position_inside_the_earth_is_refused() {
        // Arrange
        double  anyDepth = Any.Double().Between(0.001, 6_378).Generate();
        Vector3 inside   = new(anyDepth, 0, 0);

        // Act
        Exception? refusal = Record.Exception(() => Any.OrbitalState().WithPositionKm(inside).Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void The_centre_of_the_earth_is_refused() {
        // Arrange / Act
        Exception? refusal = Record.Exception(() => Any.OrbitalState().WithPositionKm(Vector3.Zero).Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_position_on_the_surface_is_refused_but_a_metre_above_is_accepted() {
        // Arrange
        Vector3 onSurface    = new(OrbitalState.EarthRadiusKm, 0, 0);
        Vector3 metreAbove   = new(OrbitalState.EarthRadiusKm + 0.001, 0, 0);

        // Act
        Exception?   onSurfaceRefusal = Record.Exception(() => Any.OrbitalState().WithPositionKm(onSurface).Generate());
        OrbitalState above            = Any.OrbitalState().WithPositionKm(metreAbove).Generate();

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(onSurfaceRefusal);
        Assert.Equal(0.001, above.AltitudeKm, 9);
    }

    [Fact]
    public void A_position_beyond_the_altitude_cap_is_refused() {
        // Arrange
        double  anyExcess = Any.Double().Between(1, 1_000_000).Generate();
        Vector3 tooFar    = new(0, OrbitalState.EarthRadiusKm + OrbitalState.MaximumAltitudeKm + anyExcess, 0);

        // Act
        Exception? refusal = Record.Exception(() => Any.OrbitalState().WithPositionKm(tooFar).Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_position_well_inside_the_altitude_cap_is_accepted_whatever_its_direction() {
        // Arrange
        double  anyRadius    = Any.Double().Between(OrbitalState.EarthRadiusKm + 1, OrbitalState.EarthRadiusKm + OrbitalState.MaximumAltitudeKm - 1).Generate();
        Vector3 anyDirection = Any.Vector3().WithX(x => x.Between(0.1, 1)).WithY(y => y.Between(-1, 1)).WithZ(z => z.Between(-1, 1)).Generate().Normalized();
        Vector3 position     = anyDirection.Scale(anyRadius);

        // Act
        OrbitalState state = Any.OrbitalState().WithPositionKm(position).Generate();

        // Assert
        Assert.Equal(anyRadius, state.PositionKm.Magnitude, 6);
    }

    [Fact]
    public void A_standing_still_object_is_refused() {
        // Arrange / Act
        Exception? refusal = Record.Exception(() => Any.OrbitalState().WithVelocityKmPerSecond(Vector3.Zero).Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_speed_above_twelve_kilometres_per_second_is_refused() {
        // Arrange
        double  anyExcess = Any.Double().Between(12.001, 1_000).Generate();
        Vector3 tooFast   = new(0, 0, anyExcess);

        // Act
        Exception? refusal = Record.Exception(() => Any.OrbitalState().WithVelocityKmPerSecond(tooFast).Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_speed_of_exactly_twelve_kilometres_per_second_is_accepted() {
        // Arrange
        Vector3 limit = new(0, 0, OrbitalState.MaximumSpeedKmPerSecond);

        // Act
        OrbitalState state = Any.OrbitalState().WithVelocityKmPerSecond(limit).Generate();

        // Assert
        Assert.Same(limit, state.VelocityKmPerSecond);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void A_missing_part_is_refused(int missing) {
        // Arrange
        OrbitalState anyState = Any.OrbitalState().Generate();
        Epoch              epoch      = missing == 0 ? null! : anyState.Epoch;
        Vector3            position   = missing == 1 ? null! : anyState.PositionKm;
        Vector3            velocity   = missing == 2 ? null! : anyState.VelocityKmPerSecond;
        PositionCovariance covariance = missing == 3 ? null! : anyState.Covariance;

        // Act
        Exception? refusal = Record.Exception(() => new OrbitalState(epoch, position, velocity, covariance));

        // Assert
        Assert.IsType<ArgumentNullException>(refusal);
    }

}
