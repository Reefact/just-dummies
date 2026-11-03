using JustDummies;

using Xunit;

namespace Drafting.Domain;

public sealed class IsoToleranceTests {

    [Theory]
    [InlineData("H")]
    [InlineData("JS")]
    public void An_upper_case_zone_designates_a_hole(string zone) {
        // Arrange
        int anyGrade = Any.Int32().Between(IsoTolerance.FinestGrade, IsoTolerance.CoarsestGrade).Generate();

        // Act
        IsoTolerance tolerance = new IsoTolerance(zone, anyGrade);

        // Assert
        Assert.True(tolerance.IsHole);
    }

    [Theory]
    [InlineData("h")]
    [InlineData("js")]
    public void A_lower_case_zone_designates_a_shaft(string zone) {
        // Arrange
        int anyGrade = Any.Int32().Between(IsoTolerance.FinestGrade, IsoTolerance.CoarsestGrade).Generate();

        // Act
        IsoTolerance tolerance = new IsoTolerance(zone, anyGrade);

        // Assert
        Assert.False(tolerance.IsHole);
    }

    [Fact]
    public void The_zone_and_the_grade_are_kept() {
        // Arrange
        IsoTolerance anyTolerance = Any.IsoTolerance().Generate();

        // Act
        IsoTolerance copy = new IsoTolerance(anyTolerance.Zone, anyTolerance.Grade);

        // Assert
        Assert.Equal(anyTolerance.Zone, copy.Zone);
        Assert.Equal(anyTolerance.Grade, copy.Grade);
        Assert.Equal(anyTolerance.IsHole, copy.IsHole);
    }

    [Theory]
    [InlineData(IsoTolerance.FinestGrade)]
    [InlineData(IsoTolerance.CoarsestGrade)]
    public void The_finest_and_the_coarsest_grades_are_accepted(int grade) {
        // Act
        IsoTolerance tolerance = Any.IsoTolerance().WithGrade(grade).Generate();

        // Assert
        Assert.Equal(grade, tolerance.Grade);
    }

    [Fact]
    public void A_grade_below_the_finest_is_refused() {
        // Arrange
        int anyTooFine = Any.Int32().LessThan(IsoTolerance.FinestGrade).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Any.IsoTolerance().WithGrade(anyTooFine).Generate());

        // Assert
        Assert.Equal("grade", exception.ParamName);
    }

    [Fact]
    public void A_grade_above_the_coarsest_is_refused() {
        // Arrange
        int anyTooCoarse = Any.Int32().GreaterThan(IsoTolerance.CoarsestGrade).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Any.IsoTolerance().WithGrade(anyTooCoarse).Generate());

        // Assert
        Assert.Equal("grade", exception.ParamName);
    }

    [Fact]
    public void A_null_zone_is_refused_as_a_null_argument() {
        // Arrange
        int anyGrade = Any.Int32().Between(IsoTolerance.FinestGrade, IsoTolerance.CoarsestGrade).Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new IsoTolerance(null!, anyGrade));

        // Assert
        Assert.Equal("zone", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("G")]
    [InlineData("g")]
    [InlineData("hs")]
    [InlineData("Js")]
    [InlineData("jS")]
    [InlineData("H ")]
    [InlineData(" h")]
    public void A_zone_other_than_the_four_supported_ones_is_refused(string zone) {
        // Arrange
        int anyGrade = Any.Int32().Between(IsoTolerance.FinestGrade, IsoTolerance.CoarsestGrade).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new IsoTolerance(zone, anyGrade));

        // Assert
        Assert.Equal("zone", exception.ParamName);
        Assert.IsNotType<ArgumentNullException>(exception);
    }

    [Fact]
    public void An_unsupported_zone_word_is_refused() {
        // Arrange
        string anyUnsupported = Any.String().Alpha().WithLengthBetween(3, 10).Generate();
        int    anyGrade       = Any.Int32().Between(IsoTolerance.FinestGrade, IsoTolerance.CoarsestGrade).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new IsoTolerance(anyUnsupported, anyGrade));
    }

    [Fact]
    public void The_grade_is_checked_before_the_zone() {
        // Act
        Exception exception = Assert.ThrowsAny<Exception>(() => new IsoTolerance("nonsense", 99));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }

    [Theory]
    [InlineData("H", 7, "H7")]
    [InlineData("h", 6, "h6")]
    [InlineData("JS", 11, "JS11")]
    [InlineData("js", 9, "js9")]
    public void A_tolerance_reads_as_its_zone_followed_by_its_grade(string zone, int grade, string expected) {
        // Assert
        Assert.Equal(expected, new IsoTolerance(zone, grade).ToString());
    }

    [Fact]
    public void Tolerances_with_the_same_zone_and_grade_are_equal_and_hash_alike() {
        // Arrange
        IsoTolerance anyTolerance = Any.IsoTolerance().Generate();
        IsoTolerance twin         = new IsoTolerance(anyTolerance.Zone, anyTolerance.Grade);

        // Assert
        Assert.Equal(anyTolerance, twin);
        Assert.True(anyTolerance.Equals((object)twin));
        Assert.Equal(anyTolerance.GetHashCode(), twin.GetHashCode());
    }

    [Fact]
    public void Zones_are_compared_case_sensitively() {
        // Assert
        Assert.NotEqual(new IsoTolerance("H", 7), new IsoTolerance("h", 7));
        Assert.NotEqual(new IsoTolerance("JS", 7), new IsoTolerance("js", 7));
    }

    [Fact]
    public void Tolerances_differing_by_grade_are_not_equal() {
        // Assert
        Assert.NotEqual(new IsoTolerance("H", 7), new IsoTolerance("H", 8));
    }

}
