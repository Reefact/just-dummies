using System;

using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Discovery;

public sealed class EpochTests {

    [Fact]
    public void An_epoch_refuses_an_instant_that_is_not_expressed_in_UTC() {
        // Arrange
        DateTimeOffset anyParisInstant = Any.DateTimeOffset()
                                            .AfterOrEqualTo(Epoch.SpaceAgeStart)
                                            .WithOffset(TimeSpan.FromHours(2))
                                            .Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Epoch(anyParisInstant));

        // Assert
        Assert.Equal("utc", refusal.ParamName);
    }

    [Fact]
    public void An_epoch_refuses_an_instant_before_the_launch_of_Sputnik() {
        // Arrange
        DateTimeOffset anyPreSpaceAgeInstant = Any.DateTimeOffset().Before(Epoch.SpaceAgeStart).Generate();

        // Act
        Action creation = () => _ = new Epoch(anyPreSpaceAgeInstant);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(creation);
    }

    [Fact]
    public void Adding_ninety_seconds_puts_the_new_epoch_ninety_seconds_after_the_first() {
        // Arrange
        Epoch anyEpoch = Any.Epoch().Generate();

        // Act
        Epoch later = anyEpoch.Add(TimeSpan.FromSeconds(90));

        // Assert
        Assert.Equal(90, later.SecondsSince(anyEpoch), precision: 6);
    }

}
