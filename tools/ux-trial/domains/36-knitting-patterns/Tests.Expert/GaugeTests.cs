using KnittingPatterns.Domain;

namespace KnittingPatterns.Tests.Expert;

public sealed class GaugeTests {

    [Fact]
    public void The_stitches_to_cast_on_for_a_width_follow_the_stitch_gauge() {
        // Arrange
        Gauge dkWeight = new(22m, 30m);

        // Act
        int castOn = dkWeight.StitchesForWidth(50m);

        // Assert
        Assert.Equal(110, castOn);
    }

    [Fact]
    public void A_half_stitch_or_row_is_rounded_up() {
        // Arrange
        Gauge gauge = new(21m, 27m);

        // Act
        (int stitches, int rows) = (gauge.StitchesForWidth(5m), gauge.RowsForLength(5m));

        // Assert
        Assert.Equal((11, 14), (stitches, rows));   // 10.5 stitches and 13.5 rows
    }

    [Fact]
    public void The_width_of_a_number_of_stitches_is_read_back_from_the_stitch_gauge() {
        // Arrange
        Gauge chunky = new(12m, 16m);

        // Act
        decimal width = chunky.WidthOf(30);

        // Assert
        Assert.Equal(25m, width);
    }

    [Fact]
    public void A_row_gauge_more_than_twice_the_stitch_gauge_is_not_a_plausible_fabric() {
        // Arrange
        // (both values are the case)

        // Act
        Exception? refusal = Record.Exception(() => new Gauge(20m, 41m));

        // Assert
        ArgumentException implausible = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("rowsPer10Cm", implausible.ParamName);
    }

}
