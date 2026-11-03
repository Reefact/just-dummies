using ColdChain.Domain;

using Freight.Domain;

using JustDummies;

using Xunit;

namespace ColdChain.Tests;

public sealed class LegCustodyTests {

    private static readonly DateTimeOffset Acceptance = new DateTimeOffset(2026, 3, 2, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_custody_keeps_everything_it_was_built_with_and_starts_open() {
        // Arrange
        Leg         anyLeg      = new AnyLeg().Generate();
        CustodianId anyCustodian = new AnyCustodianId().Generate();

        // Act
        LegCustody custody = new LegCustody(2, anyLeg, anyCustodian, Acceptance);

        // Assert
        Assert.Equal(2, custody.LegIndex);
        Assert.Same(anyLeg, custody.Leg);
        Assert.Same(anyCustodian, custody.Custodian);
        Assert.Equal(Acceptance, custody.AcceptedAt);
        Assert.Null(custody.ReleasedAt);
        Assert.True(custody.IsOpen);
    }

    [Fact]
    public void A_negative_leg_index_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new LegCustody(-1, new AnyLeg().Generate(), new AnyCustodianId().Generate(), Acceptance));

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Fact]
    public void Leg_index_zero_is_the_first_leg() {
        // Act
        LegCustody custody = new LegCustody(0, new AnyLeg().Generate(), new AnyCustodianId().Generate(), Acceptance);

        // Assert
        Assert.Equal(0, custody.LegIndex);
    }

    [Fact]
    public void A_leg_and_a_custodian_are_required() {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new LegCustody(0, null!, new AnyCustodianId().Generate(), Acceptance));
        Assert.Throws<ArgumentNullException>(() => new LegCustody(0, new AnyLeg().Generate(), null!, Acceptance));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(60, true)]
    [InlineData(100000, true)]
    public void An_open_custody_covers_everything_from_its_acceptance_on(int minutesAfterAcceptance, bool covers) {
        // Arrange
        LegCustody custody = new LegCustody(0, new AnyLeg().Generate(), new AnyCustodianId().Generate(), Acceptance);

        // Act
        bool actual = custody.Covers(Acceptance.AddMinutes(minutesAfterAcceptance));

        // Assert
        Assert.Equal(covers, actual);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(479, true)]
    [InlineData(480, false)]
    [InlineData(481, false)]
    public void A_closed_custody_covers_from_acceptance_included_to_release_excluded(int minutesAfterAcceptance, bool covers) {
        // Arrange: a single-leg journey is handed over on a two-leg one, so the first custody gets released.
        Journey journey = Journey.Planned(new JourneyOptions { Legs = 2 }).Dispatched();
        journey.Consignment.HandOverToNextLeg(new AnyCustodianId().Generate(), Journey.DispatchedAt.AddMinutes(480));
        LegCustody released = journey.Consignment.Custody[0];

        // Act
        bool actual = released.Covers(Journey.DispatchedAt.AddMinutes(minutesAfterAcceptance));

        // Assert
        Assert.Equal(covers, actual);
    }

    [Fact]
    public void A_custody_reads_back_its_leg_and_its_custodian() {
        // Arrange
        UnLocode    origin      = new UnLocode("FRLYS");
        UnLocode    destination = new UnLocode("DEHAM");
        Leg         leg         = new AnyLeg().WithOrigin(origin).WithDestination(destination).Generate();
        CustodianId custodian   = new CustodianId("FR-GDP-04217");

        // Act
        string text = new LegCustody(1, leg, custodian, Acceptance).ToString();

        // Assert
        Assert.Equal("leg 1 FRLYS->DEHAM: FR-GDP-04217", text);
    }

}
