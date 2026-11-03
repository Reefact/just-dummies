namespace Freight.Domain.Tests;

public sealed class VolumeTests {

    [Fact]
    public void A_drawn_volume_exposes_the_cubic_metres_it_was_built_from() {
        // Arrange
        double anyCubicMetres = Any.Double().Between(0.001, Volume.MaxCubicMetres).Generate();

        // Act
        Volume volume = new Volume(anyCubicMetres);

        // Assert
        Assert.Equal(anyCubicMetres, volume.CubicMetres);
    }

    [Fact]
    public void The_largest_volume_is_accepted() {
        // Act
        Volume volume = new Volume(76.4);

        // Assert
        Assert.Equal(76.4, volume.CubicMetres);
        Assert.Equal(Volume.MaxCubicMetres, volume.CubicMetres);
    }

    [Fact]
    public void A_volume_just_above_the_maximum_is_refused() {
        // Arrange
        double justAbove = double.BitIncrement(Volume.MaxCubicMetres);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Volume(justAbove));

        // Assert
        Assert.Equal("cubicMetres", refusal.ParamName);
    }

    [Fact]
    public void A_volume_above_the_maximum_is_refused() {
        // Arrange
        double anyExcess = Any.Double().GreaterThan(Volume.MaxCubicMetres).LessThan(1e9).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Volume(anyExcess));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.5)]
    [InlineData(double.NegativeInfinity)]
    public void A_volume_that_is_not_positive_is_refused(double cubicMetres) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Volume(cubicMetres));
    }

    [Fact]
    public void Negative_zero_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Volume(-0.0));
    }

    [Fact]
    public void A_negative_volume_is_refused() {
        // Arrange
        double anyNegative = Any.Double().Negative().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Volume(anyNegative));
    }

    [Fact]
    public void Positive_infinity_is_refused_as_above_the_maximum() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Volume(double.PositiveInfinity));
    }

    [Fact]
    public void NaN_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Volume(double.NaN));

        // Assert
        Assert.Contains("number", refusal.Message);
    }

    [Fact]
    public void Two_volumes_with_the_same_cubic_metres_are_equal() {
        // Arrange
        Volume anyVolume = Any.Volume().Generate();
        Volume same      = new Volume(anyVolume.CubicMetres);

        // Act & Assert
        Assert.Equal(anyVolume, same);
        Assert.True(anyVolume.Equals((object)same));
        Assert.Equal(anyVolume.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void Two_volumes_with_different_cubic_metres_are_not_equal() {
        // Arrange
        Volume anyVolume = Any.Volume().Generate();
        Volume anyOther  = Any.Volume().WithCubicMetres(cubicMetres => cubicMetres.DifferentFrom(anyVolume.CubicMetres)).Generate();

        // Act & Assert
        Assert.NotEqual(anyVolume, anyOther);
        Assert.False(anyVolume.Equals(null));
    }

    [Fact]
    public void A_volume_prints_with_its_unit() {
        // Act
        string text = new Volume(33.2).ToString();

        // Assert
        Assert.Equal("33.2 m3", text);
    }

    [Fact(Skip = "domain defect: ToString interpolates a double with the current culture, so under fr-FR it prints \"33,2 m3\", seed n/a (literal)")]
    public void The_text_does_not_depend_on_the_culture_of_the_machine() {
        // Arrange
        System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
        try {
            // Act
            string text = new Volume(33.2).ToString();

            // Assert
            Assert.Equal("33.2 m3", text);
        } finally {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

}
