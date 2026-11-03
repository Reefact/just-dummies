using JustDummies;

using Xunit;

namespace Drafting.Domain;

public sealed class StandardToleranceTests {

    private static readonly double[] UpperBounds = [3, 6, 10, 18, 30, 50, 80, 120, 180, 250, 315, 400, 500];

    // ISO 286-1 table 1, as written in the domain model: one row per grade IT6..IT11, one column per size range.
    private static readonly int[][] Expected = [
        [6, 8, 9, 11, 13, 16, 19, 22, 25, 29, 32, 36, 40],
        [10, 12, 15, 18, 21, 25, 30, 35, 40, 46, 52, 57, 63],
        [14, 18, 22, 27, 33, 39, 46, 54, 63, 72, 81, 89, 97],
        [25, 30, 36, 43, 52, 62, 74, 87, 100, 115, 130, 140, 155],
        [40, 48, 58, 70, 84, 100, 120, 140, 160, 185, 210, 230, 250],
        [60, 75, 90, 110, 130, 160, 190, 220, 250, 290, 320, 360, 400]
    ];

    public static TheoryData<double, int, int> UpperBoundsOfEveryRange() {
        TheoryData<double, int, int> data = new TheoryData<double, int, int>();
        for (int grade = IsoTolerance.FinestGrade; grade <= IsoTolerance.CoarsestGrade; grade++) {
            for (int range = 0; range < UpperBounds.Length; range++) {
                data.Add(UpperBounds[range], grade, Expected[grade - IsoTolerance.FinestGrade][range]);
            }
        }

        return data;
    }

    public static TheoryData<double, int, int> JustAboveEveryUpperBound() {
        TheoryData<double, int, int> data = new TheoryData<double, int, int>();
        for (int grade = IsoTolerance.FinestGrade; grade <= IsoTolerance.CoarsestGrade; grade++) {
            for (int range = 0; range < UpperBounds.Length - 1; range++) {
                data.Add(UpperBounds[range] + 0.001, grade, Expected[grade - IsoTolerance.FinestGrade][range + 1]);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(UpperBoundsOfEveryRange))]
    public void A_nominal_equal_to_the_upper_bound_of_a_range_belongs_to_that_range(double nominal, int grade, int expected) {
        // Act
        int micrometres = StandardTolerance.InMicrometres(nominal, grade);

        // Assert
        Assert.Equal(expected, micrometres);
    }

    [Theory]
    [MemberData(nameof(JustAboveEveryUpperBound))]
    public void A_nominal_just_above_an_upper_bound_belongs_to_the_next_range(double nominal, int grade, int expected) {
        // Act
        int micrometres = StandardTolerance.InMicrometres(nominal, grade);

        // Assert
        Assert.Equal(expected, micrometres);
    }

    [Fact]
    public void A_tiny_nominal_belongs_to_the_first_range() {
        // Arrange
        double anyTinyNominal = Any.Double().Between(0.0001, 3.0).Generate();
        int    anyGrade       = Any.Int32().Between(IsoTolerance.FinestGrade, IsoTolerance.CoarsestGrade).Generate();

        // Act
        int micrometres = StandardTolerance.InMicrometres(anyTinyNominal, anyGrade);

        // Assert
        Assert.Equal(Expected[anyGrade - IsoTolerance.FinestGrade][0], micrometres);
    }

    [Fact]
    public void A_coarser_grade_never_gives_a_tighter_tolerance_for_the_same_nominal() {
        // Arrange
        double anyNominal = Any.Double().Between(0.001, StandardTolerance.LargestNominalMillimetres).Generate();
        int    anyGrade   = Any.Int32().Between(IsoTolerance.FinestGrade, IsoTolerance.CoarsestGrade - 1).Generate();

        // Act
        int finer   = StandardTolerance.InMicrometres(anyNominal, anyGrade);
        int coarser = StandardTolerance.InMicrometres(anyNominal, anyGrade + 1);

        // Assert
        Assert.True(coarser > finer);
    }

    [Fact]
    public void A_nominal_of_zero_or_less_is_refused() {
        // Arrange
        double anyNonPositive = Any.Double().LessThanOrEqualTo(0).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => StandardTolerance.InMicrometres(anyNonPositive, 7));

        // Assert
        Assert.Equal("nominalMillimetres", exception.ParamName);
    }

    [Fact]
    public void A_nominal_above_the_largest_is_refused() {
        // Arrange
        double anyTooLarge = Any.Double().GreaterThan(StandardTolerance.LargestNominalMillimetres).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => StandardTolerance.InMicrometres(anyTooLarge, 7));

        // Assert
        Assert.Equal("nominalMillimetres", exception.ParamName);
    }

    [Fact]
    public void The_largest_nominal_is_still_covered() {
        // Act / Assert
        Assert.Equal(63, StandardTolerance.InMicrometres(StandardTolerance.LargestNominalMillimetres, 7));
    }

    [Fact]
    public void An_infinite_nominal_is_refused_by_the_range_check() {
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => StandardTolerance.InMicrometres(double.PositiveInfinity, 7));
        Assert.Throws<ArgumentOutOfRangeException>(() => StandardTolerance.InMicrometres(double.NegativeInfinity, 7));
    }

    [Fact]
    public void A_grade_outside_the_table_fails_with_an_index_error_rather_than_an_argument_error() {
        // Arrange
        int anyCoarserThanTheTable = Any.Int32().GreaterThan(IsoTolerance.CoarsestGrade).Generate();

        // Act / Assert
        Assert.Throws<IndexOutOfRangeException>(() => StandardTolerance.InMicrometres(25.0, anyCoarserThanTheTable));
    }

    [Fact]
    public void A_nan_nominal_is_not_refused_and_resolves_to_the_first_range() {
        // Act / Assert
        Assert.Equal(10, StandardTolerance.InMicrometres(double.NaN, 7));
    }

}
