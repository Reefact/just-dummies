namespace LiquidityGap.Domain.Tests.Expert.Tests;

public sealed class TimeBucketTests {

    [Fact]
    public void A_closed_bucket_covers_its_start_day_up_to_but_not_including_its_end_day() {
        // Arrange
        TimeBucket bucket = TestBuckets.Bucket(10, 20);

        // Assert
        Assert.False(bucket.Contains(9));
        Assert.True(bucket.Contains(10));
        Assert.True(bucket.Contains(19));
        Assert.False(bucket.Contains(20));
        Assert.False(bucket.IsOpenEnded);
    }

    [Fact]
    public void An_open_ended_bucket_covers_every_day_from_its_start_on() {
        // Arrange
        TimeBucket bucket = TestBuckets.Bucket(10, null);

        // Assert
        Assert.False(bucket.Contains(9));
        Assert.True(bucket.Contains(10));
        Assert.True(bucket.Contains(int.MaxValue));
        Assert.True(bucket.IsOpenEnded);
    }

    [Fact]
    public void A_bucket_may_start_on_the_as_of_date() {
        // Act
        TimeBucket bucket = TestBuckets.Bucket(0, 1);

        // Assert
        Assert.True(bucket.Contains(0));
        Assert.False(bucket.Contains(-1));
    }

    [Fact]
    public void A_bucket_may_cover_a_single_day() {
        // Act
        TimeBucket bucket = TestBuckets.Bucket(7, 8);

        // Assert
        Assert.True(bucket.Contains(7));
        Assert.False(bucket.Contains(8));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_label_is_refused(string blank) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.TimeBucket().WithLabel(blank).Generate());

        // Assert
        Assert.Equal("label", refusal.ParamName);
    }

    [Fact]
    public void A_bucket_cannot_start_before_the_as_of_date() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.TimeBucket().WithStartDay(-1).Generate());

        // Assert
        Assert.Equal("startDay", refusal.ParamName);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(10, 9)]
    [InlineData(0, 0)]
    public void A_bucket_that_ends_on_or_before_its_start_is_refused(int startDay, int endDay) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => TestBuckets.Bucket(startDay, endDay));

        // Assert
        Assert.Equal("endDay", refusal.ParamName);
    }

    // ----- EnsureLadder -----

    [Fact]
    public void A_contiguous_ladder_from_day_zero_ending_open_is_accepted() {
        // Arrange
        IReadOnlyList<TimeBucket> ladder = TestBuckets.ThreeBucketLadder();

        // Act
        Exception? refusal = Record.Exception(() => TimeBucket.EnsureLadder(ladder));

        // Assert
        Assert.Null(refusal);
    }

    [Fact]
    public void A_single_open_ended_bucket_from_day_zero_is_a_ladder() {
        // Act
        Exception? refusal = Record.Exception(() => TimeBucket.EnsureLadder([TestBuckets.Bucket(0, null)]));

        // Assert
        Assert.Null(refusal);
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
        Assert.Equal("buckets", refusal.ParamName);
    }

    [Fact]
    public void A_ladder_that_does_not_start_at_day_zero_is_refused() {
        // Arrange
        IReadOnlyList<TimeBucket> ladder = [TestBuckets.Bucket(1, 30), TestBuckets.Bucket(30, null)];

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => TimeBucket.EnsureLadder(ladder));

        // Assert
        Assert.Contains("day 0", refusal.Message);
    }

    [Fact]
    public void A_ladder_with_a_hole_between_two_buckets_is_refused_naming_both() {
        // Arrange
        TimeBucket before = Any.TimeBucket().WithLabel("1M").WithStartDay(0).WithEndDay(30).Generate();
        TimeBucket after  = Any.TimeBucket().WithLabel("3M").WithStartDay(31).WithoutEndDay().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => TimeBucket.EnsureLadder([before, after]));

        // Assert
        Assert.Contains("'1M'", refusal.Message);
        Assert.Contains("'3M'", refusal.Message);
    }

    [Fact]
    public void A_ladder_with_two_overlapping_buckets_is_refused() {
        // Arrange
        IReadOnlyList<TimeBucket> ladder = [TestBuckets.Bucket(0, 30), TestBuckets.Bucket(29, null)];

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => TimeBucket.EnsureLadder(ladder));

        // Assert
        Assert.Equal("buckets", refusal.ParamName);
    }

    [Fact]
    public void A_ladder_whose_last_bucket_is_closed_is_refused() {
        // Arrange
        IReadOnlyList<TimeBucket> ladder = [TestBuckets.Bucket(0, 30), TestBuckets.Bucket(30, 90)];

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => TimeBucket.EnsureLadder(ladder));

        // Assert
        Assert.Contains("open-ended", refusal.Message);
    }

    [Fact]
    public void An_open_ended_bucket_in_the_middle_of_a_ladder_is_refused() {
        // Arrange
        IReadOnlyList<TimeBucket> ladder = [TestBuckets.Bucket(0, null), TestBuckets.Bucket(30, null)];

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => TimeBucket.EnsureLadder(ladder));

        // Assert
        Assert.Equal("buckets", refusal.ParamName);
    }

    [Fact]
    public void Every_future_day_of_a_ladder_belongs_to_exactly_one_bucket() {
        // Arrange
        IReadOnlyList<TimeBucket> ladder = TestBuckets.ThreeBucketLadder();

        // Assert: the two edges of every bucket, and a day far out
        int[] days = [0, 29, 30, 89, 90, 3650];
        foreach (int day in days) {
            Assert.Single(ladder, bucket => bucket.Contains(day));
        }
    }

}
