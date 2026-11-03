using JustDummies;

using RadiocarbonChronology.Domain;

namespace RadiocarbonChronology.Tests.Expert;

public sealed class OccupationEstimateTests {

    [Fact]
    public void An_estimate_keeps_its_layer_code_and_its_date() {
        // Arrange
        CalibratedDate date = new CalibratedDate(-100, [0.25, 0.5, 0.25]);

        // Act
        OccupationEstimate estimate = new OccupationEstimate("US7", date);

        // Assert
        Assert.Equal("US7", estimate.LayerCode);
        Assert.Same(date, estimate.Date);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_layer_code_is_refused(string? blank) {
        // Arrange
        CalibratedDate anyDate = Any.CalibratedDate().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OccupationEstimate(blank!, anyDate));

        // Assert
        Assert.Equal("layerCode", refusal.ParamName);
    }

    [Fact]
    public void A_missing_date_is_refused() {
        // Arrange
        string anyLayerCode = AnyStratigraphicLayer.ValidCode().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new OccupationEstimate(anyLayerCode, null!));

        // Assert
        Assert.Equal("date", refusal.ParamName);
    }

    [Fact]
    public void The_plausible_interval_leaves_out_the_tails_beyond_ninety_five_point_four_percent() {
        // Arrange: the two end years hold 2% each, 4% together, so the middle three hold 96%
        CalibratedDate date = new CalibratedDate(1_000, [0.02, 0.03, 0.9, 0.03, 0.02]);

        // Act
        OccupationEstimate estimate = new OccupationEstimate("US1", date);

        // Assert
        Assert.Equal([new CalendarInterval(1_001, 1_003)], estimate.PlausibleIntervals);
    }

    [Fact]
    public void A_date_with_one_peak_is_not_multimodal_and_its_envelope_is_its_interval() {
        // Arrange
        CalibratedDate date = new CalibratedDate(1_000, [0.02, 0.03, 0.9, 0.03, 0.02]);

        // Act
        OccupationEstimate estimate = new OccupationEstimate("US1", date);

        // Assert
        Assert.False(estimate.IsMultimodal);
        Assert.Equal(new CalendarInterval(1_001, 1_003), estimate.Envelope);
    }

    [Fact]
    public void A_date_with_two_peaks_is_multimodal_and_its_envelope_spans_them_both() {
        // Arrange: years -500 and -497 hold 0.45 and 0.5, the trough years 0.03 and 0.02
        CalibratedDate date = new CalibratedDate(-500, [0.45, 0.03, 0.02, 0.5]);

        // Act
        OccupationEstimate estimate = new OccupationEstimate("US1", date);

        // Assert
        Assert.Equal([new CalendarInterval(-500, -499), new CalendarInterval(-497, -497)], estimate.PlausibleIntervals);
        Assert.True(estimate.IsMultimodal);
        Assert.Equal(new CalendarInterval(-500, -497), estimate.Envelope);
    }

    [Fact]
    public void The_credible_level_is_ninety_five_point_four_percent() {
        // Assert
        Assert.Equal(0.954, OccupationEstimate.CredibleLevel);
    }

}
