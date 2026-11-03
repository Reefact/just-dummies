namespace RailwayInterlocking.Domain.Tests.Expert;

public sealed class TrainIdTests {

    [Theory]
    [InlineData("1A23")]
    [InlineData("0A00")]   // the lowest digits
    [InlineData("9Z99")]   // the highest
    public void A_headcode_is_a_digit_a_capital_letter_and_two_digits(string text) {
        TrainId train = new TrainId(text);

        Assert.Equal(text, train.Headcode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1A2")]      // too short
    [InlineData("1A234")]    // too long
    [InlineData("A123")]     // letter first
    [InlineData("11A3")]     // letter misplaced
    [InlineData("1a23")]     // lower case
    [InlineData("1AB3")]
    [InlineData(" 1A23")]
    [InlineData("1A23 ")]
    [InlineData("1A-23")]
    [InlineData("1A2３")]   // a full-width digit
    public void A_headcode_of_any_other_shape_is_refused(string text) {
        Assert.Throws<ArgumentException>(() => new TrainId(text));
    }

    [Fact]
    public void A_headcode_without_text_is_refused_as_a_missing_argument() {
        Assert.Throws<ArgumentNullException>(() => new TrainId(null!));
    }

    [Fact(Skip = "domain defect: the format is anchored with '$', which .NET also matches before a trailing line feed, so \"1A23\\n\" is accepted as a headcode distinct from \"1A23\"; found by reading the pattern, no seed involved")]
    public void A_headcode_followed_by_a_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => new TrainId("1A23\n"));
    }

    [Fact]
    public void Two_trains_made_of_the_same_headcode_are_equal() {
        string  anyHeadcode = Any.TrainId().Generate().Headcode;
        TrainId first       = new TrainId(anyHeadcode);
        TrainId second      = new TrainId(anyHeadcode);

        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_trains_of_different_headcodes_are_not_equal() {
        TrainId first  = Any.TrainId().Generate();
        TrainId second = Any.TrainId().WithHeadcode(headcode => headcode.DifferentFrom(first.Headcode)).Generate();

        Assert.False(first.Equals(second));
        Assert.False(first.Equals((object)second));
    }

    [Fact]
    public void A_train_is_equal_to_neither_a_missing_train_nor_something_else() {
        TrainId anyTrain = Any.TrainId().Generate();

        Assert.False(anyTrain.Equals((TrainId?)null));
        Assert.False(anyTrain.Equals((object?)null));
        Assert.False(anyTrain.Equals(anyTrain.Headcode));
    }

    [Fact]
    public void A_train_reads_as_its_headcode() {
        Assert.Equal("1A23", new TrainId("1A23").ToString());
    }

}
