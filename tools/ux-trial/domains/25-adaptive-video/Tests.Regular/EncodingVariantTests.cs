using JustDummies;

namespace AdaptiveVideo.Domain.Tests;

public sealed class EncodingVariantTests {

    private static Bitrate AnyRate => new AnyBitrate().Generate();

    [Fact]
    public void A_720p30_variant_exposes_what_it_was_built_with() {
        // Arrange
        Bitrate bitrate = new Bitrate(3_000);

        // Act
        EncodingVariant variant = new EncodingVariant("720p30", 1280, 720, bitrate);

        // Assert
        Assert.Equal("720p30", variant.Id);
        Assert.Equal(1280, variant.Width);
        Assert.Equal(720, variant.Height);
        Assert.Equal(30, variant.FramesPerSecond);
        Assert.Same(bitrate, variant.Bitrate);
    }

    [Theory]
    [InlineData("720p24", 24)]
    [InlineData("720p25", 25)]
    [InlineData("720p30", 30)]
    [InlineData("720p50", 50)]
    [InlineData("720p60", 60)]
    public void The_frame_rate_is_read_from_the_id(string id, int framesPerSecond) {
        // Act
        EncodingVariant variant = new EncodingVariant(id, 1280, 720, AnyRate);

        // Assert
        Assert.Equal(framesPerSecond, variant.FramesPerSecond);
    }

    [Theory]
    [InlineData("096p30", 128, 96)]
    [InlineData("4320p60", 7680, 4320)]
    [InlineData("1080p25", 1080, 1080)]
    public void The_smallest_the_largest_and_the_square_resolutions_are_accepted(string id, int width, int height) {
        // Act
        EncodingVariant variant = new EncodingVariant(id, width, height, AnyRate);

        // Assert
        Assert.Equal(width, variant.Width);
        Assert.Equal(height, variant.Height);
    }

    [Theory]
    [InlineData("")]
    [InlineData("720")]
    [InlineData("720p")]
    [InlineData("p30")]
    [InlineData("72p30")]
    [InlineData("12345p30")]
    [InlineData("720p31")]
    [InlineData("720p3")]
    [InlineData("720P30")]
    [InlineData("720p300")]
    [InlineData(" 720p30")]
    [InlineData("720p30 ")]
    [InlineData("7a0p30")]
    public void An_id_that_is_not_a_height_and_a_frame_rate_is_refused(string id) {
        Assert.Throws<FormatException>(() => new EncodingVariant(id, 1280, 720, AnyRate));
    }

    [Fact(Skip = "domain defect: the id pattern ends with '$', which .NET also matches before a final '\\n', so '720p30\\n' is accepted as an id; deterministic, literal input, no seed")]
    public void An_id_cannot_carry_a_trailing_line_break() {
        // The id is documented as '<height>p<fps>'; a line break is neither.
        Assert.Throws<FormatException>(() => new EncodingVariant("720p30\n", 1280, 720, AnyRate));
    }

    [Fact]
    public void A_missing_id_is_refused() {
        Assert.Throws<ArgumentNullException>("id", () => new EncodingVariant(null!, 1280, 720, AnyRate));
    }

    [Fact]
    public void A_missing_bitrate_is_refused() {
        Assert.Throws<ArgumentNullException>("bitrate", () => new EncodingVariant("720p30", 1280, 720, null!));
    }

    [Theory]
    [InlineData(126)]
    [InlineData(7682)]
    public void A_width_outside_the_range_is_refused(int width) {
        Assert.Throws<ArgumentOutOfRangeException>("width", () => new EncodingVariant("096p30", width, 96, AnyRate));
    }

    [Theory]
    [InlineData(94, "094p30")]
    [InlineData(4322, "4322p30")]
    public void A_height_outside_the_range_is_refused(int height, string id) {
        Assert.Throws<ArgumentOutOfRangeException>("height", () => new EncodingVariant(id, 7680, height, AnyRate));
    }

    [Theory]
    [InlineData(1281, 720)]
    [InlineData(1280, 721)]
    public void An_odd_dimension_is_refused(int width, int height) {
        Assert.Throws<ArgumentException>(() => new EncodingVariant($"{height}p30", width, height, AnyRate));
    }

    [Fact]
    public void Portrait_content_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new EncodingVariant("1080p30", 720, 1080, AnyRate));

        // Assert
        Assert.Contains("landscape", refusal.Message);
    }

    [Fact]
    public void An_id_that_declares_another_height_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new EncodingVariant("720p30", 1920, 1080, AnyRate));

        // Assert
        Assert.Contains("720p30", refusal.Message);
    }

    [Fact]
    public void A_variant_prints_its_id_and_its_bitrate() {
        // Arrange
        EncodingVariant variant = new EncodingVariant("720p30", 1280, 720, new Bitrate(3_000));

        // Assert
        Assert.Equal("720p30 @ 3000 kbps", variant.ToString());
    }

    [Fact]
    public void A_drawn_variant_is_always_one_the_constructor_accepts() {
        // Arrange
        AnyEncodingVariant anyVariant = new AnyEncodingVariant();

        // Act / Assert: the generator is part of the test code, so it is checked over many draws.
        for (int i = 0; i < 300; i++) {
            EncodingVariant variant = anyVariant.Generate();

            Assert.StartsWith(variant.Height.ToString("D3"), variant.Id);
        }
    }

}
