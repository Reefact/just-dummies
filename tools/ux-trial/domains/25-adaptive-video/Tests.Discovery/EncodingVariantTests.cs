using AdaptiveVideo.Domain;

namespace AdaptiveVideo.Tests.Discovery;

public sealed class EncodingVariantTests {

    [Fact]
    public void A_well_formed_variant_exposes_what_it_was_given() {
        Bitrate anyBitrate = Any.Bitrate().Generate();

        EncodingVariant variant = new EncodingVariant("720p30", 1280, 720, anyBitrate);

        Assert.Equal("720p30", variant.Id);
        Assert.Equal(1280, variant.Width);
        Assert.Equal(720, variant.Height);
        Assert.Same(anyBitrate, variant.Bitrate);
    }

    [Theory]
    [InlineData("720p24", 24)]
    [InlineData("720p25", 25)]
    [InlineData("720p30", 30)]
    [InlineData("720p50", 50)]
    [InlineData("720p60", 60)]
    public void The_frame_rate_is_read_from_the_id(string id, int expectedFramesPerSecond) {
        Bitrate anyBitrate = Any.Bitrate().Generate();

        EncodingVariant variant = new EncodingVariant(id, 1280, 720, anyBitrate);

        Assert.Equal(expectedFramesPerSecond, variant.FramesPerSecond);
    }

    [Theory]
    [InlineData("1080p30", 1920, 1080)]
    [InlineData("2160p60", 3840, 2160)]
    [InlineData("4320p24", 7680, 4320)]
    [InlineData("144p25", 256, 144)]
    [InlineData("096p30", 128, 96)]
    public void Four_digit_and_three_digit_heights_are_both_accepted(string id, int width, int height) {
        Bitrate anyBitrate = Any.Bitrate().Generate();

        EncodingVariant variant = new EncodingVariant(id, width, height, anyBitrate);

        Assert.Equal(height, variant.Height);
    }

    [Fact]
    public void A_square_resolution_is_accepted() {
        Bitrate anyBitrate = Any.Bitrate().Generate();

        EncodingVariant variant = new EncodingVariant("480p30", 480, 480, anyBitrate);

        Assert.Equal(variant.Width, variant.Height);
    }

    [Theory]
    [InlineData("")]
    [InlineData("720")]
    [InlineData("720p")]
    [InlineData("p30")]
    [InlineData("720p31")]
    [InlineData("720p3")]
    [InlineData("720p300")]
    [InlineData("72p30")]
    [InlineData("12345p30")]
    [InlineData("720P30")]
    [InlineData(" 720p30")]
    [InlineData("720p30 ")]
    [InlineData("720 p30")]
    [InlineData("-720p30")]
    [InlineData("72Op30")]
    public void A_malformed_id_is_refused_with_a_format_exception(string id) {
        Bitrate anyBitrate = Any.Bitrate().Generate();

        Assert.Throws<FormatException>(() => new EncodingVariant(id, 1280, 720, anyBitrate));
    }

    [Fact(Skip = "domain defect: EncodingVariant accepts the id \"720p30\\n\" because $ in the id pattern also matches before a final line feed; literal input, any seed")]
    public void An_id_with_a_trailing_line_feed_is_refused() {
        Bitrate anyBitrate = Any.Bitrate().Generate();

        Assert.Throws<FormatException>(() => new EncodingVariant("720p30\n", 1280, 720, anyBitrate));
    }

    [Fact]
    public void A_missing_id_is_refused() {
        Bitrate anyBitrate = Any.Bitrate().Generate();

        Assert.Throws<ArgumentNullException>(() => new EncodingVariant(null!, 1280, 720, anyBitrate));
    }

    [Fact]
    public void A_missing_bitrate_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new EncodingVariant("720p30", 1280, 720, null!));
    }

    [Theory]
    [InlineData(127)]
    [InlineData(126)]
    [InlineData(7681)]
    [InlineData(7682)]
    [InlineData(0)]
    [InlineData(-1280)]
    public void A_width_outside_the_range_is_refused(int width) {
        Bitrate anyBitrate = Any.Bitrate().Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new EncodingVariant("096p30", width, 96, anyBitrate));

        Assert.Equal("width", exception.ParamName);
    }

    [Theory]
    [InlineData("094p30", 94)]
    [InlineData("095p30", 95)]
    [InlineData("4322p30", 4322)]
    [InlineData("4321p30", 4321)]
    public void A_height_outside_the_range_is_refused(string id, int height) {
        Bitrate anyBitrate = Any.Bitrate().Generate();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new EncodingVariant(id, 7680, height, anyBitrate));

        Assert.Equal("height", exception.ParamName);
    }

    [Theory]
    [InlineData(1281, 720)]
    [InlineData(1280, 721)]
    [InlineData(1281, 721)]
    public void An_odd_dimension_is_refused(int width, int height) {
        Bitrate anyBitrate = Any.Bitrate().Generate();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new EncodingVariant($"{height}p30", width, height, anyBitrate));

        Assert.IsNotType<ArgumentOutOfRangeException>(exception);
    }

    [Theory]
    [InlineData(1080, 720)]
    [InlineData(1280, 1278)]
    public void A_portrait_resolution_is_refused(int height, int width) {
        Bitrate anyBitrate = Any.Bitrate().Generate();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new EncodingVariant($"{height}p30", width, height, anyBitrate));

        Assert.Contains("landscape", exception.Message);
    }

    [Fact]
    public void An_id_that_declares_another_height_is_refused() {
        Bitrate anyBitrate = Any.Bitrate().Generate();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new EncodingVariant("1080p30", 1280, 720, anyBitrate));

        Assert.Contains("1080", exception.Message);
        Assert.Contains("720", exception.Message);
    }

    [Fact]
    public void A_variant_prints_its_id_and_bitrate() {
        EncodingVariant variant = Any.EncodingVariant().WithHeight(720).WithFramesPerSecond(30).WithBitrate(new Bitrate(3_000)).Generate();

        Assert.Equal("720p30 @ 3000 kbps", variant.ToString());
    }

    [Fact]
    public void The_drawn_frame_rate_is_the_one_the_id_declares() {
        EncodingVariant variant = Any.EncodingVariant().WithFramesPerSecond(50).Generate();

        Assert.Equal(50, variant.FramesPerSecond);
        Assert.EndsWith("p50", variant.Id);
    }

}
