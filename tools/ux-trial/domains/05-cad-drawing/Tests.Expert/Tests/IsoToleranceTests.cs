using Drafting.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Drafting.Domain.Tests.Expert.Tests;

public sealed class IsoToleranceTests {

    [Theory]
    [InlineData("H", true)]
    [InlineData("JS", true)]
    [InlineData("h", false)]
    [InlineData("js", false)]
    public void The_case_of_the_zone_tells_a_hole_from_a_shaft(string zone, bool expectedIsHole) {
        IsoTolerance tolerance = Any.IsoTolerance().WithZone(zone).Generate();

        Assert.Equal(zone, tolerance.Zone);
        Assert.Equal(expectedIsHole, tolerance.IsHole);
    }

    [Theory]
    [InlineData(IsoTolerance.FinestGrade)]
    [InlineData(IsoTolerance.CoarsestGrade)]
    [InlineData(8)]
    public void A_grade_from_6_to_11_is_accepted(int grade) {
        IsoTolerance tolerance = Any.IsoTolerance().WithGrade(grade).Generate();

        Assert.Equal(grade, tolerance.Grade);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(12)]
    [InlineData(0)]
    [InlineData(-6)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_grade_outside_6_to_11_is_refused(int grade) {
        string anyZone = AnyIsoTolerance.ValidZone().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new IsoTolerance(anyZone, grade));

        Assert.Equal("grade", error.ParamName);
    }

    [Fact]
    public void A_zone_cannot_be_null() {
        int anyGrade = AnyIsoTolerance.ValidGrade().Generate();

        Assert.Throws<ArgumentNullException>(() => new IsoTolerance(null!, anyGrade));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("K")]
    [InlineData("G")]
    [InlineData("Js")]     // zones are case-sensitive, mixed case included
    [InlineData("jS")]
    [InlineData("hs")]
    [InlineData("HS")]
    [InlineData("H ")]
    [InlineData(" h")]
    [InlineData("H7")]
    [InlineData("JS7")]
    public void A_zone_other_than_H_JS_h_or_js_is_refused(string zone) {
        int anyGrade = AnyIsoTolerance.ValidGrade().Generate();

        // ArgumentException itself, so neither the null nor the range flavour passes.
        ArgumentException error = Assert.Throws<ArgumentException>(() => new IsoTolerance(zone, anyGrade));

        Assert.Equal("zone", error.ParamName);
    }

    [Fact]
    public void The_zone_is_checked_after_the_grade() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IsoTolerance("nonsense", 99));
    }

    [Fact]
    public void A_null_zone_is_reported_before_a_bad_grade() {
        Assert.Throws<ArgumentNullException>(() => new IsoTolerance(null!, 99));
    }

    [Theory]
    [InlineData("H", 7, "H7")]
    [InlineData("h", 6, "h6")]
    [InlineData("JS", 11, "JS11")]
    [InlineData("js", 9, "js9")]
    public void A_tolerance_reads_as_its_zone_followed_by_its_grade(string zone, int grade, string expected) {
        IsoTolerance tolerance = Any.IsoTolerance().WithZone(zone).WithGrade(grade).Generate();

        Assert.Equal(expected, tolerance.ToString());
    }

    [Fact]
    public void Tolerances_with_the_same_zone_and_grade_are_equal() {
        IsoTolerance first  = new IsoTolerance("H", 7);
        IsoTolerance second = new IsoTolerance("H", 7);

        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Theory]
    [InlineData("H", 7, "H", 8)]
    [InlineData("H", 7, "JS", 7)]
    [InlineData("H", 7, "h", 7)]      // equality is case-sensitive
    [InlineData("JS", 7, "js", 7)]
    public void Tolerances_differing_by_zone_or_grade_are_not_equal(string zone, int grade, string otherZone, int otherGrade) {
        Assert.NotEqual(new IsoTolerance(zone, grade), new IsoTolerance(otherZone, otherGrade));
    }

    [Fact]
    public void A_tolerance_is_not_equal_to_null_or_to_another_kind_of_object() {
        IsoTolerance tolerance = Any.IsoTolerance().Generate();

        Assert.False(tolerance.Equals((IsoTolerance?)null));
        Assert.False(tolerance.Equals((object?)null));
        Assert.False(tolerance.Equals((object)tolerance.ToString()));
    }

}
