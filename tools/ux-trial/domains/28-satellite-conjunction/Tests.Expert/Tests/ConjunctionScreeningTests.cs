using JustDummies;
using SatelliteConjunction.Domain;
using SatelliteConjunction.Tests.Expert.Arbitrary;

namespace SatelliteConjunction.Tests.Expert.Tests;

/// <summary>What it takes for a close approach to be screened as a conjunction at all.</summary>
public sealed class ConjunctionScreeningTests {

    [Fact]
    public void An_inert_object_cannot_be_the_primary() {
        // Arrange
        Epoch      anyTca          = Any.Epoch().Generate();
        Trajectory anyInertPrimary = Any.Trajectory().WithSpaceObject(o => o.WithIsActive(false)).Covering(anyTca).Generate();
        Trajectory anySecondary    = Any.Trajectory().Covering(anyTca).Generate();

        // Act
        Action screening = () => new Conjunction(Any.Guid().NonEmpty().Generate(), anyInertPrimary, anySecondary, anyTca);

        // Assert
        Assert.Throws<ArgumentException>("primary", screening);
    }

    [Fact]
    public void An_object_cannot_be_in_conjunction_with_itself() {
        // Arrange
        Epoch      anyTca        = Any.Epoch().Generate();
        SpaceObject anySatellite = Any.SpaceObject().Active().Generate();
        Trajectory anyTrajectory      = Any.Trajectory().WithSpaceObject(anySatellite).Covering(anyTca).Generate();
        Trajectory anyOtherTrajectory = Any.Trajectory().WithSpaceObject(anySatellite).Covering(anyTca).Generate();

        // Act
        Action screening = () => new Conjunction(Any.Guid().NonEmpty().Generate(), anyTrajectory, anyOtherTrajectory, anyTca);

        // Assert
        Assert.Throws<ArgumentException>("secondary", screening);
    }

    [Fact]
    public void A_secondary_whose_ephemeris_ends_before_the_closest_approach_is_refused() {
        // Arrange
        Epoch      anyTca          = Any.Epoch().Generate();
        Trajectory anyPrimary      = Any.Trajectory().WithSpaceObject(o => o.Active()).Covering(anyTca).Generate();
        Trajectory anyStaleSecondary = Any.Trajectory().Covering(anyTca.Add(TimeSpan.FromHours(-6))).Generate();

        // Act
        Action screening = () => new Conjunction(Any.Guid().NonEmpty().Generate(), anyPrimary, anyStaleSecondary, anyTca);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("timeOfClosestApproach", screening);
    }

    [Fact]
    public void A_new_conjunction_is_screened_and_has_no_assessment_yet() {
        // Arrange

        // Act
        Conjunction anyConjunction = Any.Conjunction().Generate();

        // Assert
        Assert.Equal(ConjunctionStatus.Screened, anyConjunction.Status);
        Assert.Null(anyConjunction.LatestAssessment);
    }

}
