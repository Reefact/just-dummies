using ColdChain.Domain;
using ColdChain.Tests.Expert.Generators;
using Freight.Domain;
using JustDummies;
using Xunit;

namespace ColdChain.Tests.Expert.Entities;

public sealed class LegCustodyTests {

    private static readonly DateTimeOffset AcceptedAt = new DateTimeOffset(2026, 3, 9, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_custody_is_open_until_it_is_released() {
        // Act
        LegCustody custody = Any.LegCustody().Generate();

        // Assert
        Assert.True(custody.IsOpen);
        Assert.Null(custody.ReleasedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void A_custody_belongs_to_a_leg_of_the_itinerary_from_the_first_on(int legIndex) {
        // Act
        LegCustody custody = Any.LegCustody().WithLegIndex(legIndex).Generate();

        // Assert
        Assert.Equal(legIndex, custody.LegIndex);
    }

    [Fact]
    public void A_negative_leg_index_is_refused() {
        // Arrange
        AnyLegCustody custodies = Any.LegCustody().WithLegIndex(-1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => custodies.Generate());
    }

    [Fact]
    public void A_missing_leg_or_custodian_is_refused() {
        // Arrange
        Leg         anyLeg       = Any.Leg().Generate();
        CustodianId anyCustodian = Any.CustodianId().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new LegCustody(0, null!, anyCustodian, AcceptedAt));
        Assert.Throws<ArgumentNullException>(() => new LegCustody(0, anyLeg, null!, AcceptedAt));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(60 * 24 * 365, true)]
    public void An_open_custody_covers_every_instant_from_its_acceptance_on(int minutesAfterAcceptance, bool expectedCovered) {
        // Arrange
        LegCustody custody = Any.LegCustody().WithAcceptedAt(AcceptedAt).Generate();

        // Assert
        Assert.Equal(expectedCovered, custody.Covers(AcceptedAt.AddMinutes(minutesAfterAcceptance)));
    }

}
