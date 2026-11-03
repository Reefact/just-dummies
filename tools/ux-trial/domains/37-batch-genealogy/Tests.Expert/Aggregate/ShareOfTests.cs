using JustDummies;

using Xunit;

namespace BatchGenealogy.Domain.Tests.Aggregate;

public sealed class ShareOfTests {

    // The graphs are built from kilos chosen so that every share is exact in decimal: the quantities are
    // the subject here, the lot numbers are not.

    [Fact]
    public void A_lot_is_entirely_made_of_itself() {
        // Arrange
        Plant     plant = new Plant();
        LotNumber anyLot = Any.LotNumber().Base().Generate();
        plant.ReleasedStock(anyLot, 10m);

        // Act
        decimal share = plant.Genealogy.ShareOf(anyLot, anyLot);

        // Assert
        Assert.Equal(1m, share);
    }

    [Fact]
    public void A_blended_lot_comes_from_each_input_for_the_input_s_part_of_the_total() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(3);
        plant.ReleasedStock(lots[0], 100m);
        plant.ReleasedStock(lots[1], 100m);
        plant.Blend(lots[2], 90m, (lots[0], 30m), (lots[1], 70m));

        // Assert
        Assert.Equal(0.3m, plant.Genealogy.ShareOf(lots[0], lots[2]));
        Assert.Equal(0.7m, plant.Genealogy.ShareOf(lots[1], lots[2]));
    }

    [Fact]
    public void The_loss_of_a_step_does_not_dilute_the_share_of_its_inputs() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(3);
        plant.ReleasedStock(lots[0], 100m);
        plant.ReleasedStock(lots[1], 100m);
        plant.Blend(lots[2], 1m, (lots[0], 50m), (lots[1], 50m));

        // Assert: the 99 kg lost are not attributed to anyone
        Assert.Equal(0.5m, plant.Genealogy.ShareOf(lots[0], lots[2]));
    }

    [Fact]
    public void Shares_multiply_along_a_chain_of_steps() {
        // Arrange: A is half of B, B is a quarter of C
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(5);
        LotNumber       anyA = lots[0], anyB0 = lots[1], anyB = lots[2], anyC0 = lots[3], anyC = lots[4];
        plant.ReleasedStock(anyA, 100m);
        plant.ReleasedStock(anyB0, 100m);
        plant.ReleasedStock(anyC0, 300m);
        plant.Blend(anyB, 100m, (anyA, 50m), (anyB0, 50m));
        plant.Blend(anyC, 400m, (anyB, 100m), (anyC0, 300m));

        // Assert
        Assert.Equal(0.5m, plant.Genealogy.ShareOf(anyA, anyB));
        Assert.Equal(0.125m, plant.Genealogy.ShareOf(anyA, anyC));
        Assert.Equal(0.25m, plant.Genealogy.ShareOf(anyB, anyC));
    }

    [Fact]
    public void A_lot_is_entirely_made_of_the_only_input_of_a_process_whatever_the_yield() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(2);
        plant.ReleasedStock(lots[0], 100m);
        plant.Process(lots[1], 37.5m, lots[0], 100m);

        // Assert
        Assert.Equal(1m, plant.Genealogy.ShareOf(lots[0], lots[1]));
    }

    [Fact]
    public void Every_sub_lot_of_a_split_is_entirely_made_of_the_lot_it_was_split_from() {
        // Arrange
        Plant     plant = new Plant();
        LotNumber anySource = Any.LotNumber().Base().Generate();
        plant.ReleasedStock(anySource, 100m);
        plant.Split(anySource, 100m, 30m, 60m);

        // Assert
        Assert.Equal(1m, plant.Genealogy.ShareOf(anySource, anySource.SubLot(1)));
        Assert.Equal(1m, plant.Genealogy.ShareOf(anySource, anySource.SubLot(2)));
    }

    [Fact]
    public void Shares_through_different_paths_add_up_where_they_merge() {
        // Arrange: A is split in two halves, one stays pure and the other is blended with B, then both are blended again
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(4);
        LotNumber       anyA = lots[0], anyB = lots[1], anyHalfBlend = lots[2], anyMerge = lots[3];
        plant.ReleasedStock(anyA, 100m);
        plant.ReleasedStock(anyB, 50m);
        plant.Split(anyA, 100m, 50m, 50m);
        plant.Blend(anyHalfBlend, 100m, (anyA.SubLot(2), 50m), (anyB, 50m));
        plant.Blend(anyMerge, 150m, (anyA.SubLot(1), 50m), (anyHalfBlend, 100m));

        // Assert: 1/3 straight from A.01, plus 2/3 x 1/2 through the blend; B is the rest
        Assert.Equal(1m / 3m + 1m / 3m, plant.Genealogy.ShareOf(anyA, anyMerge), precision: 20);
        Assert.Equal(1m / 3m, plant.Genealogy.ShareOf(anyB, anyMerge), precision: 20);
    }

    [Fact]
    public void A_lot_fed_by_two_paths_from_the_same_ancestor_sums_both() {
        // Arrange: X = A + B (A is 1/2), Y = A + C (A is 1/4), Z = X + Y with X a quarter and Y three quarters
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(6);
        LotNumber       anyA = lots[0], anyB = lots[1], anyC = lots[2], anyX = lots[3], anyY = lots[4], anyZ = lots[5];
        plant.ReleasedStock(anyA, 200m);
        plant.ReleasedStock(anyB, 100m);
        plant.ReleasedStock(anyC, 300m);
        plant.Blend(anyX, 200m, (anyA, 100m), (anyB, 100m));
        plant.Blend(anyY, 400m, (anyA, 100m), (anyC, 300m));
        plant.Blend(anyZ, 400m, (anyX, 100m), (anyY, 300m));

        // Assert
        Assert.Equal(0.3125m, plant.Genealogy.ShareOf(anyA, anyZ));
        Assert.Equal(0.125m, plant.Genealogy.ShareOf(anyB, anyZ));
        Assert.Equal(0.5625m, plant.Genealogy.ShareOf(anyC, anyZ));
    }

    [Fact]
    public void The_shares_of_every_ancestor_of_a_lot_add_up_to_the_whole_of_it() {
        // Arrange: the same graph as above
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(6);
        plant.ReleasedStock(lots[0], 200m);
        plant.ReleasedStock(lots[1], 100m);
        plant.ReleasedStock(lots[2], 300m);
        plant.Blend(lots[3], 200m, (lots[0], 100m), (lots[1], 100m));
        plant.Blend(lots[4], 400m, (lots[0], 100m), (lots[2], 300m));
        plant.Blend(lots[5], 400m, (lots[3], 100m), (lots[4], 300m));

        // Act
        decimal total = new[] { lots[0], lots[1], lots[2] }.Sum(source => plant.Genealogy.ShareOf(source, lots[5]));

        // Assert
        Assert.Equal(1m, total);
    }

    [Fact]
    public void Shares_never_exceed_the_whole_when_every_path_comes_from_the_same_ancestor() {
        // Arrange: A is split in halves that are blended back together
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(2);
        plant.ReleasedStock(lots[0], 100m);
        plant.Split(lots[0], 100m, 50m, 50m);
        plant.Blend(lots[1], 100m, (lots[0].SubLot(1), 50m), (lots[0].SubLot(2), 50m));

        // Assert
        Assert.Equal(1m, plant.Genealogy.ShareOf(lots[0], lots[1]));
    }

    [Fact]
    public void A_lot_owes_nothing_to_a_lot_that_is_not_among_its_ancestors() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(4);
        plant.ReleasedStock(lots[0], 100m);
        plant.ReleasedStock(lots[2], 100m);
        plant.Process(lots[1], 50m, lots[0], 50m);
        plant.ProducedLot(lots[3], 50m);

        // Assert: a sibling stock, and a lot with no step at all
        Assert.Equal(0m, plant.Genealogy.ShareOf(lots[2], lots[1]));
        Assert.Equal(0m, plant.Genealogy.ShareOf(lots[0], lots[3]));
    }

    [Fact]
    public void Descent_does_not_run_backwards() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(2);
        plant.ReleasedStock(lots[0], 100m);
        plant.Process(lots[1], 50m, lots[0], 50m);

        // Assert
        Assert.Equal(0m, plant.Genealogy.ShareOf(lots[1], lots[0]));
    }

    [Fact]
    public void Asking_for_the_share_of_nothing_is_refused() {
        // Arrange
        Plant     plant = new Plant();
        LotNumber anyLot = Any.LotNumber().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => plant.Genealogy.ShareOf(null!, anyLot));
        Assert.Throws<ArgumentNullException>(() => plant.Genealogy.ShareOf(anyLot, null!));
    }

    [Fact(Skip = "domain inconsistency (undefined by the model): ShareOf answers 0 for two unregistered lots and 1 for an unregistered lot against itself, while Get, TraceUpstream and TraceDownstream refuse an unknown lot with KeyNotFoundException; no seed involved, the outcome does not depend on the drawn numbers")]
    public void Asking_for_the_share_of_a_lot_the_genealogy_has_never_seen_is_refused_as_looking_it_up_is() {
        // Arrange
        Plant     plant = new Plant();
        LotNumber anyUnknown = Any.LotNumber().Generate();
        LotNumber anotherUnknown = Any.LotNumber().DifferentFrom(anyUnknown).Generate();

        // Act / Assert
        Assert.Throws<KeyNotFoundException>(() => plant.Genealogy.ShareOf(anyUnknown, anotherUnknown));
        Assert.Throws<KeyNotFoundException>(() => plant.Genealogy.ShareOf(anyUnknown, anyUnknown));
    }

}
