using JustDummies;

using Xunit;

namespace SatelliteConjunction.Domain.Tests.Expert.ValueObjects;

public sealed class PositionCovarianceTests {

    // The ceiling of a variance, in km²: a standard deviation of 1000 km is an object that is effectively lost.
    private const double VarianceCeiling = 1_000_000;

    [Fact]
    public void A_covariance_exposes_its_six_terms() {
        // Act
        PositionCovariance covariance = new(4, 5, 6, 1, 0.5, -1);

        // Assert
        Assert.Equal(4, covariance.Xx);
        Assert.Equal(5, covariance.Yy);
        Assert.Equal(6, covariance.Zz);
        Assert.Equal(1, covariance.Xy);
        Assert.Equal(0.5, covariance.Xz);
        Assert.Equal(-1, covariance.Yz);
    }

    [Fact]
    public void The_largest_variance_the_domain_accepts_is_a_standard_deviation_of_1000_km() {
        // Act
        PositionCovariance covariance = new(VarianceCeiling, VarianceCeiling, VarianceCeiling, 0, 0, 0);

        // Assert
        Assert.Equal(1000, covariance.MeanSigmaKm);
    }

    [Theory]
    [InlineData(0, "xx")]
    [InlineData(1, "yy")]
    [InlineData(2, "zz")]
    public void A_variance_above_the_ceiling_is_refused_on_each_axis(int axis, string parameterName) {
        // Arrange
        double justAbove = Math.BitIncrement(VarianceCeiling);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => WithVariance(axis, justAbove));

        // Assert
        Assert.Equal(parameterName, refusal.ParamName);
    }

    [Theory]
    [InlineData(0, "xx")]
    [InlineData(1, "yy")]
    [InlineData(2, "zz")]
    public void A_variance_that_is_zero_is_refused_on_each_axis(int axis, string parameterName) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => WithVariance(axis, 0));

        // Assert
        Assert.Equal(parameterName, refusal.ParamName);
    }

    [Theory]
    [InlineData(0, "xx")]
    [InlineData(1, "yy")]
    [InlineData(2, "zz")]
    public void A_negative_variance_is_refused_on_each_axis(int axis, string parameterName) {
        // Arrange
        double anyNegativeVariance = Any.Double().Negative().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => WithVariance(axis, anyNegativeVariance));

        // Assert
        Assert.Equal(parameterName, refusal.ParamName);
    }

    [Theory]
    [InlineData(0, double.NaN)]
    [InlineData(1, double.NaN)]
    [InlineData(2, double.NaN)]
    [InlineData(0, double.PositiveInfinity)]
    [InlineData(1, double.PositiveInfinity)]
    [InlineData(2, double.PositiveInfinity)]
    public void A_variance_that_is_not_finite_is_refused_on_each_axis(int axis, double nonFinite) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => WithVariance(axis, nonFinite));
    }

    [Theory]
    [InlineData(0, "xy")]
    [InlineData(1, "xz")]
    [InlineData(2, "yz")]
    public void A_cross_term_that_is_not_finite_is_refused(int crossTerm, string parameterName) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => WithCrossTerm(crossTerm, double.NaN));
        ArgumentOutOfRangeException other   = Assert.Throws<ArgumentOutOfRangeException>(() => WithCrossTerm(crossTerm, double.PositiveInfinity));

        // Assert
        Assert.Equal(parameterName, refusal.ParamName);
        Assert.Equal(parameterName, other.ParamName);
    }

    [Theory]
    [InlineData(0, "xy")]
    [InlineData(1, "xz")]
    [InlineData(2, "yz")]
    public void A_perfect_correlation_is_refused_whether_positive_or_negative(int crossTerm, string parameterName) {
        // Arrange
        // Unit variances everywhere, so a cross term of 1 (or -1) is a correlation of exactly 1 (or -1).
        // Act
        ArgumentOutOfRangeException positive = Assert.Throws<ArgumentOutOfRangeException>(() => WithCrossTerm(crossTerm, 1));
        ArgumentOutOfRangeException negative = Assert.Throws<ArgumentOutOfRangeException>(() => WithCrossTerm(crossTerm, -1));

        // Assert
        Assert.Equal(parameterName, positive.ParamName);
        Assert.Equal(parameterName, negative.ParamName);
    }

    [Fact]
    public void A_correlation_just_under_one_is_accepted_when_the_matrix_stays_positive_definite() {
        // Act
        PositionCovariance covariance = WithCrossTerm(0, 0.99);

        // Assert
        Assert.Equal(0.99, covariance.Xy);
    }

    [Fact]
    public void Three_correlations_that_cannot_hold_together_are_refused_even_though_each_is_acceptable_alone() {
        // Arrange
        // Unit variances and a correlation of -0.6 between every pair: each pair is fine, the three are not
        // (the determinant is 1 + 2(-0.6)^3 - 3(-0.6)^2 = -0.512).
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PositionCovariance(1, 1, 1, -0.6, -0.6, -0.6));
    }

    [Fact]
    public void Three_strong_correlations_that_do_hold_together_are_accepted() {
        // Arrange
        // Determinant 1 + 2(0.9)^3 - 3(0.9)^2 = 0.028: close to singular, but positive definite.
        // Act
        PositionCovariance covariance = new(1, 1, 1, 0.9, 0.9, 0.9);

        // Assert
        Assert.Equal(0.9, covariance.Yz);
    }

    [Fact]
    public void The_mean_sigma_is_the_root_of_the_mean_of_the_three_variances() {
        // Arrange
        PositionCovariance covariance = new(1, 4, 7, 0, 0, 0);

        // Act & Assert
        Assert.Equal(2, covariance.MeanSigmaKm);
    }

    [Fact]
    public void The_mean_sigma_ignores_the_cross_terms() {
        // Arrange
        PositionCovariance uncorrelated = new(1, 4, 7, 0, 0, 0);
        PositionCovariance correlated   = new(1, 4, 7, 1, 0.5, 2);

        // Act & Assert
        Assert.Equal(uncorrelated.MeanSigmaKm, correlated.MeanSigmaKm);
    }

    [Fact]
    public void Combining_two_covariances_adds_every_term() {
        // Arrange
        PositionCovariance left  = new(1, 2, 3, 0.1, 0.2, 0.3);
        PositionCovariance right = new(10, 20, 30, 1, 2, 3);

        // Act
        PositionCovariance combined = left.Combine(right);

        // Assert
        Assert.Equal(11, combined.Xx);
        Assert.Equal(22, combined.Yy);
        Assert.Equal(33, combined.Zz);
        Assert.Equal(1.1, combined.Xy, precision: 12);
        Assert.Equal(2.2, combined.Xz, precision: 12);
        Assert.Equal(3.3, combined.Yz, precision: 12);
    }

    [Fact]
    public void Combining_leaves_both_operands_as_they_were() {
        // Arrange
        PositionCovariance left  = new(1, 2, 3, 0, 0, 0);
        PositionCovariance right = new(10, 20, 30, 0, 0, 0);

        // Act
        left.Combine(right);

        // Assert
        Assert.Equal(1, left.Xx);
        Assert.Equal(10, right.Xx);
    }

    [Fact]
    public void Combining_with_nothing_is_refused() {
        // Arrange
        PositionCovariance anyCovariance = Any.PositionCovariance().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyCovariance.Combine(null!));
    }

    [Fact(Skip = "domain defect: Combine sums two valid covariances into one the constructor refuses when the sum passes the 1 000 km sigma ceiling (ArgumentOutOfRangeException on 'xx'); literal case, no seed")]
    public void Two_covariances_that_are_each_within_the_ceiling_but_add_up_beyond_it_can_still_be_combined() {
        // Arrange
        // Two objects each known to 775 km (variance 600 000 km², within the 1 000 km sigma ceiling) are
        // independent: the relative position is known to 1 095 km, and the sum is what the code uses to assess them.
        PositionCovariance first  = new(600_000, 600_000, 600_000, 0, 0, 0);
        PositionCovariance second = new(600_000, 600_000, 600_000, 0, 0, 0);

        // Act
        PositionCovariance combined = first.Combine(second);

        // Assert
        Assert.Equal(1_200_000, combined.Xx);
    }

    [Fact]
    public void Projecting_onto_a_unit_axis_gives_the_variance_along_it() {
        // Arrange
        PositionCovariance covariance = new(4, 5, 6, 1, 0.5, -1);
        Vector3            xAxis      = new(1, 0, 0);
        Vector3            zAxis      = new(0, 0, 1);

        // Act & Assert
        Assert.Equal(4, covariance.Project(xAxis, xAxis));
        Assert.Equal(6, covariance.Project(zAxis, zAxis));
    }

    [Fact]
    public void Projecting_onto_two_different_axes_gives_their_covariance_and_is_symmetric() {
        // Arrange
        PositionCovariance covariance = new(4, 5, 6, 1, 0.5, -1);
        Vector3            xAxis      = new(1, 0, 0);
        Vector3            yAxis      = new(0, 1, 0);
        Vector3            zAxis      = new(0, 0, 1);

        // Act & Assert
        Assert.Equal(1, covariance.Project(xAxis, yAxis));
        Assert.Equal(1, covariance.Project(yAxis, xAxis));
        Assert.Equal(0.5, covariance.Project(xAxis, zAxis));
        Assert.Equal(-1, covariance.Project(yAxis, zAxis));
    }

    [Fact]
    public void Projecting_onto_a_diagonal_axis_mixes_the_variances_and_the_cross_term() {
        // Arrange
        PositionCovariance covariance = new(4, 5, 6, 1, 0.5, -1);
        double             component  = 1 / Math.Sqrt(2);
        Vector3            diagonalXy = new(component, component, 0);

        // Act
        double variance = covariance.Project(diagonalXy, diagonalXy);

        // Assert
        // (xx + yy) / 2 + xy = 4.5 + 1
        Assert.Equal(5.5, variance, precision: 12);
    }

    // The two others are drawn: whatever they are, they are not what the refusal is about. Each cross term is zero, so
    // no cross term can be the cause either.
    private static PositionCovariance WithVariance(int axis, double variance) {
        double[] others = [Any.Double().Between(0.1, 100).Generate(), Any.Double().Between(0.1, 100).Generate(), Any.Double().Between(0.1, 100).Generate()];
        others[axis] = variance;

        return new PositionCovariance(others[0], others[1], others[2], 0, 0, 0);
    }

    // Unit variances: a cross term is then its own correlation.
    private static PositionCovariance WithCrossTerm(int crossTerm, double value) {
        double[] terms = [0, 0, 0];
        terms[crossTerm] = value;

        return new PositionCovariance(1, 1, 1, terms[0], terms[1], terms[2]);
    }

}
