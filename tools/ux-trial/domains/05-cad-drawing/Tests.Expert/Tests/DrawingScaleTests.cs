using Drafting.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Drafting.Domain.Tests.Expert.Tests;

public sealed class DrawingScaleTests {

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(5, 1)]
    [InlineData(1, 2)]
    [InlineData(1, 50)]
    public void A_scale_has_a_one_on_one_side(int numerator, int denominator) {
        DrawingScale scale = new DrawingScale(numerator, denominator);

        Assert.Equal(numerator, scale.Numerator);
        Assert.Equal(denominator, scale.Denominator);
    }

    [Theory]
    [InlineData(7, 1)]            // no preferred series: any integer is accepted on the other side
    [InlineData(1, 3)]
    [InlineData(1, 1_000_000)]
    [InlineData(int.MaxValue, 1)]
    [InlineData(1, int.MaxValue)]
    public void Any_integer_is_accepted_on_the_side_that_is_not_one(int numerator, int denominator) {
        Assert.Equal($"{numerator}:{denominator}", new DrawingScale(numerator, denominator).ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_numerator_below_one_is_refused(int numerator) {
        int anyDenominator = AnyDrawingScale.ValidTerm().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new DrawingScale(numerator, anyDenominator));

        Assert.Equal("numerator", error.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_denominator_below_one_is_refused(int denominator) {
        int anyNumerator = AnyDrawingScale.ValidTerm().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new DrawingScale(anyNumerator, denominator));

        Assert.Equal("denominator", error.ParamName);
    }

    [Fact]
    public void The_numerator_is_checked_before_the_denominator() {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new DrawingScale(0, 0));

        Assert.Equal("numerator", error.ParamName);
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(2, 3)]
    [InlineData(5, 2)]
    [InlineData(100, 100)]
    public void A_scale_with_no_one_on_either_side_is_refused(int numerator, int denominator) {
        // Plain ArgumentException: Assert.Throws<T> is exact, so the out-of-range flavour would fail this.
        ArgumentException error = Assert.Throws<ArgumentException>(() => new DrawingScale(numerator, denominator));

        Assert.Equal("denominator", error.ParamName);
    }

    [Theory]
    [InlineData(2, 1, 10.0, 20.0)]      // enlargement
    [InlineData(1, 4, 100.0, 25.0)]     // reduction
    [InlineData(1, 1, 123.5, 123.5)]
    [InlineData(5, 1, 0.5, 2.5)]
    [InlineData(1, 50, 1_000.0, 20.0)]
    [InlineData(2, 1, 0.0, 0.0)]
    public void A_real_length_is_scaled_by_numerator_over_denominator(int numerator, int denominator, double realMillimetres, double expectedPaperMillimetres) {
        DrawingScale scale = new DrawingScale(numerator, denominator);

        double paper = scale.PaperLengthOf(realMillimetres);

        Assert.Equal(expectedPaperMillimetres, paper, 9);
    }

    [Theory]
    [InlineData(1, 1, "1:1")]
    [InlineData(5, 1, "5:1")]
    [InlineData(1, 20, "1:20")]
    public void A_scale_reads_as_numerator_colon_denominator(int numerator, int denominator, string expected) {
        Assert.Equal(expected, new DrawingScale(numerator, denominator).ToString());
    }

}
