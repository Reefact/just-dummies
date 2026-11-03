namespace RailwayInterlocking.Domain.Tests.Expert;

public sealed class PointTests {

    [Theory]
    [InlineData(PointPosition.Normal)]
    [InlineData(PointPosition.Reverse)]
    public void A_new_point_lies_in_its_section_in_the_position_it_was_given_and_is_free(PointPosition initialPosition) {
        SectionId section = new SectionId("T101");

        Point point = Any.Point().WithName("P7").WithSection(section).WithInitialPosition(initialPosition).Generate();

        Assert.Equal("P7", point.Name);
        Assert.Equal(section, point.Section);
        Assert.Equal(initialPosition, point.Position);
        Assert.False(point.IsLocked);
        Assert.Null(point.LockedByRoute);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t\n")]
    public void A_point_needs_a_name(string blankName) {
        Point anyPoint = Any.Point().Generate();

        Assert.Throws<ArgumentException>(() => new Point(blankName, anyPoint.Section, anyPoint.Position));
    }

    [Fact]
    public void A_point_without_a_name_is_refused_as_a_missing_argument() {
        Point anyPoint = Any.Point().Generate();

        Assert.Throws<ArgumentNullException>(() => new Point(null!, anyPoint.Section, anyPoint.Position));
    }

    [Fact]
    public void A_point_needs_a_section_to_lie_in() {
        Point anyPoint = Any.Point().Generate();

        Assert.Throws<ArgumentNullException>(() => new Point(anyPoint.Name, null!, anyPoint.Position));
    }

    [Fact]
    public void A_point_needs_a_position_it_can_take() {
        Point anyPoint = Any.Point().Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Point(anyPoint.Name, anyPoint.Section, (PointPosition)2));

        Assert.Equal("initialPosition", refusal.ParamName);
    }

}
