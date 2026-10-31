using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Regular.Tests;

public sealed class EpochTests {

    [Fact]
    public void An_epoch_refuses_an_instant_that_is_not_expressed_in_utc() {
        // Arrange
        DateTimeOffset anyInstant = Any.DateTimeOffset().WithOffset(TimeSpan.FromHours(2)).After(Epoch.SpaceAgeStart).Generate();

        // Act
        Action act = () => new Epoch(anyInstant);

        // Assert
        Assert.Throws<ArgumentException>("utc", act);
    }

    [Fact]
    public void An_epoch_refuses_an_instant_before_the_space_age() {
        // Arrange
        DateTimeOffset anyPreSpaceAge = Any.DateTimeOffset().Before(Epoch.SpaceAgeStart).Generate();

        // Act
        Action act = () => new Epoch(anyPreSpaceAge);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("utc", act);
    }

    [Fact]
    public void Seconds_since_an_epoch_gives_back_the_offset_that_was_added() {
        // Arrange
        Epoch anyEpoch = Arbitrary.Epoch().Generate();
        Epoch later    = anyEpoch.Add(TimeSpan.FromSeconds(90));

        // Act
        double seconds = later.SecondsSince(anyEpoch);

        // Assert
        Assert.Equal(90, seconds);
    }

}

public sealed class Vector3Tests {

    [Fact]
    public void The_cross_product_of_x_and_y_is_z() {
        // Arrange
        Vector3 x = new(1, 0, 0);
        Vector3 y = new(0, 1, 0);

        // Act
        Vector3 cross = x.Cross(y);

        // Assert
        Assert.Equal(new Vector3(0, 0, 1), cross);
    }

    [Fact]
    public void A_normalized_vector_has_unit_length() {
        // Arrange
        Vector3 anyVelocity = Arbitrary.OrbitalState().Generate().VelocityKmPerSecond;

        // Act
        Vector3 direction = anyVelocity.Normalized();

        // Assert
        Assert.Equal(1, direction.Magnitude, 12);
    }

    [Fact]
    public void The_zero_vector_has_no_direction() {
        // Arrange
        Vector3 zero = Vector3.Zero;

        // Act
        Action act = () => zero.Normalized();

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

}

public sealed class CollisionProbabilityTests {

    [Theory]
    [InlineData(1e-4, RiskLevel.High)]
    [InlineData(9.9e-5, RiskLevel.Elevated)]
    [InlineData(1e-5, RiskLevel.Elevated)]
    [InlineData(9.9e-6, RiskLevel.Low)]
    public void A_probability_is_classified_against_the_operational_thresholds(double value, RiskLevel expected) {
        // Arrange
        CollisionProbability probability = new(value);

        // Act
        RiskLevel level = probability.Level;

        // Assert
        Assert.Equal(expected, level);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void A_probability_outside_zero_to_one_is_refused(double value) {
        // Arrange
        // (nothing: the value is the case)

        // Act
        Action act = () => new CollisionProbability(value);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("value", act);
    }

}

public sealed class PositionCovarianceTests {

    [Fact]
    public void Combining_two_independent_covariances_adds_them_term_by_term() {
        // Arrange
        // Each variance stays under half the ceiling: the domain refuses a combined variance above 1000² km².
        double halfCeiling = PositionCovariance.MaximumSigmaKm * PositionCovariance.MaximumSigmaKm / 2;
        AnyPositionCovariance anyCovariances = Arbitrary.PositionCovariance()
            .WithXx(v => v.LessThanOrEqualTo(halfCeiling))
            .WithYy(v => v.LessThanOrEqualTo(halfCeiling))
            .WithZz(v => v.LessThanOrEqualTo(halfCeiling));
        PositionCovariance anyPrimary   = anyCovariances.Generate();
        PositionCovariance anySecondary = anyCovariances.Generate();

        // Act
        PositionCovariance combined = anyPrimary.Combine(anySecondary);

        // Assert
        Assert.Equal(anyPrimary.Xx + anySecondary.Xx, combined.Xx);
        Assert.Equal(anyPrimary.Yz + anySecondary.Yz, combined.Yz);
    }

    [Fact]
    public void A_cross_term_reaching_full_correlation_is_refused() {
        // Arrange
        // xy = sqrt(xx * yy): correlation exactly 1.

        // Act
        Action act = () => new PositionCovariance(4, 9, 1, 6, 0, 0);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("xy", act);
    }

    [Fact]
    public void A_covariance_that_is_not_positive_definite_is_refused() {
        // Arrange
        // Every pairwise correlation is legal (|0.9| < 1), but together they are inconsistent.

        // Act
        Action act = () => new PositionCovariance(1, 1, 1, 0.9, 0.9, -0.9);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void The_mean_sigma_of_an_isotropic_covariance_is_its_sigma() {
        // Arrange
        PositionCovariance covariance = Encounters.Isotropic(0.2);

        // Act
        double meanSigma = covariance.MeanSigmaKm;

        // Assert
        Assert.Equal(0.2, meanSigma, 12);
    }

}
