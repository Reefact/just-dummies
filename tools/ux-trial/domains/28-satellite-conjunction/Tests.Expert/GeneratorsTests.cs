using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Domain.Tests.Expert;

// The generators are test infrastructure with rules of their own: a dummy that the domain refuses would fail
// every test that uses it, far from its cause. These tests draw each generator many times so that a broken base
// is reported here, by name, rather than inside some unrelated test.
public sealed class GeneratorsTests {

    private const int Draws = 300;

    [Fact]
    public void Every_epoch_is_a_valid_one() {
        for (int draw = 0; draw < Draws; draw++) { Any.Epoch().Generate(); }
    }

    [Fact]
    public void Every_covariance_is_positive_definite() {
        for (int draw = 0; draw < Draws; draw++) { Any.PositionCovariance().Generate(); }
    }

    [Fact]
    public void Every_space_object_is_valid_whatever_the_kind_drawn() {
        for (int draw = 0; draw < Draws; draw++) { Any.SpaceObject().Generate(); }
    }

    [Fact]
    public void Every_orbital_state_is_valid() {
        for (int draw = 0; draw < Draws; draw++) { Any.OrbitalState().Generate(); }
    }

    [Fact]
    public void Every_trajectory_is_valid() {
        for (int draw = 0; draw < Draws; draw++) { Any.Trajectory().Generate(); }
    }

    [Fact]
    public void Every_conjunction_has_an_active_primary_a_different_secondary_and_both_cover_the_time_of_closest_approach() {
        for (int draw = 0; draw < Draws; draw++) {
            Conjunction anyConjunction = Any.Conjunction().Generate();

            Assert.True(anyConjunction.Primary.SpaceObject.IsActive);
            Assert.NotEqual(anyConjunction.Primary.SpaceObject.CatalogNumber, anyConjunction.Secondary.SpaceObject.CatalogNumber);
            Assert.True(anyConjunction.Primary.Covers(anyConjunction.TimeOfClosestApproach));
            Assert.True(anyConjunction.Secondary.Covers(anyConjunction.TimeOfClosestApproach));
        }
    }

    [Fact]
    public void Every_risk_assessment_is_valid() {
        for (int draw = 0; draw < Draws; draw++) { Any.RiskAssessment().Generate(); }
    }

}
