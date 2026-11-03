using LiquidityGap.Domain;

namespace LiquidityGap.Tests.Discovery;

public sealed class TimeBucketTests {

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(19, true)]
    [InlineData(20, false)]
    [InlineData(1000, false)]
    public void A_bounded_bucket_contains_its_start_day_but_not_its_end_day(int day, bool expected) {
        // Arrange
        TimeBucket bucket = Ladders.Bucket(10, 20);

        // Act
        bool contains = bucket.Contains(day);

        // Assert
        Assert.Equal(expected, contains);
    }

    [Theory]
    [InlineData(89, false)]
    [InlineData(90, true)]
    [InlineData(91, true)]
    [InlineData(int.MaxValue, true)]
    public void An_open_ended_bucket_contains_every_day_from_its_start(int day, bool expected) {
        // Arrange
        TimeBucket bucket = Ladders.Bucket(90, null);

        // Act
        bool contains = bucket.Contains(day);

        // Assert
        Assert.Equal(expected, contains);
        Assert.True(bucket.IsOpenEnded);
    }

    [Fact]
    public void A_bounded_bucket_is_not_open_ended() {
        // Arrange
        TimeBucket bucket = Any.TimeBucket().WithStartDay(0).WithEndDay(1).Generate();

        // Assert
        Assert.False(bucket.IsOpenEnded);
        Assert.Equal(1, bucket.EndDay);
    }

    [Fact]
    public void A_bucket_may_start_on_the_as_of_date() {
        // Act
        TimeBucket bucket = Any.TimeBucket().WithStartDay(0).Generate();

        // Assert
        Assert.Equal(0, bucket.StartDay);
        Assert.True(bucket.Contains(0));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void A_blank_label_is_refused(string label) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.TimeBucket().WithLabel(label).Generate());

        // Assert
        Assert.Equal("label", refusal.ParamName);
    }

    [Fact]
    public void A_bucket_cannot_start_before_the_as_of_date() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.TimeBucket().WithStartDay(-1).WithEndDay(10).Generate());

        // Assert
        Assert.Equal("startDay", refusal.ParamName);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(10, 9)]
    [InlineData(0, 0)]
    [InlineData(10, 0)]
    public void A_bucket_that_ends_on_or_before_its_start_is_refused(int start, int end) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.TimeBucket().WithStartDay(start).WithEndDay(end).Generate());

        // Assert
        Assert.Equal("endDay", refusal.ParamName);
    }

    [Fact]
    public void A_one_day_bucket_is_valid() {
        // Act
        TimeBucket bucket = Ladders.Bucket(10, 11);

        // Assert
        Assert.True(bucket.Contains(10));
        Assert.False(bucket.Contains(11));
    }

    [Fact]
    public void A_ladder_of_contiguous_buckets_ending_open_is_accepted() {
        // Arrange
        IReadOnlyList<TimeBucket> ladder = Ladders.ThreeBuckets();

        // Act & Assert
        TimeBucket.EnsureLadder(ladder);
    }

    [Fact]
    public void A_single_open_ended_bucket_from_day_zero_is_a_ladder() {
        // Act & Assert
        TimeBucket.EnsureLadder(new List<TimeBucket> { Ladders.Bucket(0, null) });
    }

    [Fact]
    public void An_arbitrary_ladder_is_accepted() {
        // Arrange
        IReadOnlyList<TimeBucket> ladder = new AnyBucketLadder().Generate();

        // Act & Assert
        TimeBucket.EnsureLadder(ladder);
    }

    [Fact]
    public void No_buckets_is_not_a_ladder() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => TimeBucket.EnsureLadder(new List<TimeBucket>()));
    }

    [Fact]
    public void A_missing_ladder_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TimeBucket.EnsureLadder(null!));
    }

    [Fact]
    public void A_ladder_must_start_at_day_zero() {
        // Arrange
        List<TimeBucket> ladder = new() { Ladders.Bucket(1, 30), Ladders.Bucket(30, null) };

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => TimeBucket.EnsureLadder(ladder));

        // Assert
        Assert.Contains("day 0", refusal.Message);
    }

    [Fact]
    public void A_gap_between_two_buckets_is_refused_naming_both() {
        // Arrange
        TimeBucket first  = Any.TimeBucket().WithLabel("short").WithStartDay(0).WithEndDay(30).Generate();
        TimeBucket second = Any.TimeBucket().WithLabel("long").WithStartDay(31).WithoutEndDay().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => TimeBucket.EnsureLadder(new List<TimeBucket> { first, second }));

        // Assert
        Assert.Contains("'short'", refusal.Message);
        Assert.Contains("'long'", refusal.Message);
    }

    [Fact]
    public void An_overlap_between_two_buckets_is_refused() {
        // Arrange
        List<TimeBucket> ladder = new() { Ladders.Bucket(0, 30), Ladders.Bucket(20, null) };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TimeBucket.EnsureLadder(ladder));
    }

    [Fact]
    public void The_last_bucket_must_be_open_ended() {
        // Arrange
        List<TimeBucket> ladder = new() { Ladders.Bucket(0, 30), Ladders.Bucket(30, 90) };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TimeBucket.EnsureLadder(ladder));
    }

    [Fact]
    public void Only_the_last_bucket_may_be_open_ended() {
        // Arrange
        List<TimeBucket> ladder = new() { Ladders.Bucket(0, null), Ladders.Bucket(30, null) };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TimeBucket.EnsureLadder(ladder));
    }

}
