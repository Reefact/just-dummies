using JustDummies;
using JustDummies.Xunit;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Discovery;

[Reproducible]
public sealed class GaugeTests {

    [Fact]
    public void The_cast_on_for_a_width_follows_the_stitch_gauge() {
        // Arrange
        Gauge gauge = Any.Gauge().WithStitchesPer10Cm(22m).Generate();

        // Act
        int stitches = gauge.StitchesForWidth(50m);

        // Assert
        Assert.Equal(110, stitches);
    }

    [Fact]
    public void A_cast_on_rounds_half_a_stitch_up() {
        // Arrange
        Gauge gauge = Any.Gauge().WithStitchesPer10Cm(21m).Generate();

        // Act
        int stitches = gauge.StitchesForWidth(15m);   // 31.5 stitches

        // Assert
        Assert.Equal(32, stitches);
    }

    [Fact]
    public void The_rows_for_a_length_follow_the_row_gauge() {
        // Arrange
        Gauge gauge = Any.Gauge().WithStitchesPer10Cm(20m).WithRowsPer10Cm(28m).Generate();

        // Act
        int rows = gauge.RowsForLength(25m);

        // Assert
        Assert.Equal(70, rows);
    }

    [Fact]
    public void The_width_of_a_number_of_stitches_follows_the_stitch_gauge() {
        // Arrange
        Gauge gauge = Any.Gauge().WithStitchesPer10Cm(20m).Generate();

        // Act
        decimal width = gauge.WidthOf(50);

        // Assert
        Assert.Equal(25m, width);
    }

    [Fact]
    public void A_fabric_three_times_as_many_rows_as_stitches_is_not_plausible() {
        // Arrange
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Gauge(10m, 30m));

        // Assert
        Assert.Equal("rowsPer10Cm", error.ParamName);
    }

}
