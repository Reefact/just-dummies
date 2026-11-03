using JustDummies;

using Xunit;

namespace SatelliteConjunction.Domain.Tests.Expert.ValueObjects;

public sealed class EpochTests {

    private static readonly DateTimeOffset SpaceAgeStart = new(1957, 10, 4, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void An_epoch_exposes_the_instant_it_was_built_from() {
        // Arrange
        DateTimeOffset launchOfExplorer1 = new(1958, 2, 1, 3, 48, 0, TimeSpan.Zero);

        // Act
        Epoch epoch = new(launchOfExplorer1);

        // Assert
        Assert.Equal(launchOfExplorer1, epoch.Utc);
    }

    [Fact]
    public void The_start_of_the_space_age_is_the_earliest_epoch() {
        // Act
        Epoch epoch = new(SpaceAgeStart);

        // Assert
        Assert.Equal(SpaceAgeStart, epoch.Utc);
    }

    [Fact]
    public void An_instant_one_tick_before_the_space_age_is_refused() {
        // Arrange
        DateTimeOffset justBefore = SpaceAgeStart.AddTicks(-1);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Epoch(justBefore));

        // Assert
        Assert.Equal("utc", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-5)]
    [InlineData(5.5)]
    public void An_instant_expressed_with_an_offset_is_refused_whatever_the_instant(double offsetHours) {
        // Arrange
        // The instant is a dummy (any modern instant would do); the offset is the whole point.
        DateTimeOffset anyInstantWithOffset = Any.DateTimeOffset()
                                                 .Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2050, 1, 1, 0, 0, 0, TimeSpan.Zero))
                                                 .WithOffset(TimeSpan.FromHours(offsetHours))
                                                 .Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Epoch(anyInstantWithOffset));

        // Assert
        Assert.Equal("utc", refusal.ParamName);
    }

    [Fact]
    public void Adding_a_duration_moves_the_epoch_by_exactly_that_many_seconds() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();

        // Act
        Epoch later = anyEpoch.Add(TimeSpan.FromSeconds(90));

        // Assert
        Assert.Equal(90, later.SecondsSince(anyEpoch));
    }

    [Fact]
    public void Adding_a_negative_duration_moves_the_epoch_back_and_the_seconds_since_turn_negative() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();

        // Act
        Epoch earlier = anyEpoch.Add(TimeSpan.FromSeconds(-90));

        // Assert
        Assert.Equal(-90, earlier.SecondsSince(anyEpoch));
        Assert.Equal(90, anyEpoch.SecondsSince(earlier));
    }

    [Fact]
    public void Adding_a_duration_does_not_change_the_epoch_it_is_added_to() {
        // Arrange
        Epoch anyEpoch  = Any.Epoch().Generate();
        Epoch untouched = new(anyEpoch.Utc);

        // Act
        anyEpoch.Add(TimeSpan.FromHours(3));

        // Assert
        Assert.Equal(untouched, anyEpoch);
    }

    [Fact]
    public void Adding_a_duration_that_goes_back_before_the_space_age_is_refused() {
        // Arrange
        Epoch startOfTheSpaceAge = new(SpaceAgeStart);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => startOfTheSpaceAge.Add(TimeSpan.FromTicks(-1)));
    }

    [Fact]
    public void Seconds_since_an_epoch_that_is_null_are_refused() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyEpoch.SecondsSince(null!));
    }

    [Fact]
    public void An_earlier_epoch_is_ordered_before_a_later_one() {
        // Arrange
        Epoch anyEarlier = Any.Epoch().Generate();
        Epoch anyLater   = anyEarlier.Add(TimeSpan.FromTicks(1));

        // Act & Assert
        Assert.True(anyEarlier.CompareTo(anyLater) < 0);
        Assert.True(anyLater.CompareTo(anyEarlier) > 0);
    }

    [Fact]
    public void An_epoch_compares_as_equal_to_one_at_the_same_instant() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();
        Epoch sameInstant = new(anyEpoch.Utc);

        // Act & Assert
        Assert.Equal(0, anyEpoch.CompareTo(sameInstant));
    }

    [Fact]
    public void Any_epoch_is_ordered_after_nothing() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();

        // Act & Assert
        Assert.True(anyEpoch.CompareTo(null) > 0);
    }

    [Fact]
    public void Two_epochs_at_the_same_instant_are_equal_and_share_a_hash_code() {
        // Arrange
        Epoch anyEpoch    = Any.Epoch().Generate();
        Epoch sameInstant = new(anyEpoch.Utc);

        // Act & Assert
        Assert.Equal(anyEpoch, sameInstant);
        Assert.True(anyEpoch.Equals((object)sameInstant));
        Assert.Equal(anyEpoch.GetHashCode(), sameInstant.GetHashCode());
    }

    [Fact]
    public void Two_epochs_one_tick_apart_are_different() {
        // Arrange
        Epoch anyEpoch    = Any.Epoch().Generate();
        Epoch oneTickLater = anyEpoch.Add(TimeSpan.FromTicks(1));

        // Act & Assert
        Assert.NotEqual(anyEpoch, oneTickLater);
    }

    [Fact]
    public void An_epoch_is_never_equal_to_null_or_to_something_that_is_not_an_epoch() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();

        // Act & Assert
        Assert.False(anyEpoch.Equals((Epoch?)null));
        Assert.False(anyEpoch.Equals((object)anyEpoch.Utc));
    }

    [Fact]
    public void An_epoch_is_written_in_iso_8601_to_the_millisecond_with_a_trailing_Z() {
        // Arrange
        Epoch epoch = new(new DateTimeOffset(2026, 3, 14, 15, 9, 26, 535, TimeSpan.Zero));

        // Act & Assert
        Assert.Equal("2026-03-14T15:09:26.535Z", epoch.ToString());
    }

}
