namespace RailwayInterlocking.Domain.Tests.Expert;

public sealed class PointSettingTests {

    [Theory]
    [InlineData(PointPosition.Normal)]
    [InlineData(PointPosition.Reverse)]
    public void A_setting_keeps_the_point_and_the_position_it_requires(PointPosition position) {
        PointSetting setting = new PointSetting("P7", position);

        Assert.Equal("P7", setting.PointName);
        Assert.Equal(position, setting.Required);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void A_setting_needs_a_point_that_is_named(string blankName) {
        PointPosition anyPosition = Any.Enum<PointPosition>().Generate();

        Assert.Throws<ArgumentException>(() => new PointSetting(blankName, anyPosition));
    }

    [Fact]
    public void A_setting_without_a_point_name_is_refused_as_a_missing_argument() {
        PointPosition anyPosition = Any.Enum<PointPosition>().Generate();

        Assert.Throws<ArgumentNullException>(() => new PointSetting(null!, anyPosition));
    }

    [Theory]
    [InlineData(2)]    // the first value past the declared members
    [InlineData(-1)]
    public void A_setting_needs_a_position_the_points_can_take(int undeclared) {
        string anyPointName = AnyPoint.ValidName().Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new PointSetting(anyPointName, (PointPosition)undeclared));

        Assert.Equal("required", refusal.ParamName);
    }

    [Theory]
    [InlineData(PointPosition.Normal, "P7N")]
    [InlineData(PointPosition.Reverse, "P7R")]
    public void A_setting_reads_as_the_point_followed_by_the_initial_of_its_position(PointPosition position, string expected) {
        Assert.Equal(expected, new PointSetting("P7", position).ToString());
    }

}
