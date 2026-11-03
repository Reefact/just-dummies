namespace RadiocarbonChronology.Tests.Discovery;

public sealed class OccupationEstimateTests {

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void A_blank_layer_code_is_refused(string? layerCode) {
        // Arrange
        CalibratedDate anyDate = Any.CalibratedDate().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new OccupationEstimate(layerCode!, anyDate));

        // Assert
        Assert.Equal("layerCode", refusal.ParamName);
    }

    [Fact]
    public void An_estimate_needs_a_date() {
        // Arrange
        string anyLayerCode = Any.StratigraphicLayer().Generate().Code;

        // Assert
        Assert.Throws<ArgumentNullException>(() => new OccupationEstimate(anyLayerCode, null!));
    }

    [Fact]
    public void The_plausible_intervals_hold_954_per_mille_of_the_date() {
        // Arrange
        string         anyLayerCode = Any.StratigraphicLayer().Generate().Code;
        CalibratedDate date         = new CalibratedDate(100, [0.02, 0.48, 0.48, 0.02]);

        // Act
        OccupationEstimate estimate = new OccupationEstimate(anyLayerCode, date);

        // Assert
        Assert.Equal(0.954, OccupationEstimate.CredibleLevel);
        Assert.Equal([new CalendarInterval(101, 102)], estimate.PlausibleIntervals);
        Assert.False(estimate.IsMultimodal);
        Assert.Equal(new CalendarInterval(101, 102), estimate.Envelope);
    }

    [Fact]
    public void A_date_with_two_peaks_is_multimodal_and_its_envelope_spans_both() {
        // Arrange
        string         anyLayerCode = Any.StratigraphicLayer().Generate().Code;
        CalibratedDate date         = new CalibratedDate(-100, [0.48, 0.02, 0.02, 0.48]);

        // Act
        OccupationEstimate estimate = new OccupationEstimate(anyLayerCode, date);

        // Assert
        Assert.Equal([new CalendarInterval(-100, -100), new CalendarInterval(-97, -97)], estimate.PlausibleIntervals);
        Assert.True(estimate.IsMultimodal);
        Assert.Equal(new CalendarInterval(-100, -97), estimate.Envelope);
    }

}
