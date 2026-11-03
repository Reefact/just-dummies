using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Discovery;

public sealed class EpochTests {

    [Fact]
    public void An_epoch_keeps_the_utc_instant_it_is_built_from() {
        // Arrange
        DateTimeOffset instant = new DateTimeOffset(2026, 3, 14, 15, 9, 26, 535, TimeSpan.Zero);

        // Act
        Epoch epoch = new Epoch(instant);

        // Assert
        Assert.Equal(instant, epoch.Utc);
    }

    [Theory]
    [InlineData(60)]
    [InlineData(-300)]
    [InlineData(330)]
    public void An_epoch_refuses_an_instant_expressed_with_a_non_zero_offset(int offsetMinutes) {
        // Arrange
        DateTimeOffset anyLocalInstant = Any.DateTimeOffset()
                                            .Between(Epoch.SpaceAgeStart.AddYears(1), new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero))
                                            .WithOffset(TimeSpan.FromMinutes(offsetMinutes))
                                            .Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Epoch(anyLocalInstant));

        // Assert
        Assert.Equal("utc", refusal.ParamName);
    }

    [Fact]
    public void An_epoch_may_be_the_very_launch_of_sputnik() {
        // Act
        Epoch epoch = new Epoch(new DateTimeOffset(1957, 10, 4, 0, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.Equal(Epoch.SpaceAgeStart, epoch.Utc);
    }

    [Fact]
    public void An_epoch_cannot_predate_the_space_age_by_a_single_tick() {
        // Arrange
        DateTimeOffset justBefore = new DateTimeOffset(1957, 10, 4, 0, 0, 0, TimeSpan.Zero).AddTicks(-1);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Epoch(justBefore));

        // Assert
        Assert.Equal("utc", refusal.ParamName);
    }

    [Fact]
    public void An_epoch_refuses_an_instant_before_the_space_age() {
        // Arrange
        DateTimeOffset anyEarlyInstant = Any.DateTimeOffset().Before(new DateTimeOffset(1957, 10, 4, 0, 0, 0, TimeSpan.Zero)).Generate();

        // Act
        Action construct = () => new Epoch(anyEarlyInstant);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void Adding_a_duration_moves_the_epoch_by_exactly_that_duration() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();

        // Act
        Epoch later = anyEpoch.Add(TimeSpan.FromMinutes(90));

        // Assert
        Assert.Equal(5400, later.SecondsSince(anyEpoch));
    }

    [Fact]
    public void Adding_a_negative_duration_moves_the_epoch_back() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().WithUtc(utc => utc.After(new DateTimeOffset(1990, 1, 1, 0, 0, 0, TimeSpan.Zero))).Generate();

        // Act
        Epoch earlier = anyEpoch.Add(TimeSpan.FromSeconds(-30));

        // Assert
        Assert.Equal(-30, earlier.SecondsSince(anyEpoch));
    }

    [Fact]
    public void Adding_a_duration_cannot_carry_the_epoch_before_the_space_age() {
        // Arrange
        Epoch launch = new Epoch(Epoch.SpaceAgeStart);

        // Act
        Action rewind = () => launch.Add(TimeSpan.FromSeconds(-1));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(rewind);
    }

    [Fact]
    public void Seconds_since_an_earlier_epoch_are_positive_and_since_a_later_one_negative() {
        // Arrange
        Epoch anyEarlier = Any.Epoch().Generate();
        Epoch anyLater   = anyEarlier.Add(TimeSpan.FromSeconds(2.5));

        // Act / Assert
        Assert.Equal(2.5, anyLater.SecondsSince(anyEarlier));
        Assert.Equal(-2.5, anyEarlier.SecondsSince(anyLater));
        Assert.Equal(0, anyEarlier.SecondsSince(anyEarlier));
    }

    [Fact]
    public void Seconds_since_nothing_is_refused() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();

        // Act
        Action measure = () => anyEpoch.SecondsSince(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(measure);
    }

    [Fact]
    public void Epochs_are_ordered_chronologically() {
        // Arrange
        Epoch anyEarlier = Any.Epoch().Generate();
        Epoch anyLater   = anyEarlier.Add(TimeSpan.FromTicks(1));

        // Act / Assert
        Assert.True(anyEarlier.CompareTo(anyLater) < 0);
        Assert.True(anyLater.CompareTo(anyEarlier) > 0);
        Assert.Equal(0, anyEarlier.CompareTo(new Epoch(anyEarlier.Utc)));
    }

    [Fact]
    public void Any_epoch_sorts_after_nothing() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();

        // Act / Assert
        Assert.True(anyEpoch.CompareTo(null) > 0);
    }

    [Fact]
    public void Two_epochs_of_the_same_instant_are_equal_and_hash_alike() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();
        Epoch sameInstant = new Epoch(anyEpoch.Utc);

        // Act / Assert
        Assert.Equal(anyEpoch, sameInstant);
        Assert.Equal(anyEpoch.GetHashCode(), sameInstant.GetHashCode());
        Assert.True(anyEpoch.Equals((object)sameInstant));
    }

    [Fact]
    public void Two_epochs_of_different_instants_are_different() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();

        // Act
        Epoch otherEpoch = anyEpoch.Add(TimeSpan.FromTicks(1));

        // Assert
        Assert.NotEqual(anyEpoch, otherEpoch);
        Assert.False(anyEpoch.Equals(null));
    }

    [Fact]
    public void An_epoch_is_written_in_iso_8601_with_milliseconds() {
        // Arrange
        Epoch epoch = new Epoch(new DateTimeOffset(2026, 3, 14, 15, 9, 26, 535, TimeSpan.Zero));

        // Act / Assert
        Assert.Equal("2026-03-14T15:09:26.535Z", epoch.ToString());
    }

}
