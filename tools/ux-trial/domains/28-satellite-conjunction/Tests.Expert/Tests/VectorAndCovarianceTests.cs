using JustDummies;
using SatelliteConjunction.Domain;
using SatelliteConjunction.Tests.Expert.Arbitrary;

namespace SatelliteConjunction.Tests.Expert.Tests;

public sealed class VectorAndCovarianceTests {

    [Fact]
    public void The_zero_vector_has_no_direction() {
        // Arrange
        Vector3 zero = Vector3.Zero;

        // Act
        Func<Vector3> normalizing = zero.Normalized;

        // Assert
        Assert.Throws<InvalidOperationException>(normalizing);
    }

    [Fact]
    public void A_covariance_with_admissible_correlations_but_no_positive_determinant_is_refused() {
        // Arrange: each pairwise correlation is 0.9 in magnitude, but the three together are inconsistent.

        // Act
        Action creating = () => new PositionCovariance(1, 1, 1, 0.9, 0.9, -0.9);

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(creating);
        Assert.Contains("positive definite", refusal.Message);
    }

    [Fact]
    public void A_correlation_of_one_is_refused() {
        // Arrange: a 2 km² cross term between two 2 km² variances is a perfect correlation.

        // Act
        Action creating = () => new PositionCovariance(2, 2, 1, 2, 0, 0);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("xy", creating);
    }

    [Fact]
    public void Combining_two_independent_uncertainties_adds_their_terms() {
        // Arrange
        PositionCovariance primary   = new(1, 2, 3, 0.1, 0.2, 0.3);
        PositionCovariance secondary = new(4, 5, 6, 0.4, 0.5, 0.6);

        // Act
        PositionCovariance combined = primary.Combine(secondary);

        // Assert
        Assert.Equal([5, 7, 9, 0.5, 0.7, 0.9],
                     [combined.Xx, combined.Yy, combined.Zz, combined.Xy, combined.Xz, combined.Yz],
                     new ToleranceComparer(1e-12));
    }

    [Fact]
    public void Projecting_on_an_axis_gives_the_variance_along_it() {
        // Arrange: the cross terms are dummies: projecting on x alone never reads them.
        PositionCovariance anyCovarianceWithXx4 = Any.PositionCovariance().WithXx(4).Generate();
        Vector3            alongX               = new(1, 0, 0);

        // Act
        double variance = anyCovarianceWithXx4.Project(alongX, alongX);

        // Assert
        Assert.Equal(4, variance, precision: 12);
    }

    private sealed class ToleranceComparer(double tolerance) : IEqualityComparer<double> {

        public bool Equals(double x, double y) {
            return Math.Abs(x - y) <= tolerance;
        }

        public int GetHashCode(double obj) {
            return 0;
        }

    }

}
