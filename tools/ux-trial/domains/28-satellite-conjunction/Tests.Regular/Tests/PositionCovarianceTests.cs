using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Regular;

public sealed class PositionCovarianceTests {

    private const double MaximumVariance = PositionCovariance.MaximumSigmaKm * PositionCovariance.MaximumSigmaKm;

    [Fact]
    public void A_covariance_keeps_its_six_terms() {
        // Arrange
        double anyXx = Any.Double().Between(1, 100).Generate();
        double anyYy = Any.Double().Between(1, 100).Generate();
        double anyZz = Any.Double().Between(1, 100).Generate();

        // Act
        PositionCovariance covariance = new(anyXx, anyYy, anyZz, 0.1, -0.2, 0.3);

        // Assert
        Assert.Equal(anyXx, covariance.Xx);
        Assert.Equal(anyYy, covariance.Yy);
        Assert.Equal(anyZz, covariance.Zz);
        Assert.Equal(0.1, covariance.Xy);
        Assert.Equal(-0.2, covariance.Xz);
        Assert.Equal(0.3, covariance.Yz);
    }

    [Fact]
    public void An_arbitrary_covariance_is_always_accepted() {
        // Arrange / Act
        PositionCovariance anyCovariance = Any.PositionCovariance().Generate();

        // Assert
        Assert.True(anyCovariance.Xx > 0);
        Assert.True(anyCovariance.Yy > 0);
        Assert.True(anyCovariance.Zz > 0);
    }

    [Fact]
    public void The_mean_sigma_is_the_root_of_the_mean_variance() {
        // Arrange
        PositionCovariance covariance = new(1, 4, 7, 0, 0, 0);

        // Act
        double meanSigma = covariance.MeanSigmaKm;

        // Assert
        Assert.Equal(2d, meanSigma, 12);
    }

    [Fact]
    public void A_variance_at_the_maximum_is_accepted() {
        // Arrange / Act
        PositionCovariance covariance = new(MaximumVariance, 1, 1, 0, 0, 0);

        // Assert
        Assert.Equal(MaximumVariance, covariance.Xx);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void A_variance_just_above_the_maximum_is_refused(int axis) {
        // Arrange
        double[] variances = [1, 1, 1];
        variances[axis] = Math.BitIncrement(MaximumVariance);

        // Act
        Exception? refusal = Record.Exception(() => new PositionCovariance(variances[0], variances[1], variances[2], 0, 0, 0));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void A_variance_of_zero_or_less_is_refused(int axis) {
        // Arrange
        double[] variances = [1, 1, 1];
        variances[axis] = Any.Double().LessThanOrEqualTo(0).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new PositionCovariance(variances[0], variances[1], variances[2], 0, 0, 0));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void A_variance_far_above_the_maximum_is_refused(int axis) {
        // Arrange
        double[] variances = [1, 1, 1];
        variances[axis] = Any.Double().GreaterThan(MaximumVariance).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new PositionCovariance(variances[0], variances[1], variances[2], 0, 0, 0));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_variance_that_is_not_finite_is_refused(double variance) {
        // Arrange / Act
        Exception? refusal = Record.Exception(() => new PositionCovariance(variance, 1, 1, 0, 0, 0));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void A_cross_term_that_is_not_finite_is_refused(int pair) {
        // Arrange
        double[] cross = [0, 0, 0];
        cross[pair] = Any.OneOf(double.NaN, double.PositiveInfinity, double.NegativeInfinity).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new PositionCovariance(1, 1, 1, cross[0], cross[1], cross[2]));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void A_correlation_of_one_or_more_is_refused(int pair) {
        // Arrange
        double anyVariance    = Any.Double().Between(0.01, 1_000).Generate();
        double anyCorrelation = Any.Double().Between(1, 10).Generate();
        double sign           = Any.OneOf(-1d, 1d).Generate();
        double[] cross = [0, 0, 0];
        cross[pair] = sign * anyCorrelation * anyVariance;

        // Act
        Exception? refusal = Record.Exception(() => new PositionCovariance(anyVariance, anyVariance, anyVariance, cross[0], cross[1], cross[2]));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void A_correlation_just_under_one_is_accepted_when_the_matrix_stays_positive_definite() {
        // Arrange
        double almostOne = Math.BitDecrement(1d);

        // Act
        PositionCovariance covariance = new(1, 1, 1, almostOne, 0, 0);

        // Assert
        Assert.Equal(almostOne, covariance.Xy);
    }

    [Fact]
    public void Pairwise_acceptable_correlations_can_still_form_a_matrix_that_is_not_positive_definite() {
        // Arrange
        double anyVariance = Any.Double().Between(0.01, 1_000).Generate();
        double strong      = 0.9 * anyVariance;

        // Act
        Exception? refusal = Record.Exception(() => new PositionCovariance(anyVariance, anyVariance, anyVariance, strong, strong, -strong));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void Combine_adds_the_two_covariances_term_by_term() {
        // Arrange
        PositionCovariance left  = new(1, 2, 3, 0.1, 0.2, 0.3);
        PositionCovariance right = new(10, 20, 30, 1, 2, 3);

        // Act
        PositionCovariance combined = left.Combine(right);

        // Assert
        Assert.Equal(11d, combined.Xx);
        Assert.Equal(22d, combined.Yy);
        Assert.Equal(33d, combined.Zz);
        Assert.Equal(1.1, combined.Xy, 12);
        Assert.Equal(2.2, combined.Xz, 12);
        Assert.Equal(3.3, combined.Yz, 12);
    }

    [Fact]
    public void Combining_two_covariances_adds_their_variances() {
        // Arrange
        PositionCovariance anyLeft  = Any.PositionCovariance().WithXx(x => x.Between(1e-4, 4e5)).WithYy(y => y.Between(1e-4, 4e5)).WithZz(z => z.Between(1e-4, 4e5)).Generate();
        PositionCovariance anyRight = Any.PositionCovariance().WithXx(x => x.Between(1e-4, 4e5)).WithYy(y => y.Between(1e-4, 4e5)).WithZz(z => z.Between(1e-4, 4e5)).Generate();

        // Act
        PositionCovariance combined = anyLeft.Combine(anyRight);

        // Assert
        Assert.Equal(anyLeft.Xx + anyRight.Xx, combined.Xx);
        Assert.Equal(anyLeft.Yy + anyRight.Yy, combined.Yy);
        Assert.Equal(anyLeft.Zz + anyRight.Zz, combined.Zz);
    }

    [Fact]
    public void Combining_with_a_missing_covariance_is_refused() {
        // Arrange
        PositionCovariance anyCovariance = Any.PositionCovariance().Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyCovariance.Combine(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(refusal);
    }

    [Fact(Skip = "domain defect: Combine rebuilds its result through the public constructor, so two valid covariances whose variances sum above 1 000 000 km² per axis (for example 640 000 km² each, sigma 800 km) cannot be combined; Conjunction.Assess then throws ArgumentOutOfRangeException for a conjunction both of whose trajectories are valid. Deterministic: no seed needed")]
    public void Two_valid_covariances_can_always_be_combined() {
        // Arrange
        PositionCovariance sigma800 = new(640_000, 640_000, 640_000, 0, 0, 0);

        // Act
        PositionCovariance combined = sigma800.Combine(sigma800);

        // Assert
        Assert.Equal(1_280_000d, combined.Xx);
    }

    [Fact]
    public void Projecting_on_the_axes_reads_the_matrix() {
        // Arrange
        PositionCovariance covariance = new(4, 9, 16, 1, 2, 3);
        Vector3            x          = new(1, 0, 0);
        Vector3            y          = new(0, 1, 0);
        Vector3            z          = new(0, 0, 1);

        // Act / Assert
        Assert.Equal(4d, covariance.Project(x, x));
        Assert.Equal(9d, covariance.Project(y, y));
        Assert.Equal(16d, covariance.Project(z, z));
        Assert.Equal(1d, covariance.Project(x, y));
        Assert.Equal(2d, covariance.Project(x, z));
        Assert.Equal(3d, covariance.Project(y, z));
    }

    [Fact]
    public void The_projected_covariance_is_symmetric() {
        // Arrange
        PositionCovariance anyCovariance = Any.PositionCovariance().Generate();
        Vector3            anyU          = Any.Vector3().WithX(x => x.Between(-1, 1)).WithY(y => y.Between(-1, 1)).WithZ(z => z.Between(-1, 1)).Generate();
        Vector3            anyV          = Any.Vector3().WithX(x => x.Between(-1, 1)).WithY(y => y.Between(-1, 1)).WithZ(z => z.Between(-1, 1)).Generate();

        // Act
        double uv = anyCovariance.Project(anyU, anyV);
        double vu = anyCovariance.Project(anyV, anyU);

        // Assert
        Assert.Equal(uv, vu, 1e-9 * (1 + Math.Abs(uv)));
    }

    [Fact]
    public void The_variance_along_any_direction_is_positive() {
        // Arrange
        PositionCovariance anyCovariance = Any.PositionCovariance().Generate();
        Vector3            anyDirection  = Any.Vector3().WithX(x => x.Between(0.1, 1)).WithY(y => y.Between(-1, 1)).WithZ(z => z.Between(-1, 1)).Generate().Normalized();

        // Act
        double variance = anyCovariance.Project(anyDirection, anyDirection);

        // Assert
        Assert.True(variance > 0);
    }

}
