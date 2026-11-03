namespace Laboratory.Tests;

public sealed class SpecimenQualityTests {

    // ── construction ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_quality_exposes_what_it_was_built_from() {
        // Act
        SpecimenQuality quality = new SpecimenQuality(3.5, 40, TimeSpan.FromHours(6));

        // Assert
        Assert.Equal(3.5, quality.VolumeMillilitres);
        Assert.Equal(40, quality.HemolysisIndex);
        Assert.Equal(TimeSpan.FromHours(6), quality.MaxTransit);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]            // NaN is refused too
    [InlineData(50.000001)]
    [InlineData(51.0)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_volume_that_is_not_positive_or_beyond_a_tube_is_refused(double volume) {
        // Arrange
        int      anyHemolysis = Any.Int32().Between(0, 500).Generate();
        TimeSpan anyTransit   = Any.TimeSpan().Positive().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SpecimenQuality(volume, anyHemolysis, anyTransit));

        // Assert
        Assert.Equal("volumeMillilitres", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.000001)]     // the smallest volumes are still volumes
    [InlineData(50.0)]         // a full tube
    public void A_volume_up_to_a_full_tube_is_accepted(double volume) {
        // Arrange
        int      anyHemolysis = Any.Int32().Between(0, 500).Generate();
        TimeSpan anyTransit   = Any.TimeSpan().Positive().Generate();

        // Act
        SpecimenQuality quality = new SpecimenQuality(volume, anyHemolysis, anyTransit);

        // Assert
        Assert.Equal(volume, quality.VolumeMillilitres);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(501)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_hemolysis_index_outside_zero_to_five_hundred_is_refused(int hemolysis) {
        // Arrange
        double   anyVolume  = Any.Double().Between(0.1, 50d).Generate();
        TimeSpan anyTransit = Any.TimeSpan().Positive().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SpecimenQuality(anyVolume, hemolysis, anyTransit));

        // Assert
        Assert.Equal("hemolysisIndex", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    public void A_hemolysis_index_of_zero_or_five_hundred_is_accepted(int hemolysis) {
        // Arrange
        double   anyVolume  = Any.Double().Between(0.1, 50d).Generate();
        TimeSpan anyTransit = Any.TimeSpan().Positive().Generate();

        // Act
        SpecimenQuality quality = new SpecimenQuality(anyVolume, hemolysis, anyTransit);

        // Assert
        Assert.Equal(hemolysis, quality.HemolysisIndex);
    }

    [Fact]
    public void A_transit_time_of_zero_is_refused() {
        // Arrange
        double anyVolume     = Any.Double().Between(0.1, 50d).Generate();
        int    anyHemolysis = Any.Int32().Between(0, 500).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SpecimenQuality(anyVolume, anyHemolysis, TimeSpan.Zero));

        // Assert
        Assert.Equal("maxTransit", refusal.ParamName);
    }

    [Fact]
    public void A_negative_transit_time_is_refused() {
        // Arrange
        double   anyVolume       = Any.Double().Between(0.1, 50d).Generate();
        int      anyHemolysis    = Any.Int32().Between(0, 500).Generate();
        TimeSpan anyNegativeTime = Any.TimeSpan().Negative().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpecimenQuality(anyVolume, anyHemolysis, anyNegativeTime));
    }

    [Fact]
    public void The_smallest_transit_time_is_accepted() {
        // Arrange
        TimeSpan oneTick = TimeSpan.FromTicks(1);

        // Act
        SpecimenQuality quality = Any.SpecimenQuality().WithMaxTransit(oneTick).Generate();

        // Assert
        Assert.Equal(oneTick, quality.MaxTransit);
    }

    // ── potassium ───────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_sufficient_volume_with_the_hemolysis_index_at_the_limit_is_acceptable_for_potassium() {
        // Arrange: the volume is sufficient, whatever it is; the hemolysis index is exactly at the limit
        SpecimenQuality quality = Any.SpecimenQuality()
                                     .WithVolumeMillilitres(v => v.GreaterThanOrEqualTo(SpecimenQuality.MinimumVolumeForChemistry))
                                     .WithHemolysisIndex(100)
                                     .Generate();

        // Act / Assert
        Assert.True(quality.AcceptableForPotassium);
    }

    [Fact]
    public void A_sufficient_volume_with_the_hemolysis_index_one_above_the_limit_is_not_acceptable_for_potassium() {
        // Arrange
        SpecimenQuality quality = Any.SpecimenQuality()
                                     .WithVolumeMillilitres(v => v.GreaterThanOrEqualTo(SpecimenQuality.MinimumVolumeForChemistry))
                                     .WithHemolysisIndex(101)
                                     .Generate();

        // Act / Assert
        Assert.False(quality.AcceptableForPotassium);
    }

    [Fact]
    public void Any_lower_hemolysis_index_with_a_sufficient_volume_is_acceptable_for_potassium() {
        // Arrange
        SpecimenQuality quality = Any.SpecimenQuality()
                                     .WithVolumeMillilitres(v => v.GreaterThanOrEqualTo(SpecimenQuality.MinimumVolumeForChemistry))
                                     .WithHemolysisIndex(h => h.Between(0, 100))
                                     .Generate();

        // Act / Assert
        Assert.True(quality.AcceptableForPotassium);
    }

    [Fact]
    public void A_volume_of_exactly_the_chemistry_minimum_is_acceptable_for_potassium() {
        // Arrange
        SpecimenQuality quality = Any.SpecimenQuality()
                                     .WithVolumeMillilitres(0.5)
                                     .WithHemolysisIndex(h => h.Between(0, 100))
                                     .Generate();

        // Act / Assert
        Assert.True(quality.AcceptableForPotassium);
    }

    [Fact]
    public void A_volume_just_below_the_chemistry_minimum_is_not_acceptable_for_potassium() {
        // Arrange
        SpecimenQuality quality = Any.SpecimenQuality()
                                     .WithVolumeMillilitres(0.4999)
                                     .WithHemolysisIndex(h => h.Between(0, 100))
                                     .Generate();

        // Act / Assert
        Assert.False(quality.AcceptableForPotassium);
    }

    [Fact]
    public void A_specimen_failing_both_ways_is_not_acceptable_for_potassium() {
        // Arrange
        SpecimenQuality quality = Any.SpecimenQuality()
                                     .WithVolumeMillilitres(0.1)
                                     .WithHemolysisIndex(h => h.GreaterThan(100))
                                     .Generate();

        // Act / Assert
        Assert.False(quality.AcceptableForPotassium);
    }

    [Fact]
    public void The_chemistry_minimum_volume_is_half_a_millilitre() {
        Assert.Equal(0.5, SpecimenQuality.MinimumVolumeForChemistry);
    }

}
