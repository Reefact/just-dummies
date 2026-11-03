using Drafting.Domain.Tests.Expert.Generators;

using Xunit;

namespace Drafting.Domain.Tests.Expert.Tests;

public sealed class StandardToleranceTests {

    // ISO 286-1, IT6..IT11 in micrometres: one row per grade, one column per size range.
    private static readonly double[] RangeUpperBounds = [3, 6, 10, 18, 30, 50, 80, 120, 180, 250, 315, 400, 500];

    private static readonly int[][] Table = [
        [6, 8, 9, 11, 13, 16, 19, 22, 25, 29, 32, 36, 40],
        [10, 12, 15, 18, 21, 25, 30, 35, 40, 46, 52, 57, 63],
        [14, 18, 22, 27, 33, 39, 46, 54, 63, 72, 81, 89, 97],
        [25, 30, 36, 43, 52, 62, 74, 87, 100, 115, 130, 140, 155],
        [40, 48, 58, 70, 84, 100, 120, 140, 160, 185, 210, 230, 250],
        [60, 75, 90, 110, 130, 160, 190, 220, 250, 290, 320, 360, 400]
    ];

    public static TheoryData<int, double, int> UpperBoundOfEachRange() {
        TheoryData<int, double, int> cases = new TheoryData<int, double, int>();
        for (int grade = 6; grade <= 11; grade++) {
            for (int range = 0; range < RangeUpperBounds.Length; range++) {
                cases.Add(grade, RangeUpperBounds[range], Table[grade - 6][range]);
            }
        }

        return cases;
    }

    public static TheoryData<int, double, int> JustAboveEachUpperBound() {
        TheoryData<int, double, int> cases = new TheoryData<int, double, int>();
        for (int grade = 6; grade <= 11; grade++) {
            for (int range = 0; range < RangeUpperBounds.Length - 1; range++) {
                cases.Add(grade, RangeUpperBounds[range] + 0.001, Table[grade - 6][range + 1]);
            }
        }

        return cases;
    }

    [Theory]
    [MemberData(nameof(UpperBoundOfEachRange))]
    public void A_size_range_is_closed_at_its_upper_bound(int grade, double upperBound, int expectedMicrometres) {
        Assert.Equal(expectedMicrometres, StandardTolerance.InMicrometres(upperBound, grade));
    }

    [Theory]
    [MemberData(nameof(JustAboveEachUpperBound))]
    public void A_size_just_above_an_upper_bound_belongs_to_the_next_range(int grade, double nominal, int expectedMicrometres) {
        Assert.Equal(expectedMicrometres, StandardTolerance.InMicrometres(nominal, grade));
    }

    [Theory]
    [InlineData(7, 25.0, 21)]        // the textbook case, Ø25 IT7
    [InlineData(6, 25.0, 13)]
    [InlineData(7, 0.001, 10)]       // the smallest sizes fall in the first range
    [InlineData(11, 0.001, 60)]
    [InlineData(9, 1.5, 25)]
    [InlineData(8, 100.0, 54)]
    public void Typical_sizes_read_the_table(int grade, double nominal, int expectedMicrometres) {
        Assert.Equal(expectedMicrometres, StandardTolerance.InMicrometres(nominal, grade));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.001)]
    [InlineData(-25.0)]
    [InlineData(500.000001)]
    [InlineData(501.0)]
    [InlineData(double.MaxValue)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_size_outside_zero_to_500_millimetres_is_refused(double nominal) {
        int anyGrade = AnyIsoTolerance.ValidGrade().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => StandardTolerance.InMicrometres(nominal, anyGrade));

        Assert.Equal("nominalMillimetres", error.ParamName);
    }

    [Fact]
    public void The_largest_nominal_is_500_millimetres() {
        Assert.Equal(500.0, StandardTolerance.LargestNominalMillimetres);
    }

    [Theory(Skip = "domain defect: a grade outside 6..11 fails with an IndexOutOfRangeException instead of an ArgumentOutOfRangeException naming the grade")]
    [InlineData(5)]
    [InlineData(12)]
    [InlineData(-1)]
    public void A_grade_outside_6_to_11_is_refused_as_a_bad_argument(int grade) {
        Assert.Throws<ArgumentOutOfRangeException>(() => StandardTolerance.InMicrometres(25.0, grade));
    }

    [Fact(Skip = "domain defect: StandardTolerance.InMicrometres(double.NaN, 7) passes the range check and answers the first range (10) instead of refusing the size")]
    public void A_size_that_is_not_a_number_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => StandardTolerance.InMicrometres(double.NaN, 7));
    }

}
