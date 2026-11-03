using System.Numerics;

using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Domain.Tests.Expert;

public sealed class TransformerTests {

    [Fact]
    public void A_transformer_keeps_which_end_is_high_voltage() {
        BusId high = new BusId("HV");
        BusId low  = new BusId("LV");

        Transformer transformer = Any.Transformer().WithHighVoltageBus(high).WithLowVoltageBus(low).Generate();

        Assert.Equal(high, transformer.HighVoltageBus);
        Assert.Equal(low, transformer.LowVoltageBus);
    }

    [Fact]
    public void A_transformer_cannot_connect_a_bus_to_itself() {
        AnyTransformer loop = Any.Transformer().WithHighVoltageBus(new BusId("SAME")).WithLowVoltageBus(new BusId("SAME"));

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => loop.Generate());

        Assert.Equal("lowVoltageBus", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.9, true)]
    [InlineData(1.1, true)]
    [InlineData(0.8999, false)]
    [InlineData(1.1001, false)]
    [InlineData(double.NaN, false)]
    public void A_tap_ratio_is_accepted_from_point_nine_to_one_point_one(double tap, bool accepted) {
        AnyTransformer transformer = Any.Transformer().WithTapRatio(tap);

        if (accepted) {
            Assert.Equal(tap, transformer.Generate().TapRatio);
        } else {
            ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => transformer.Generate());
            Assert.Equal("tapRatio", refusal.ParamName);
        }
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-5d)]
    [InlineData(10_000.0001)]
    public void A_rating_outside_zero_exclusive_to_ten_thousand_mva_is_refused(double rating) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Transformer().WithRatingMva(rating).Generate());

        Assert.Equal("ratingMva", refusal.ParamName);
    }

    [Fact]
    public void An_identifier_longer_than_thirty_two_characters_is_refused() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Transformer().WithId(new string('T', 33)).Generate());

        Assert.Equal("id", refusal.ParamName);
    }

    [Fact]
    public void Raising_the_tap_moves_it_one_step_up() {
        Transformer transformer = Any.Transformer().WithTapRatio(1.0).Generate();

        transformer.RaiseTap();

        Assert.Equal(1.0125, transformer.TapRatio, 9);
    }

    [Fact]
    public void Lowering_the_tap_moves_it_one_step_down() {
        Transformer transformer = Any.Transformer().WithTapRatio(1.0).Generate();

        transformer.LowerTap();

        Assert.Equal(0.9875, transformer.TapRatio, 9);
    }

    [Fact]
    public void Raising_then_lowering_the_tap_brings_it_back() {
        Transformer transformer = Any.Transformer().WithTapRatio(1.0375).Generate();

        transformer.RaiseTap();
        transformer.LowerTap();

        Assert.Equal(1.0375, transformer.TapRatio, 9);
    }

    [Fact]
    public void The_upper_end_stop_is_reached_after_exactly_eight_steps_from_nominal_and_cannot_be_passed() {
        Transformer transformer = Any.Transformer().WithTapRatio(1.0).Generate();

        for (int step = 0; step < 8; step++) {
            transformer.RaiseTap();
        }

        Assert.Equal(1.1, transformer.TapRatio, 9);
        Assert.Throws<InvalidOperationException>(() => transformer.RaiseTap());
        Assert.Equal(1.1, transformer.TapRatio, 9);
    }

    [Fact]
    public void The_lower_end_stop_is_reached_after_exactly_eight_steps_from_nominal_and_cannot_be_passed() {
        Transformer transformer = Any.Transformer().WithTapRatio(1.0).Generate();

        for (int step = 0; step < 8; step++) {
            transformer.LowerTap();
        }

        Assert.Equal(0.9, transformer.TapRatio, 9);
        Assert.Throws<InvalidOperationException>(() => transformer.LowerTap());
        Assert.Equal(0.9, transformer.TapRatio, 9);
    }

    [Fact]
    public void A_tap_off_the_step_grid_still_refuses_a_step_that_would_leave_the_range() {
        Transformer transformer = Any.Transformer().WithTapRatio(1.095).Generate();

        Assert.Throws<InvalidOperationException>(() => transformer.RaiseTap());
        Assert.Equal(1.095, transformer.TapRatio);
    }

    [Fact]
    public void The_refusal_at_an_end_stop_names_the_transformer() {
        Transformer transformer = Any.Transformer().WithTapRatio(1.1).Generate();

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => transformer.RaiseTap());

        Assert.Contains(transformer.Id, refusal.Message);
    }

    [Fact]
    public void At_nominal_tap_the_stamp_is_the_series_admittance_whatever_the_side() {
        Transformer transformer = Any.Transformer().WithImpedance(new Impedance(0.1, 0.2)).WithTapRatio(1.0).Generate();

        (Complex hvhv, Complex hvlv, Complex lvhv, Complex lvlv) = transformer.Stamp();

        Assert.Equal(new Complex(2, -4), Rounded(hvhv));
        Assert.Equal(new Complex(-2, 4), Rounded(hvlv));
        Assert.Equal(Rounded(hvlv), Rounded(lvhv));
        Assert.Equal(new Complex(2, -4), Rounded(lvlv));
    }

    [Fact]
    public void An_off_nominal_tap_divides_the_high_voltage_diagonal_by_its_square_and_the_mutual_terms_by_itself() {
        Transformer transformer = Any.Transformer().WithImpedance(new Impedance(0.1, 0.2)).WithTapRatio(1.1).Generate();

        (Complex hvhv, Complex hvlv, Complex lvhv, Complex lvlv) = transformer.Stamp();

        Complex y = new Complex(2, -4);
        Assert.Equal(Rounded(y / 1.21), Rounded(hvhv));
        Assert.Equal(Rounded(-y / 1.1), Rounded(hvlv));
        Assert.Equal(Rounded(hvlv), Rounded(lvhv));
        Assert.Equal(Rounded(y), Rounded(lvlv));
    }

    private static Complex Rounded(Complex value) {
        return new Complex(Math.Round(value.Real, 9), Math.Round(value.Imaginary, 9));
    }

}
