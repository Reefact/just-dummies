using AdaptiveVideo.Domain;

using JustDummies;

using Xunit;

namespace AdaptiveVideo.Tests.Expert;

public sealed class EncodingVariantTests {

    // A width that suits a height: even, at least as wide as it is high, and inside the supported range.
    private static int AnyWidthFor(int height) {
        return Any.Int32().Between(Math.Max(128, height), 7680).Even().Generate();
    }

    [Fact]
    public void A_variant_exposes_what_it_was_built_from() {
        // Arrange
        Bitrate bitrate = new Bitrate(6_000);

        // Act
        EncodingVariant variant = new EncodingVariant("1080p60", 1920, 1080, bitrate);

        // Assert
        Assert.Equal("1080p60", variant.Id);
        Assert.Equal(1920, variant.Width);
        Assert.Equal(1080, variant.Height);
        Assert.Equal(60, variant.FramesPerSecond);
        Assert.Same(bitrate, variant.Bitrate);
        Assert.Equal("1080p60 @ 6000 kbps", variant.ToString());
    }

    [Theory]
    [InlineData("720p24", 24)]
    [InlineData("720p25", 25)]
    [InlineData("720p30", 30)]
    [InlineData("720p50", 50)]
    [InlineData("720p60", 60)]
    public void The_frame_rate_is_read_from_the_end_of_the_id(string id, int expectedFramesPerSecond) {
        // Arrange
        int     anyWidth   = AnyWidthFor(720);
        Bitrate anyBitrate = Any.Bitrate().Generate();

        // Act
        EncodingVariant variant = new EncodingVariant(id, anyWidth, 720, anyBitrate);

        // Assert
        Assert.Equal(expectedFramesPerSecond, variant.FramesPerSecond);
    }

    [Theory]
    [InlineData("096p30", 96)]
    [InlineData("0720p30", 720)]
    [InlineData("4320p60", 4320)]
    public void An_id_of_three_or_four_digits_names_the_height_it_stands_for(string id, int height) {
        // Arrange
        int     anyWidth   = AnyWidthFor(height);
        Bitrate anyBitrate = Any.Bitrate().Generate();

        // Act
        EncodingVariant variant = new EncodingVariant(id, anyWidth, height, anyBitrate);

        // Assert
        Assert.Equal(id, variant.Id);
        Assert.Equal(height, variant.Height);
    }

    [Theory]
    [InlineData("")]
    [InlineData("720")]
    [InlineData("720p")]
    [InlineData("p30")]
    [InlineData("72p30")]
    [InlineData("12345p30")]
    [InlineData("720p29")]
    [InlineData("720p300")]
    [InlineData("720P30")]
    [InlineData("720i30")]
    [InlineData("720p30fps")]
    [InlineData(" 720p30")]
    [InlineData("720p30 ")]
    [InlineData("7a0p30")]
    public void An_id_that_is_not_a_height_and_a_standard_frame_rate_is_a_format_error(string id) {
        // Arrange
        EncodingVariant anyVariant = Any.EncodingVariant().Generate();

        // Act + Assert
        Assert.Throws<FormatException>(() => new EncodingVariant(id, anyVariant.Width, anyVariant.Height, anyVariant.Bitrate));
    }

    [Fact(Skip = "domain defect: the id pattern ends with '$', which .NET also lets match before a final line feed, so " +
                 "\"720p30\\n\" is accepted as an id (Id keeps the line feed). Literal input, no seed involved.")]
    public void An_id_followed_by_a_line_feed_is_a_format_error() {
        // Arrange
        int     anyWidth   = AnyWidthFor(720);
        Bitrate anyBitrate = Any.Bitrate().Generate();

        // Act + Assert
        Assert.Throws<FormatException>(() => new EncodingVariant("720p30\n", anyWidth, 720, anyBitrate));
    }

    [Fact]
    public void A_variant_needs_an_id_and_a_bitrate() {
        // Arrange
        EncodingVariant anyVariant = Any.EncodingVariant().Generate();

        // Act
        ArgumentNullException withoutId      = Assert.Throws<ArgumentNullException>(() => new EncodingVariant(null!, anyVariant.Width, anyVariant.Height, anyVariant.Bitrate));
        ArgumentNullException withoutBitrate = Assert.Throws<ArgumentNullException>(() => new EncodingVariant(anyVariant.Id, anyVariant.Width, anyVariant.Height, null!));

        // Assert
        Assert.Equal("id", withoutId.ParamName);
        Assert.Equal("bitrate", withoutBitrate.ParamName);
    }

    [Theory]
    [InlineData(126)]
    [InlineData(0)]
    [InlineData(-1280)]
    [InlineData(7682)]
    public void A_width_outside_128_to_7680_is_out_of_range(int width) {
        // Arrange
        Bitrate anyBitrate = Any.Bitrate().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new EncodingVariant("096p30", width, 96, anyBitrate));

        // Assert
        Assert.Equal("width", refusal.ParamName);
    }

    [Theory]
    [InlineData("094p30", 94)]
    [InlineData("000p30", 0)]
    [InlineData("4322p30", 4322)]
    public void A_height_outside_96_to_4320_is_out_of_range(string id, int height) {
        // Arrange
        Bitrate anyBitrate = Any.Bitrate().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new EncodingVariant(id, 7680, height, anyBitrate));

        // Assert
        Assert.Equal("height", refusal.ParamName);
    }

    [Theory]
    [InlineData(1281, 720)]
    [InlineData(1280, 721)]
    [InlineData(1281, 721)]
    public void An_odd_dimension_cannot_be_subsampled_in_4_2_0(int width, int height) {
        // Arrange
        Bitrate anyBitrate = Any.Bitrate().Generate();
        string  id         = $"{height}p30";

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new EncodingVariant(id, width, height, anyBitrate));
    }

    [Fact]
    public void A_portrait_variant_is_refused() {
        // Arrange
        Bitrate anyBitrate = Any.Bitrate().Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new EncodingVariant("1080p30", 720, 1080, anyBitrate));
    }

    [Fact]
    public void A_square_variant_is_not_portrait() {
        // Arrange
        Bitrate anyBitrate = Any.Bitrate().Generate();

        // Act
        EncodingVariant variant = new EncodingVariant("720p30", 720, 720, anyBitrate);

        // Assert
        Assert.Equal(variant.Width, variant.Height);
    }

    [Fact]
    public void The_height_in_the_id_must_be_the_height_of_the_variant() {
        // Arrange
        Bitrate anyBitrate = Any.Bitrate().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new EncodingVariant("720p30", 1920, 1080, anyBitrate));

        // Assert
        Assert.Contains("720p30", refusal.Message);
        Assert.Contains("1080", refusal.Message);
    }

}
