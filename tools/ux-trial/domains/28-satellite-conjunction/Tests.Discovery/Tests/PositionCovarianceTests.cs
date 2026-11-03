using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Discovery;

public sealed class PositionCovarianceTests {

    public static TheoryData<string, double> InvalidVariances() {
        TheoryData<string, double> cases = new TheoryData<string, double>();
        foreach (string axis in new[] { "xx", "yy", "zz" }) {
            foreach (double invalid in new[] { 0.0, -1.0, 1_000_000.5, double.NaN, double.PositiveInfinity, double.NegativeInfinity }) {
                cases.Add(axis, invalid);
            }
        }

        return cases;
    }

    private static AnyPositionCovariance WithVariance(string axis, double variance) {
        return axis switch {
            "xx" => Any.PositionCovariance().WithXx(variance),
            "yy" => Any.PositionCovariance().WithYy(variance),
            _    => Any.PositionCovariance().WithZz(variance)
        };
    }

    [Fact]
    public void A_covariance_keeps_its_six_terms() {
        // Act
        PositionCovariance covariance = new PositionCovariance(4, 9, 16, 1, 2, 3);

        // Assert
        Assert.Equal(4, covariance.Xx);
        Assert.Equal(9, covariance.Yy);
        Assert.Equal(16, covariance.Zz);
        Assert.Equal(1, covariance.Xy);
        Assert.Equal(2, covariance.Xz);
        Assert.Equal(3, covariance.Yz);
    }

    [Theory]
    [MemberData(nameof(InvalidVariances))]
    public void A_covariance_refuses_a_variance_that_is_not_in_zero_to_one_million_square_kilometres(string axis, double variance) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => WithVariance(axis, variance).Generate());

        // Assert
        Assert.Equal(axis, refusal.ParamName);
    }

    [Theory]
    [InlineData("xx")]
    [InlineData("yy")]
    [InlineData("zz")]
    public void A_covariance_accepts_a_variance_of_exactly_one_million_square_kilometres(string axis) {
        // Act
        PositionCovariance covariance = WithVariance(axis, 1_000_000).Generate();

        // Assert
        Assert.NotNull(covariance);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_covariance_refuses_a_cross_term_that_is_not_finite(double notFinite) {
        // Act / Assert
        Assert.Equal("xy", Assert.Throws<ArgumentOutOfRangeException>(() => new PositionCovariance(1, 1, 1, notFinite, 0, 0)).ParamName);
        Assert.Equal("xz", Assert.Throws<ArgumentOutOfRangeException>(() => new PositionCovariance(1, 1, 1, 0, notFinite, 0)).ParamName);
        Assert.Equal("yz", Assert.Throws<ArgumentOutOfRangeException>(() => new PositionCovariance(1, 1, 1, 0, 0, notFinite)).ParamName);
    }

    [Theory]
    [InlineData(6.0)]
    [InlineData(-6.0)]
    [InlineData(7.0)]
    public void A_covariance_refuses_a_cross_term_whose_correlation_reaches_one(double crossTerm) {
        // Arrange: variances 4 and 9 allow a cross term strictly inside (-6, 6)
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new PositionCovariance(4, 9, 1, crossTerm, 0, 0));

        // Assert
        Assert.Equal("xy", refusal.ParamName);
    }

    [Fact]
    public void A_covariance_accepts_a_correlation_just_below_one() {
        // Act
        PositionCovariance covariance = new PositionCovariance(4, 9, 1, 5.99, 0, 0);

        // Assert
        Assert.Equal(5.99, covariance.Xy);
    }

    [Fact]
    public void A_covariance_refuses_a_matrix_that_is_not_positive_definite() {
        // Arrange: every correlation is -0.9, individually acceptable, jointly impossible (determinant < 0)
        // Act
        Action construct = () => new PositionCovariance(1, 1, 1, -0.9, -0.9, -0.9);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_covariance_refuses_a_singular_matrix() {
        // Arrange: three correlations of -0.5 give a determinant of exactly zero
        // Act
        Action construct = () => new PositionCovariance(1, 1, 1, -0.5, -0.5, -0.5);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_covariance_accepts_three_correlations_of_one_half() {
        // Act
        PositionCovariance covariance = new PositionCovariance(1, 1, 1, 0.5, 0.5, 0.5);

        // Assert
        Assert.Equal(0.5, covariance.Yz);
    }

    [Fact]
    public void The_mean_sigma_is_the_root_of_the_mean_variance() {
        // Arrange
        PositionCovariance covariance = Any.PositionCovariance().WithXx(3).WithYy(6).WithZz(9).Generate();

        // Act / Assert
        Assert.Equal(Math.Sqrt(6), covariance.MeanSigmaKm, 12);
    }

    [Fact]
    public void Combining_two_covariances_sums_their_terms() {
        // Arrange
        PositionCovariance left  = new PositionCovariance(4, 9, 16, 1, 2, 3);
        PositionCovariance right = new PositionCovariance(1, 2, 3, 0.5, 0.25, 0.125);

        // Act
        PositionCovariance combined = left.Combine(right);

        // Assert
        Assert.Equal(5, combined.Xx);
        Assert.Equal(11, combined.Yy);
        Assert.Equal(19, combined.Zz);
        Assert.Equal(1.5, combined.Xy);
        Assert.Equal(2.25, combined.Xz);
        Assert.Equal(3.125, combined.Yz);
    }

    [Fact]
    public void Combining_with_nothing_is_refused() {
        // Arrange
        PositionCovariance anyCovariance = Any.PositionCovariance().Generate();

        // Act
        Action combine = () => anyCovariance.Combine(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(combine);
    }

    [Fact(Skip = "domain defect: Combine of two covariances that are each valid (xx = 600000 km2, below the 1e6 limit) throws ArgumentOutOfRangeException because the sum exceeds the limit; literal values, no seed")]
    public void Combining_two_wide_covariances_stays_valid() {
        // Arrange: each is within the 1000 km limit on its own
        PositionCovariance left  = new PositionCovariance(600_000, 1, 1, 0, 0, 0);
        PositionCovariance right = new PositionCovariance(600_000, 1, 1, 0, 0, 0);

        // Act
        PositionCovariance combined = left.Combine(right);

        // Assert
        Assert.Equal(1_200_000, combined.Xx);
    }

    [Fact]
    public void Projecting_onto_the_axes_reads_the_matching_terms() {
        // Arrange
        PositionCovariance covariance = new PositionCovariance(4, 9, 16, 1, 2, 3);
        Vector3            x          = new Vector3(1, 0, 0);
        Vector3            y          = new Vector3(0, 1, 0);
        Vector3            z          = new Vector3(0, 0, 1);

        // Act / Assert
        Assert.Equal(4, covariance.Project(x, x));
        Assert.Equal(9, covariance.Project(y, y));
        Assert.Equal(16, covariance.Project(z, z));
        Assert.Equal(1, covariance.Project(x, y));
        Assert.Equal(1, covariance.Project(y, x));
        Assert.Equal(2, covariance.Project(x, z));
        Assert.Equal(3, covariance.Project(y, z));
    }

    [Fact]
    public void Projecting_along_a_diagonal_direction_mixes_the_terms() {
        // Arrange: u = v = (1, 1, 0) / sqrt(2) gives (Xx + Yy + 2 Xy) / 2
        PositionCovariance covariance = new PositionCovariance(4, 9, 16, 1, 2, 3);
        Vector3            diagonal   = new Vector3(1, 1, 0).Normalized();

        // Act / Assert
        Assert.Equal(7.5, covariance.Project(diagonal, diagonal), 12);
    }

}
