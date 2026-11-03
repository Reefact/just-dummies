using JustDummies;

using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Regular;

public sealed class TimeBucketTests {

    [Fact]
    public void A_closed_bucket_contains_its_start_day_and_not_its_end_day() {
        // Arrange
        TimeBucket bucket = Any.TimeBucket().WithStartDay(30).WithEndDay(90).Generate();

        // Act & Assert
        Assert.False(bucket.Contains(29));
        Assert.True(bucket.Contains(30));
        Assert.True(bucket.Contains(89));
        Assert.False(bucket.Contains(90));
        Assert.False(bucket.IsOpenEnded);
    }

    [Fact]
    public void An_open_ended_bucket_contains_every_day_from_its_start_on() {
        // Arrange
        TimeBucket bucket = Any.TimeBucket().WithStartDay(365).WithoutEndDay().Generate();

        // Act & Assert
        Assert.False(bucket.Contains(364));
        Assert.True(bucket.Contains(365));
        Assert.True(bucket.Contains(int.MaxValue));
        Assert.True(bucket.IsOpenEnded);
        Assert.Null(bucket.EndDay);
    }

    [Fact]
    public void A_one_day_bucket_contains_exactly_that_day() {
        // Arrange
        TimeBucket bucket = new TimeBucket("today", 0, 1);

        // Act & Assert
        Assert.True(bucket.Contains(0));
        Assert.False(bucket.Contains(1));
        Assert.False(bucket.Contains(-1));
    }

    [Fact]
    public void A_bucket_keeps_its_label() {
        // Arrange
        string label = Any.String().NotBlank().Generate();

        // Act
        TimeBucket bucket = Any.TimeBucket().WithLabel(label).Generate();

        // Assert
        Assert.Equal(label, bucket.Label);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void A_blank_label_is_refused(string? blankLabel) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TimeBucket(blankLabel!, 0, 30));

        // Assert
        Assert.Equal("label", refusal.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_bucket_cannot_start_before_the_as_of_date(int startDay) {
        // Arrange
        string anyLabel = Any.String().NotBlank().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new TimeBucket(anyLabel, startDay, null));

        // Assert
        Assert.Equal("startDay", refusal.ParamName);
    }

    [Theory]
    [InlineData(30, 30)]
    [InlineData(30, 29)]
    [InlineData(30, 0)]
    [InlineData(0, 0)]
    [InlineData(5, -5)]
    public void A_closed_bucket_that_ends_on_or_before_its_start_is_refused(int startDay, int endDay) {
        // Arrange
        string anyLabel = Any.String().NotBlank().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TimeBucket(anyLabel, startDay, endDay));

        // Assert
        Assert.Equal("endDay", refusal.ParamName);
    }

    [Fact]
    public void The_standard_ladder_is_a_ladder() {
        // Act & Assert
        TimeBucket.EnsureLadder(Fixtures.StandardLadder());
    }

    [Fact]
    public void A_single_open_ended_bucket_from_day_zero_is_a_ladder() {
        // Arrange
        TimeBucket[] ladder = [new TimeBucket("all", 0, null)];

        // Act & Assert
        TimeBucket.EnsureLadder(ladder);
    }

    [Fact]
    public void A_generated_ladder_is_a_ladder() {
        // Arrange
        IReadOnlyList<TimeBucket> anyLadder = new AnyBucketLadder().Generate();

        // Act & Assert
        TimeBucket.EnsureLadder(anyLadder);
    }

    [Fact]
    public void A_missing_ladder_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => TimeBucket.EnsureLadder(null!));

        // Assert
        Assert.Equal("buckets", refusal.ParamName);
    }

    [Fact]
    public void An_empty_ladder_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => TimeBucket.EnsureLadder([]));

        // Assert
        Assert.Contains("at least one bucket", refusal.Message);
    }

    [Fact]
    public void A_ladder_that_does_not_start_at_day_zero_is_refused() {
        // Arrange
        TimeBucket[] ladder = [new TimeBucket("late", 1, null)];

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => TimeBucket.EnsureLadder(ladder));

        // Assert
        Assert.Contains("day 0", refusal.Message);
    }

    [Fact]
    public void A_gap_between_two_buckets_is_refused_naming_both() {
        // Arrange
        TimeBucket[] ladder = [new TimeBucket("first", 0, 30), new TimeBucket("second", 31, null)];

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => TimeBucket.EnsureLadder(ladder));

        // Assert
        Assert.Contains("'first'", refusal.Message);
        Assert.Contains("'second'", refusal.Message);
        Assert.Contains("not contiguous", refusal.Message);
    }

    [Fact]
    public void An_overlap_between_two_buckets_is_refused() {
        // Arrange
        TimeBucket[] ladder = [new TimeBucket("first", 0, 30), new TimeBucket("second", 29, null)];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TimeBucket.EnsureLadder(ladder));
    }

    [Fact]
    public void A_ladder_whose_last_bucket_is_closed_is_refused() {
        // Arrange
        TimeBucket[] ladder = [new TimeBucket("first", 0, 30), new TimeBucket("second", 30, 60)];

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => TimeBucket.EnsureLadder(ladder));

        // Assert
        Assert.Contains("open-ended", refusal.Message);
    }

    [Fact]
    public void An_open_ended_bucket_before_the_last_one_is_refused() {
        // Arrange
        TimeBucket[] ladder = [new TimeBucket("open", 0, null), new TimeBucket("last", 30, null)];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TimeBucket.EnsureLadder(ladder));
    }

}
