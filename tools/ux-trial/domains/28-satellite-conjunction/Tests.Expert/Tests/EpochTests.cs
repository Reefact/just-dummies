using JustDummies;
using SatelliteConjunction.Domain;
using SatelliteConjunction.Tests.Expert.Arbitrary;

namespace SatelliteConjunction.Tests.Expert.Tests;

public sealed class EpochTests {

    [Fact]
    public void An_epoch_with_a_non_zero_offset_is_refused() {
        // Arrange
        DateTimeOffset anyLocalInstant = Any.DateTimeOffset()
                                            .WithOffsetBetween(TimeSpan.FromHours(1), TimeSpan.FromHours(14))
                                            .Generate();

        // Act
        Action creating = () => new Epoch(anyLocalInstant);

        // Assert
        Assert.Throws<ArgumentException>("utc", creating);
    }

    [Fact]
    public void An_epoch_before_Sputnik_is_refused() {
        // Arrange
        DateTimeOffset dayBeforeSputnik = new(1957, 10, 3, 0, 0, 0, TimeSpan.Zero);

        // Act
        Action creating = () => new Epoch(dayBeforeSputnik);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("utc", creating);
    }

    [Fact]
    public void Adding_ninety_seconds_puts_the_new_epoch_ninety_seconds_later() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();

        // Act
        Epoch later = anyEpoch.Add(TimeSpan.FromSeconds(90));

        // Assert
        Assert.Equal(90, later.SecondsSince(anyEpoch), precision: 6);
    }

}
