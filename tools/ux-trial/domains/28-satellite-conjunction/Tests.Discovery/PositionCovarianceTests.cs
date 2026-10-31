using System;

using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Discovery;

public sealed class PositionCovarianceTests {

    [Fact]
    public void A_cross_term_giving_a_perfect_correlation_is_refused() {
        // Arrange
        // xx = 4 and yy = 9: a cross term of 6 is a correlation of exactly 1. zz plays no part.
        double anyZz = Any.Double().Positive().LessThanOrEqualTo(PositionCovariance.MaximumSigmaKm * PositionCovariance.MaximumSigmaKm).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new PositionCovariance(4, 9, anyZz, 6, 0, 0));

        // Assert
        Assert.Equal("xy", refusal.ParamName);
    }

    [Fact]
    public void A_matrix_that_is_not_positive_definite_is_refused() {
        // Arrange
        // Each pair is correlated at 0.6 or -0.6, which is allowed alone, but together the determinant is negative.
        double xx = 1, yy = 1, zz = 1, xy = 0.6, xz = 0.6, yz = -0.6;

        // Act
        Action creation = () => _ = new PositionCovariance(xx, yy, zz, xy, xz, yz);

        // Assert
        Assert.Throws<ArgumentException>(creation);
    }

    [Fact]
    public void Combining_two_independent_covariances_sums_their_terms() {
        // Arrange
        PositionCovariance primary   = new(1, 2, 3, 0.5, 0, 0);
        PositionCovariance secondary = new(4, 5, 6, 1, 0.5, 0.25);

        // Act
        PositionCovariance combined = primary.Combine(secondary);

        // Assert
        Assert.Equal((5d, 7d, 9d, 1.5d, 0.5d, 0.25d), (combined.Xx, combined.Yy, combined.Zz, combined.Xy, combined.Xz, combined.Yz));
    }

    [Fact]
    public void The_mean_sigma_is_the_root_of_the_mean_variance() {
        // Arrange
        PositionCovariance covariance = new(1, 4, 7, 0, 0, 0);

        // Act
        double meanSigma = covariance.MeanSigmaKm;

        // Assert
        Assert.Equal(2, meanSigma, precision: 12);
    }

}
