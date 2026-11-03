using System.Numerics;

using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Domain.Tests.Expert;

public sealed class LineTests {

    [Fact]
    public void A_new_line_is_in_service() {
        Line line = Any.Line().Generate();

        Assert.True(line.InService);
    }

    [Fact]
    public void A_line_keeps_the_ends_it_was_given_in_order() {
        BusId from = new BusId("FROM");
        BusId to   = new BusId("TO");

        Line line = Any.Line().WithFromBus(from).WithToBus(to).Generate();

        Assert.Equal(from, line.FromBus);
        Assert.Equal(to, line.ToBus);
    }

    [Fact]
    public void A_line_cannot_loop_back_on_its_own_bus() {
        BusId bus = new BusId("LOOP");
        AnyLine loop = Any.Line().WithFromBus(bus).WithToBus(new BusId("LOOP"));

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => loop.Generate());

        Assert.Equal("toBus", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void A_blank_identifier_is_refused(string id) {
        Assert.Throws<ArgumentException>(() => Any.Line().WithId(id).Generate());
    }

    [Fact]
    public void An_identifier_of_thirty_two_characters_is_accepted_and_thirty_three_refused() {
        Line accepted = Any.Line().WithId(new string('L', 32)).Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Line().WithId(new string('L', 33)).Generate());

        Assert.Equal(new string('L', 32), accepted.Id);
        Assert.Equal("id", refusal.ParamName);
    }

    [Theory]
    [InlineData(0d, true)]
    [InlineData(5d, true)]
    [InlineData(-0.0001, false)]
    [InlineData(5.0001, false)]
    [InlineData(double.NaN, false)]
    public void Charging_susceptance_is_accepted_from_zero_to_five_pu(double susceptance, bool accepted) {
        AnyLine line = Any.Line().WithChargingSusceptancePu(susceptance);

        if (accepted) {
            Assert.Equal(susceptance, line.Generate().ChargingSusceptancePu);
        } else {
            ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => line.Generate());
            Assert.Equal("chargingSusceptancePu", refusal.ParamName);
        }
    }

    [Theory]
    [InlineData(0.0001, true)]
    [InlineData(10_000d, true)]
    [InlineData(0d, false)]
    [InlineData(-1d, false)]
    [InlineData(10_000.0001, false)]
    [InlineData(double.PositiveInfinity, false)]
    public void A_rating_is_accepted_above_zero_up_to_ten_thousand_mva(double rating, bool accepted) {
        AnyLine line = Any.Line().WithRatingMva(rating);

        if (accepted) {
            Assert.Equal(rating, line.Generate().RatingMva);
        } else {
            ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => line.Generate());
            Assert.Equal("ratingMva", refusal.ParamName);
        }
    }

    [Fact]
    public void A_missing_end_or_impedance_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Line().WithFromBus((BusId)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Any.Line().WithToBus((BusId)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Any.Line().WithImpedance((Impedance)null!).Generate());
    }

    [Fact]
    public void Without_charging_the_stamp_is_the_series_admittance_on_the_diagonal_and_its_opposite_off_it() {
        Line line = Any.Line().WithImpedance(new Impedance(0.1, 0.2)).WithChargingSusceptancePu(0).Generate();

        (Complex ff, Complex ft, Complex tf, Complex tt) = line.Stamp();

        Assert.Equal(2.0, ff.Real, 12);
        Assert.Equal(-4.0, ff.Imaginary, 12);
        Assert.Equal(-2.0, ft.Real, 12);
        Assert.Equal(4.0, ft.Imaginary, 12);
        Assert.Equal(ft, tf);
        Assert.Equal(ff, tt);
    }

    [Fact]
    public void Charging_adds_half_its_susceptance_to_each_diagonal_term_and_nothing_off_the_diagonal() {
        Line line = Any.Line().WithImpedance(new Impedance(0.1, 0.2)).WithChargingSusceptancePu(0.06).Generate();

        (Complex ff, Complex ft, Complex tf, Complex tt) = line.Stamp();

        Assert.Equal(2.0, ff.Real, 12);
        Assert.Equal(-4.0 + 0.03, ff.Imaginary, 12);
        Assert.Equal(ff, tt);
        Assert.Equal(new Complex(-2.0, 4.0).Real, ft.Real, 12);
        Assert.Equal(4.0, ft.Imaginary, 12);
        Assert.Equal(ft, tf);
    }

}
