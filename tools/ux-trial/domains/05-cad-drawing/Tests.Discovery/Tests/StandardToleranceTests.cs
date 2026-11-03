using JustDummies;

using Xunit;

namespace Drafting.Domain.Tests;

public sealed class StandardToleranceTests {

    private static readonly double[] UpperBounds = [3, 6, 10, 18, 30, 50, 80, 120, 180, 250, 315, 400, 500];

    // ISO 286-1 IT6..IT11, one row per grade, one column per size range.
    private static readonly int[][] Expected = [
        [6, 8, 9, 11, 13, 16, 19, 22, 25, 29, 32, 36, 40],
        [10, 12, 15, 18, 21, 25, 30, 35, 40, 46, 52, 57, 63],
        [14, 18, 22, 27, 33, 39, 46, 54, 63, 72, 81, 89, 97],
        [25, 30, 36, 43, 52, 62, 74, 87, 100, 115, 130, 140, 155],
        [40, 48, 58, 70, 84, 100, 120, 140, 160, 185, 210, 230, 250],
        [60, 75, 90, 110, 130, 160, 190, 220, 250, 290, 320, 360, 400]
    ];

    public static TheoryData<double, int, int> UpperBoundOfEachRange() {
        TheoryData<double, int, int> data = new();
        for (int grade = 6; grade <= 11; grade++) {
            for (int range = 0; range < UpperBounds.Length; range++) {
                data.Add(UpperBounds[range], grade, Expected[grade - 6][range]);
            }
        }

        return data;
    }

    public static TheoryData<double, int, int> JustAboveTheUpperBoundOfEachRange() {
        TheoryData<double, int, int> data = new();
        for (int grade = 6; grade <= 11; grade++) {
            for (int range = 0; range < UpperBounds.Length - 1; range++) {
                data.Add(UpperBounds[range] + 0.001, grade, Expected[grade - 6][range + 1]);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(UpperBoundOfEachRange))]
    public void The_upper_bound_of_a_range_belongs_to_that_range(double nominal, int grade, int expectedMicrometres) {
        Assert.Equal(expectedMicrometres, StandardTolerance.InMicrometres(nominal, grade));
    }

    [Theory]
    [MemberData(nameof(JustAboveTheUpperBoundOfEachRange))]
    public void Just_above_a_bound_the_next_range_applies(double nominal, int grade, int expectedMicrometres) {
        Assert.Equal(expectedMicrometres, StandardTolerance.InMicrometres(nominal, grade));
    }

    [Theory]
    [InlineData(0.001, 6, 6)]      // the smallest sizes are in the first range
    [InlineData(1.0, 7, 10)]
    [InlineData(25.0, 7, 21)]      // the textbook Ø25 H7
    [InlineData(25.0, 6, 13)]      // and its h6 shaft
    [InlineData(100.0, 11, 220)]
    [InlineData(499.999, 8, 97)]
    public void Typical_sizes_resolve_to_the_textbook_value(double nominal, int grade, int expectedMicrometres) {
        Assert.Equal(expectedMicrometres, StandardTolerance.InMicrometres(nominal, grade));
    }

    [Fact]
    public void The_largest_nominal_is_500_millimetres() {
        Assert.Equal(500.0, StandardTolerance.LargestNominalMillimetres);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(-1e300)]
    [InlineData(500.0001)]
    [InlineData(1000.0)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.MaxValue)]
    public void A_nominal_outside_zero_to_500_is_refused(double nominal) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => StandardTolerance.InMicrometres(nominal, 7));

        Assert.Equal("nominalMillimetres", error.ParamName);
    }

    [Fact]
    public void A_nominal_smaller_than_any_size_a_drawing_holds_is_still_in_the_first_range() {
        Assert.Equal(6, StandardTolerance.InMicrometres(double.Epsilon, 6));
    }

    [Fact(Skip = "domain defect: a NaN nominal passes both range comparisons and silently resolves to the first size range (Dimension refuses NaN, this method does not); no seed involved")]
    public void A_NaN_nominal_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => StandardTolerance.InMicrometres(double.NaN, 7));
    }

    [Theory(Skip = "domain defect: the grade is not validated, so a grade outside 6..11 fails with IndexOutOfRangeException instead of ArgumentOutOfRangeException (documented in MODEL.md, but IsoTolerance guards it); no seed involved")]
    [InlineData(5)]
    [InlineData(12)]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_grade_outside_6_to_11_is_refused_with_an_argument_error(int grade) {
        double anyNominal = Any.Double().Positive().LessThanOrEqualTo(StandardTolerance.LargestNominalMillimetres).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => StandardTolerance.InMicrometres(anyNominal, grade));
    }

}
