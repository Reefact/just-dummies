using JustDummies;

using Xunit;

namespace Drafting.Domain.Tests;

public sealed class IsoToleranceTests {

    [Theory]
    [InlineData("H", true)]
    [InlineData("JS", true)]
    [InlineData("h", false)]
    [InlineData("js", false)]
    public void The_zone_decides_whether_the_class_is_a_hole(string zone, bool isHole) {
        // Arrange
        int anyGrade = Any.Int32().Between(IsoTolerance.FinestGrade, IsoTolerance.CoarsestGrade).Generate();

        // Act
        IsoTolerance tolerance = new IsoTolerance(zone, anyGrade);

        // Assert
        Assert.Equal(isHole, tolerance.IsHole);
        Assert.Equal(zone, tolerance.Zone);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    public void Every_grade_from_6_to_11_is_accepted(int grade) {
        // Act
        IsoTolerance tolerance = new IsoTolerance("H", grade);

        // Assert
        Assert.Equal(grade, tolerance.Grade);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(12)]
    [InlineData(0)]
    [InlineData(-7)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_grade_outside_6_to_11_is_refused(int grade) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new IsoTolerance("H", grade));

        Assert.Equal("grade", error.ParamName);
    }

    [Fact]
    public void The_grade_limits_are_published_as_constants() {
        Assert.Equal(6, IsoTolerance.FinestGrade);
        Assert.Equal(11, IsoTolerance.CoarsestGrade);
    }

    [Fact]
    public void A_null_zone_is_refused_as_null() {
        Assert.Throws<ArgumentNullException>(() => new IsoTolerance(null!, 7));
    }

    [Theory]
    [InlineData("")]
    [InlineData("G")]
    [InlineData("Js")]            // case-sensitive
    [InlineData("jS")]
    [InlineData("hh")]
    [InlineData(" H")]
    [InlineData("H ")]
    [InlineData("k")]
    [InlineData("JS7")]
    public void An_unsupported_zone_is_refused(string zone) {
        ArgumentException error = Assert.Throws<ArgumentException>(() => new IsoTolerance(zone, 7));

        Assert.IsNotType<ArgumentOutOfRangeException>(error);
        Assert.Equal("zone", error.ParamName);
    }

    [Fact]
    public void Null_is_checked_before_the_grade() {
        Assert.Throws<ArgumentNullException>(() => new IsoTolerance(null!, 99));
    }

    [Fact]
    public void The_grade_is_checked_before_the_zone() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IsoTolerance("nonsense", 99));
    }

    [Theory]
    [InlineData("H", 7, "H7")]
    [InlineData("h", 6, "h6")]
    [InlineData("JS", 11, "JS11")]
    [InlineData("js", 9, "js9")]
    public void ToString_writes_zone_then_grade(string zone, int grade, string expected) {
        Assert.Equal(expected, new IsoTolerance(zone, grade).ToString());
    }

    [Fact]
    public void Two_classes_with_the_same_zone_and_grade_are_equal() {
        // Arrange
        IsoTolerance first  = new IsoTolerance("H", 7);
        IsoTolerance second = new IsoTolerance("H", 7);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equality_is_case_sensitive_on_the_zone() {
        Assert.NotEqual(new IsoTolerance("H", 7), new IsoTolerance("h", 7));
        Assert.NotEqual(new IsoTolerance("JS", 7), new IsoTolerance("js", 7));
    }

    [Fact]
    public void Classes_of_different_grades_are_not_equal() {
        Assert.NotEqual(new IsoTolerance("H", 7), new IsoTolerance("H", 8));
    }

    [Fact]
    public void A_class_is_not_equal_to_null() {
        Assert.False(new IsoTolerance("H", 7).Equals((IsoTolerance?)null));
        Assert.False(new IsoTolerance("H", 7).Equals((object?)null));
    }

}
