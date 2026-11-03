using JustDummies;

using Xunit;

namespace BatchGenealogy.Domain.Tests.Generators;

/// <summary>
///     The suite leans on these generators for "a valid X"; if one drifts from the domain, it should say
///     so here, by name, rather than as a constructor refusal in a test about something else.
/// </summary>
public sealed class GeneratorTests {

    [Fact]
    public void A_drawn_lot_always_has_a_shelf_life_within_the_limit() {
        // Act
        List<Lot> lots = Any.ListOf(Any.Lot()).WithCount(300).Generate();

        // Assert
        Assert.All(lots, lot => Assert.InRange(lot.ExpiresOn.DayNumber - lot.ManufacturedOn.DayNumber, 1, Lot.MaximumShelfLifeDays));
    }

    [Fact]
    public void A_drawn_lot_is_quarantined_and_holds_something() {
        // Act
        List<Lot> lots = Any.ListOf(Any.Lot()).WithCount(300).Generate();

        // Assert
        Assert.All(lots, lot => {
            Assert.Equal(LotStatus.Quarantined, lot.Status);
            Assert.False(lot.InitialQuantity.IsZero);
        });
    }

    [Fact]
    public void A_pinned_expiry_date_leaves_a_manufacture_date_within_the_shelf_life_before_it() {
        // Arrange
        DateOnly expiry = new DateOnly(2026, 3, 10);

        // Act
        Lot lot = Any.Lot().WithExpiresOn(expiry).Generate();

        // Assert
        Assert.Equal(expiry, lot.ExpiresOn);
        Assert.InRange(expiry.DayNumber - lot.ManufacturedOn.DayNumber, 1, Lot.MaximumShelfLifeDays);
    }

    [Fact]
    public void A_pinned_manufacture_date_leaves_an_expiry_date_within_the_shelf_life_after_it() {
        // Arrange
        DateOnly manufacture = new DateOnly(2026, 3, 10);

        // Act
        Lot lot = Any.Lot().WithManufacturedOn(manufacture).Generate();

        // Assert
        Assert.Equal(manufacture, lot.ManufacturedOn);
        Assert.InRange(lot.ExpiresOn.DayNumber - manufacture.DayNumber, 1, Lot.MaximumShelfLifeDays);
    }

    [Fact]
    public void A_drawn_provenance_link_never_loops_a_lot_onto_itself() {
        // Act
        List<ProvenanceLink> links = Any.ListOf(Any.ProvenanceLink()).WithCount(300).Generate();

        // Assert
        Assert.All(links, link => Assert.NotEqual(link.Source, link.Target));
    }

}
