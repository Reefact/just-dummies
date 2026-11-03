using JustDummies;

using Xunit;

namespace Laboratory.Domain.Tests;

public sealed class SpecimenQualityTests {

    [Fact]
    public void A_tube_within_the_limits_is_accepted_and_kept() {
        // Arrange
        double   anyVolume    = Any.Double().Between(0.001d, 50d).Generate();
        int      anyHemolysis = Any.Int32().Between(0, 500).Generate();
        TimeSpan anyTransit   = Any.TimeSpan().Positive().Generate();

        // Act
        SpecimenQuality quality = new SpecimenQuality(anyVolume, anyHemolysis, anyTransit);

        // Assert
        Assert.Equal(anyVolume, quality.VolumeMillilitres);
        Assert.Equal(anyHemolysis, quality.HemolysisIndex);
        Assert.Equal(anyTransit, quality.MaxTransit);
    }

    [Fact]
    public void The_generator_only_draws_qualities_the_constructor_accepts() {
        // Act
        SpecimenQuality anyQuality = Any.SpecimenQuality().Generate();

        // Assert
        Assert.InRange(anyQuality.VolumeMillilitres, double.Epsilon, 50d);
        Assert.InRange(anyQuality.HemolysisIndex, 0, 500);
        Assert.True(anyQuality.MaxTransit > TimeSpan.Zero);
    }

    // ---- volume -------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(50d)]
    [InlineData(0.5d)]
    [InlineData(double.Epsilon)]
    public void A_volume_up_to_fifty_millilitres_and_above_zero_is_accepted(double volume) {
        Assert.Equal(volume, new SpecimenQuality(volume, 0, TimeSpan.FromHours(1)).VolumeMillilitres);
    }

    [Fact]
    public void A_volume_above_fifty_millilitres_is_refused_as_out_of_range() {
        // Arrange
        double   anyTooLarge = Any.Double().GreaterThan(50d).Generate();
        int      anyHemolysis = Any.Int32().Between(0, 500).Generate();
        TimeSpan anyTransit  = Any.TimeSpan().Positive().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SpecimenQuality(anyTooLarge, anyHemolysis, anyTransit));

        // Assert
        Assert.Equal("volumeMillilitres", refusal.ParamName);
    }

    [Fact]
    public void A_volume_just_above_fifty_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpecimenQuality(Math.BitIncrement(50d), 0, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void A_volume_of_zero_or_less_is_refused_as_out_of_range() {
        // Arrange
        double anyNonPositive = Any.Double().LessThanOrEqualTo(0d).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SpecimenQuality(anyNonPositive, 0, TimeSpan.FromHours(1)));

        // Assert
        Assert.Equal("volumeMillilitres", refusal.ParamName);
    }

    [Fact]
    public void A_NaN_volume_is_refused_as_out_of_range() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpecimenQuality(double.NaN, 0, TimeSpan.FromHours(1)));
    }

    // ---- hemolysis index ----------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    public void A_hemolysis_index_from_zero_to_five_hundred_is_accepted(int index) {
        Assert.Equal(index, new SpecimenQuality(1d, index, TimeSpan.FromHours(1)).HemolysisIndex);
    }

    [Fact]
    public void A_negative_hemolysis_index_is_refused_as_out_of_range() {
        // Arrange
        int anyNegative = Any.Int32().Negative().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SpecimenQuality(1d, anyNegative, TimeSpan.FromHours(1)));

        // Assert
        Assert.Equal("hemolysisIndex", refusal.ParamName);
    }

    [Fact]
    public void A_hemolysis_index_above_five_hundred_is_refused_as_out_of_range() {
        // Arrange
        int anyTooHigh = Any.Int32().GreaterThan(500).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpecimenQuality(1d, anyTooHigh, TimeSpan.FromHours(1)));
    }

    // ---- transit ------------------------------------------------------------------------------------------------

    [Fact]
    public void A_transit_of_a_single_tick_is_accepted() {
        Assert.Equal(TimeSpan.FromTicks(1), new SpecimenQuality(1d, 0, TimeSpan.FromTicks(1)).MaxTransit);
    }

    [Fact]
    public void A_transit_of_zero_is_refused_as_out_of_range() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new SpecimenQuality(1d, 0, TimeSpan.Zero));

        // Assert
        Assert.Equal("maxTransit", refusal.ParamName);
    }

    [Fact]
    public void A_negative_transit_is_refused_as_out_of_range() {
        // Arrange
        TimeSpan anyNegative = Any.TimeSpan().Negative().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpecimenQuality(1d, 0, anyNegative));
    }

    // ---- the potassium rule -------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0, 50d, true)]
    [InlineData(100, 0.5d, true)]            // both limits are inclusive
    [InlineData(100, 25d, true)]
    [InlineData(101, 25d, false)]
    [InlineData(500, 50d, false)]
    [InlineData(0, 0.49d, false)]            // too little to run a chemistry panel
    [InlineData(100, 0.49d, false)]
    [InlineData(101, 0.49d, false)]
    public void A_specimen_is_acceptable_for_potassium_up_to_an_index_of_one_hundred_and_from_half_a_millilitre(int hemolysis, double volume, bool expected) {
        // Arrange
        TimeSpan anyTransit = Any.TimeSpan().Positive().Generate();

        // Act
        SpecimenQuality quality = new SpecimenQuality(volume, hemolysis, anyTransit);

        // Assert
        Assert.Equal(expected, quality.AcceptableForPotassium);
    }

    [Fact]
    public void The_minimum_volume_for_chemistry_is_half_a_millilitre() {
        Assert.Equal(0.5d, SpecimenQuality.MinimumVolumeForChemistry);
    }

    [Fact]
    public void Any_clear_enough_and_full_enough_tube_is_acceptable_for_potassium() {
        // Arrange
        double          anyVolume = Any.Double().Between(SpecimenQuality.MinimumVolumeForChemistry, 50d).Generate();
        int             anyIndex  = Any.Int32().Between(0, 100).Generate();
        SpecimenQuality quality   = new SpecimenQuality(anyVolume, anyIndex, Any.TimeSpan().Positive().Generate());

        // Act & Assert
        Assert.True(quality.AcceptableForPotassium);
    }

    [Fact]
    public void Any_hemolyzed_tube_is_unacceptable_for_potassium() {
        // Arrange
        double          anyVolume = Any.Double().Between(SpecimenQuality.MinimumVolumeForChemistry, 50d).Generate();
        int             anyIndex  = Any.Int32().Between(101, 500).Generate();
        SpecimenQuality quality   = new SpecimenQuality(anyVolume, anyIndex, Any.TimeSpan().Positive().Generate());

        // Act & Assert
        Assert.False(quality.AcceptableForPotassium);
    }

}
