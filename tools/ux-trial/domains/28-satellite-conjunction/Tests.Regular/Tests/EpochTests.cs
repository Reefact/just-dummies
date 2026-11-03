using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Regular;

public sealed class EpochTests {

    private static readonly DateTimeOffset LastInstant = new(2100, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void An_epoch_keeps_the_utc_instant_it_was_built_from() {
        // Arrange
        DateTimeOffset anyUtc = Any.DateTimeOffset().Between(Epoch.SpaceAgeStart, LastInstant).Generate();

        // Act
        Epoch epoch = new(anyUtc);

        // Assert
        Assert.Equal(anyUtc, epoch.Utc);
    }

    [Fact]
    public void The_first_instant_of_the_space_age_is_accepted() {
        // Arrange
        DateTimeOffset sputnik = new(1957, 10, 4, 0, 0, 0, TimeSpan.Zero);

        // Act
        Epoch epoch = new(sputnik);

        // Assert
        Assert.Equal(sputnik, epoch.Utc);
    }

    [Fact]
    public void One_tick_before_the_space_age_is_refused() {
        // Arrange
        DateTimeOffset beforeSputnik = new DateTimeOffset(1957, 10, 4, 0, 0, 0, TimeSpan.Zero).AddTicks(-1);

        // Act
        Exception? refusal = Record.Exception(() => new Epoch(beforeSputnik));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void An_instant_before_the_space_age_is_refused() {
        // Arrange
        DateTimeOffset anyPrehistoricInstant = Any.DateTimeOffset().Before(Epoch.SpaceAgeStart).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Epoch(anyPrehistoricInstant));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void An_instant_expressed_with_a_positive_offset_is_refused() {
        // Arrange
        TimeSpan       anyOffset  = Any.TimeSpan().Between(TimeSpan.FromMinutes(1), TimeSpan.FromHours(14)).WithGranularity(TimeSpan.FromMinutes(1)).Generate();
        DateTimeOffset anyInstant = Any.DateTimeOffset().Between(Epoch.SpaceAgeStart.AddYears(1), LastInstant).WithOffset(anyOffset).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Epoch(anyInstant));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void An_instant_expressed_with_a_negative_offset_is_refused() {
        // Arrange
        TimeSpan       anyOffset  = Any.TimeSpan().Between(TimeSpan.FromHours(-12), TimeSpan.FromMinutes(-1)).WithGranularity(TimeSpan.FromMinutes(1)).Generate();
        DateTimeOffset anyInstant = Any.DateTimeOffset().Between(Epoch.SpaceAgeStart.AddYears(1), LastInstant).WithOffset(anyOffset).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Epoch(anyInstant));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void Adding_a_duration_moves_the_epoch_by_exactly_that_duration() {
        // Arrange
        Epoch    anyEpoch    = Any.Epoch().WithUtc(u => u.Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2050, 1, 1, 0, 0, 0, TimeSpan.Zero))).Generate();
        TimeSpan anyDuration = Any.TimeSpan().Between(TimeSpan.FromDays(-365), TimeSpan.FromDays(365)).Generate();

        // Act
        Epoch moved = anyEpoch.Add(anyDuration);

        // Assert
        Assert.Equal(anyEpoch.Utc + anyDuration, moved.Utc);
        Assert.Equal(anyDuration.TotalSeconds, moved.SecondsSince(anyEpoch));
    }

    [Fact]
    public void Adding_a_duration_that_goes_back_before_the_space_age_is_refused() {
        // Arrange
        Epoch firstDay = new(Epoch.SpaceAgeStart);

        // Act
        Exception? refusal = Record.Exception(() => firstDay.Add(TimeSpan.FromTicks(-1)));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void Seconds_since_is_signed() {
        // Arrange
        Epoch earlier = new(new DateTimeOffset(2024, 3, 1, 12, 0, 0, TimeSpan.Zero));
        Epoch later   = new(new DateTimeOffset(2024, 3, 1, 12, 1, 30, TimeSpan.Zero));

        // Act
        double forward  = later.SecondsSince(earlier);
        double backward = earlier.SecondsSince(later);

        // Assert
        Assert.Equal(90d, forward);
        Assert.Equal(-90d, backward);
    }

    [Fact]
    public void Seconds_since_a_missing_epoch_is_refused() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyEpoch.SecondsSince(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(refusal);
    }

    [Fact]
    public void Epochs_are_ordered_by_their_instant() {
        // Arrange
        Epoch anyEarlier = Any.Epoch().WithUtc(u => u.Before(new DateTimeOffset(2050, 1, 1, 0, 0, 0, TimeSpan.Zero))).Generate();
        Epoch anyLater   = Any.Epoch().WithUtc(u => u.After(anyEarlier.Utc)).Generate();

        // Act
        int ascending  = anyEarlier.CompareTo(anyLater);
        int descending = anyLater.CompareTo(anyEarlier);
        int against    = anyEarlier.CompareTo(null);

        // Assert
        Assert.True(ascending < 0);
        Assert.True(descending > 0);
        Assert.True(against > 0);
    }

    [Fact]
    public void Two_epochs_on_the_same_instant_are_equal_and_hash_alike() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();
        Epoch sameInstant = new(anyEpoch.Utc);

        // Act
        bool equal     = anyEpoch.Equals(sameInstant);
        bool equalAsObject = anyEpoch.Equals((object)sameInstant);

        // Assert
        Assert.True(equal);
        Assert.True(equalAsObject);
        Assert.Equal(anyEpoch.GetHashCode(), sameInstant.GetHashCode());
        Assert.Equal(0, anyEpoch.CompareTo(sameInstant));
    }

    [Fact]
    public void Two_epochs_on_different_instants_are_not_equal() {
        // Arrange
        Epoch anyEpoch   = Any.Epoch().Generate();
        Epoch anyOther   = Any.Epoch().WithUtc(u => u.DifferentFrom(anyEpoch.Utc)).Generate();

        // Act
        bool equal       = anyEpoch.Equals(anyOther);
        bool equalToNull = anyEpoch.Equals(null);

        // Assert
        Assert.False(equal);
        Assert.False(equalToNull);
    }

    [Fact]
    public void An_epoch_is_written_in_iso_8601_with_milliseconds() {
        // Arrange
        Epoch epoch = new(new DateTimeOffset(1999, 12, 31, 23, 59, 58, 123, TimeSpan.Zero));

        // Act
        string text = epoch.ToString();

        // Assert
        Assert.Equal("1999-12-31T23:59:58.123Z", text);
    }

}
