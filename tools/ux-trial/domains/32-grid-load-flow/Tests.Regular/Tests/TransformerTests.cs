using System.Numerics;

using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Tests.Regular;

public sealed class TransformerTests {

    [Fact]
    public void Joins_two_distinct_buses() {
        // Arrange
        Transformer anyTransformer = Any.Transformer().Generate();

        // Assert
        Assert.NotEqual(anyTransformer.HighVoltageBus, anyTransformer.LowVoltageBus);
    }

    [Fact]
    public void Refuses_a_transformer_from_a_bus_to_itself() {
        // Arrange
        BusId       anyBus         = Any.BusId().Generate();
        Transformer anyTransformer = Any.Transformer().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new Transformer(anyTransformer.Id, anyBus, anyBus, anyTransformer.Impedance, anyTransformer.RatingMva, anyTransformer.TapRatio));

        // Assert
        Assert.Equal("lowVoltageBus", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Refuses_a_blank_id(string id) {
        Assert.Throws<ArgumentException>(() => Any.Transformer().WithId(id).Generate());
    }

    [Fact]
    public void An_id_of_thirty_two_characters_is_the_longest_accepted() {
        // Arrange
        string anyLongestId = Any.String().Alpha().WithLength(32).Generate();
        string anyTooLongId = Any.String().Alpha().WithLength(33).Generate();

        // Act
        Transformer transformer = Any.Transformer().WithId(anyLongestId).Generate();
        Exception?  refusal     = Record.Exception(() => Any.Transformer().WithId(anyTooLongId).Generate());

        // Assert
        Assert.Equal(anyLongestId, transformer.Id);
        Assert.IsType<ArgumentException>(refusal);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(10_000.001)]
    [InlineData(double.NaN)]
    public void Refuses_a_rating_outside_zero_exclusive_to_ten_thousand(double ratingMva) {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Transformer().WithRatingMva(ratingMva).Generate());

        // Assert
        Assert.Equal("ratingMva", exception.ParamName);
    }

    [Theory]
    [InlineData(0.8999)]
    [InlineData(1.1001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Refuses_a_tap_ratio_outside_point_nine_to_one_point_one(double tapRatio) {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Transformer().WithTapRatio(tapRatio).Generate());

        // Assert
        Assert.Equal("tapRatio", exception.ParamName);
    }

    [Theory]
    [InlineData(0.9)]
    [InlineData(1.0)]
    [InlineData(1.1)]
    public void Accepts_a_tap_ratio_from_point_nine_to_one_point_one_inclusive(double tapRatio) {
        // Act
        Transformer transformer = Any.Transformer().WithTapRatio(tapRatio).Generate();

        // Assert
        Assert.Equal(tapRatio, transformer.TapRatio);
    }

    [Fact]
    public void Stamp_at_nominal_tap_is_a_plain_series_admittance() {
        // Arrange
        Impedance   impedance   = new Impedance(0.01, 0.08);
        Transformer transformer = Any.Transformer().WithImpedance(impedance).WithTapRatio(1.0).Generate();
        Complex     y           = impedance.Admittance;

        // Act
        (Complex ff, Complex ft, Complex tf, Complex tt) = transformer.Stamp();

        // Assert
        Assert.Equal(y, ff);
        Assert.Equal(-y, ft);
        Assert.Equal(-y, tf);
        Assert.Equal(y, tt);
    }

    [Fact]
    public void Stamp_scales_the_high_voltage_side_by_the_square_of_the_tap_and_the_mutual_terms_by_the_tap() {
        // Arrange
        Impedance   impedance   = new Impedance(0.01, 0.08);
        Transformer transformer = Any.Transformer().WithImpedance(impedance).WithTapRatio(1.05).Generate();
        Complex     y           = impedance.Admittance;

        // Act
        (Complex ff, Complex ft, Complex tf, Complex tt) = transformer.Stamp();

        // Assert
        Assert.Equal(y / (1.05 * 1.05), ff);
        Assert.Equal(-y / 1.05, ft);
        Assert.Equal(-y / 1.05, tf);
        Assert.Equal(y, tt);
    }

    [Fact]
    public void Raising_the_tap_moves_it_one_step_up() {
        // Arrange
        Transformer transformer = Any.Transformer().WithTapRatio(1.0).Generate();

        // Act
        transformer.RaiseTap();

        // Assert
        Assert.Equal(1.0125, transformer.TapRatio, 9);
    }

    [Fact]
    public void Lowering_the_tap_moves_it_one_step_down() {
        // Arrange
        Transformer transformer = Any.Transformer().WithTapRatio(1.0).Generate();

        // Act
        transformer.LowerTap();

        // Assert
        Assert.Equal(0.9875, transformer.TapRatio, 9);
    }

    [Fact]
    public void Raising_then_lowering_gives_back_the_tap_it_started_from() {
        // Arrange
        Transformer transformer = Any.Transformer().WithTapRatio(1.0375).Generate();

        // Act
        transformer.RaiseTap();
        transformer.LowerTap();

        // Assert
        Assert.Equal(1.0375, transformer.TapRatio, 9);
    }

    [Fact]
    public void The_tap_climbs_to_its_upper_stop_in_sixteen_steps_and_goes_no_further() {
        // Arrange
        Transformer transformer = Any.Transformer().WithTapRatio(0.9).Generate();

        // Act
        for (int step = 0; step < 16; step++) {
            transformer.RaiseTap();
        }

        // Assert
        Assert.Equal(1.1, transformer.TapRatio, 9);
        Assert.Throws<InvalidOperationException>(() => transformer.RaiseTap());
        Assert.Equal(1.1, transformer.TapRatio, 9);
    }

    [Fact]
    public void The_tap_descends_to_its_lower_stop_in_sixteen_steps_and_goes_no_further() {
        // Arrange
        Transformer transformer = Any.Transformer().WithTapRatio(1.1).Generate();

        // Act
        for (int step = 0; step < 16; step++) {
            transformer.LowerTap();
        }

        // Assert
        Assert.Equal(0.9, transformer.TapRatio, 9);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => transformer.LowerTap());
        Assert.Contains(transformer.Id, exception.Message);
        Assert.Equal(0.9, transformer.TapRatio, 9);
    }

    [Fact]
    public void A_tap_between_two_steps_cannot_step_past_a_stop() {
        // Arrange
        Transformer nearTop    = Any.Transformer().WithTapRatio(1.095).Generate();
        Transformer nearBottom = Any.Transformer().WithTapRatio(0.905).Generate();

        // Assert
        Assert.Throws<InvalidOperationException>(() => nearTop.RaiseTap());
        Assert.Throws<InvalidOperationException>(() => nearBottom.LowerTap());
    }

}
