using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Regular.Tests;

public sealed class GaugeTests {

    [Fact]
    public void The_stitches_to_cast_on_follow_the_stitch_gauge() {
        // Arrange
        Gauge anyGaugeAt22Stitches = Arbitrary.Gauge().WithStitchesPer10Cm(22m).Generate();

        // Act
        int castOn = anyGaugeAt22Stitches.StitchesForWidth(50m);

        // Assert
        Assert.Equal(110, castOn);
    }

    [Fact]
    public void A_half_stitch_is_rounded_up() {
        // Arrange
        Gauge anyGaugeAt18Stitches = Arbitrary.Gauge().WithStitchesPer10Cm(18m).Generate();

        // Act
        int castOn = anyGaugeAt18Stitches.StitchesForWidth(2.5m);

        // Assert
        Assert.Equal(5, castOn);
    }

    [Fact]
    public void A_narrow_width_on_a_bulky_gauge_still_casts_on_one_stitch() {
        // Arrange
        Gauge bulkyGauge = new(4m, 6m);

        // Act
        int castOn = bulkyGauge.StitchesForWidth(1m);

        // Assert
        Assert.Equal(1, castOn);
    }

    [Fact]
    public void The_rows_to_work_follow_the_row_gauge() {
        // Arrange
        Gauge anyGaugeAt30Rows = Arbitrary.Gauge()
                                          .WithStitchesPer10Cm(s => s.GreaterThanOrEqualTo(15m))
                                          .WithRowsPer10Cm(30m)
                                          .Generate();

        // Act
        int rows = anyGaugeAt30Rows.RowsForLength(12m);

        // Assert
        Assert.Equal(36, rows);
    }

    [Fact]
    public void The_width_of_a_stitch_count_follows_the_stitch_gauge() {
        // Arrange
        Gauge anyGaugeAt22Stitches = Arbitrary.Gauge().WithStitchesPer10Cm(22m).Generate();

        // Act
        decimal width = anyGaugeAt22Stitches.WidthOf(110);

        // Assert
        Assert.Equal(50m, width);
    }

    [Fact]
    public void A_gauge_with_more_than_twice_as_many_rows_as_stitches_is_not_a_plausible_fabric() {
        // Arrange
        // Act
        Exception? refusal = Record.Exception(() => new Gauge(20m, 45m));

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("rowsPer10Cm", argumentException.ParamName);
    }

}
