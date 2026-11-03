using JustDummies;

using Xunit;

namespace BatchGenealogy.Domain.Tests.Aggregate;

public sealed class TraceTests {

    // ---- upstream: what a product was made from -----------------------------------------------------

    [Fact]
    public void A_lot_that_was_not_made_from_anything_has_no_ancestors() {
        // Arrange
        Plant     plant = new Plant();
        LotNumber anyStock = Any.LotNumber().Base().Generate();
        plant.ReleasedStock(anyStock, 10m);

        // Act
        IReadOnlyList<TraceEntry> upstream = plant.Genealogy.TraceUpstream(anyStock);

        // Assert
        Assert.Empty(upstream);
    }

    [Fact]
    public void The_ancestors_of_a_blend_carry_their_share_and_the_quantity_of_each_the_product_contains() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(3);
        LotNumber       anyApi = lots[0], anyExcipient = lots[1], anyBlend = lots[2];
        plant.ReleasedStock(anyApi, 100m);
        plant.ReleasedStock(anyExcipient, 100m);
        plant.Blend(anyBlend, 80m, (anyApi, 25m), (anyExcipient, 75m));

        // Act
        IReadOnlyList<TraceEntry> upstream = plant.Genealogy.TraceUpstream(anyBlend);

        // Assert
        Assert.Equal(2, upstream.Count);
        TraceEntry api = Assert.Single(upstream, entry => entry.Lot.Equals(anyApi));
        TraceEntry excipient = Assert.Single(upstream, entry => entry.Lot.Equals(anyExcipient));
        Assert.Equal((1, 0.25m, Plant.Kg(20m)), (api.Depth, api.Share, api.ExposedQuantity));
        Assert.Equal((1, 0.75m, Plant.Kg(60m)), (excipient.Depth, excipient.Share, excipient.ExposedQuantity));
    }

    [Fact]
    public void The_product_itself_is_not_among_its_ancestors() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(2);
        plant.ReleasedStock(lots[0], 10m);
        plant.Process(lots[1], 10m, lots[0], 10m);

        // Act
        IReadOnlyList<TraceEntry> upstream = plant.Genealogy.TraceUpstream(lots[1]);

        // Assert
        Assert.DoesNotContain(upstream, entry => entry.Lot.Equals(lots[1]));
    }

    [Fact]
    public void The_ancestors_of_a_chain_are_as_deep_as_the_steps_between_them_and_the_product() {
        // Arrange: A -> B -> C -> D, each a plain process
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(4);
        plant.ReleasedStock(lots[0], 100m);
        plant.Process(lots[1], 100m, lots[0], 100m);
        plant.Process(lots[2], 100m, lots[1], 100m);
        plant.Process(lots[3], 100m, lots[2], 100m);

        // Act
        IReadOnlyList<TraceEntry> upstream = plant.Genealogy.TraceUpstream(lots[3]);

        // Assert
        Assert.Equal([(lots[2], 1), (lots[1], 2), (lots[0], 3)], upstream.Select(entry => (entry.Lot, entry.Depth)));
    }

    [Fact]
    public void An_ancestor_reached_by_several_paths_is_listed_once_at_its_shortest_depth_with_the_sum_of_its_shares() {
        // Arrange: A feeds D directly (50 of 100) and through B (the other 50 is B, itself made of A alone)
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(3);
        LotNumber       anyA = lots[0], anyB = lots[1], anyD = lots[2];
        plant.ReleasedStock(anyA, 200m);
        plant.Process(anyB, 50m, anyA, 50m);
        plant.Blend(anyD, 90m, (anyA, 50m), (anyB, 50m));

        // Act
        IReadOnlyList<TraceEntry> upstream = plant.Genealogy.TraceUpstream(anyD);

        // Assert
        TraceEntry fromA = Assert.Single(upstream, entry => entry.Lot.Equals(anyA));
        TraceEntry fromB = Assert.Single(upstream, entry => entry.Lot.Equals(anyB));
        Assert.Equal((1, 1m, Plant.Kg(90m)), (fromA.Depth, fromA.Share, fromA.ExposedQuantity));
        Assert.Equal((1, 0.5m, Plant.Kg(45m)), (fromB.Depth, fromB.Share, fromB.ExposedQuantity));
    }

    [Fact]
    public void Ancestors_are_listed_nearest_first_then_by_lot_number() {
        // Arrange: the order depends on the text of the numbers, so these ones are chosen
        Plant     plant   = new Plant();
        LotNumber farZ    = new LotNumber("ZZZ-000001");
        LotNumber nearB   = new LotNumber("BBB-000001");
        LotNumber nearA   = new LotNumber("AAA-000001");
        LotNumber middle  = new LotNumber("MMM-000001");
        LotNumber product = new LotNumber("PRD-000001");
        plant.ReleasedStock(farZ, 100m);
        plant.ReleasedStock(nearA, 100m);
        plant.ReleasedStock(nearB, 100m);
        plant.Process(middle, 100m, farZ, 100m);
        plant.Blend(product, 100m, (middle, 50m), (nearB, 25m), (nearA, 25m));

        // Act
        IReadOnlyList<TraceEntry> upstream = plant.Genealogy.TraceUpstream(product);

        // Assert
        Assert.Equal([nearA, nearB, middle, farZ], upstream.Select(entry => entry.Lot));
    }

    [Fact]
    public void The_quantity_an_ancestor_contributes_is_expressed_in_the_unit_the_product_was_registered_in() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(2);
        plant.ReleasedStock(lots[0], 10m);
        plant.Process(lots[1], 8m, lots[0], 10m);

        // Act
        TraceEntry entry = Assert.Single(plant.Genealogy.TraceUpstream(lots[1]));

        // Assert
        Assert.Equal(QuantityUnit.Kilogram, entry.ExposedQuantity.Unit);
        Assert.Equal(8m, entry.ExposedQuantity.Amount);
    }

    [Fact]
    public void Tracing_a_product_upstream_that_is_unknown_is_refused() {
        // Arrange
        Plant     plant = new Plant();
        LotNumber anyUnknown = Any.LotNumber().Generate();

        // Act / Assert
        Assert.Throws<KeyNotFoundException>(() => plant.Genealogy.TraceUpstream(anyUnknown));
        Assert.Throws<ArgumentNullException>(() => plant.Genealogy.TraceUpstream(null!));
    }

    // ---- downstream: where a material went -------------------------------------------------------------

    [Fact]
    public void A_lot_nothing_was_made_from_reaches_no_other_lot() {
        // Arrange
        Plant     plant = new Plant();
        LotNumber anyStock = Any.LotNumber().Base().Generate();
        plant.ReleasedStock(anyStock, 10m);

        // Act
        IReadOnlyList<TraceEntry> downstream = plant.Genealogy.TraceDownstream(anyStock);

        // Assert
        Assert.Empty(downstream);
    }

    [Fact]
    public void The_descendants_of_a_material_carry_the_share_of_it_they_are_made_of_and_the_quantity_of_it_they_hold() {
        // Arrange: the material is 25 % of a 80 kg blend, which is itself half of a 400 kg lot
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(4);
        LotNumber       anyMaterial = lots[0], anyOther = lots[1], anyBlend = lots[2], anyFinished = lots[3];
        plant.ReleasedStock(anyMaterial, 100m);
        plant.ReleasedStock(anyOther, 100m);
        plant.Blend(anyBlend, 80m, (anyMaterial, 25m), (anyOther, 75m));
        plant.Process(anyFinished, 80m, anyBlend, 80m);

        // Act
        IReadOnlyList<TraceEntry> downstream = plant.Genealogy.TraceDownstream(anyMaterial);

        // Assert
        Assert.Equal(2, downstream.Count);
        TraceEntry blend = Assert.Single(downstream, entry => entry.Lot.Equals(anyBlend));
        TraceEntry finished = Assert.Single(downstream, entry => entry.Lot.Equals(anyFinished));
        Assert.Equal((1, 0.25m, Plant.Kg(20m)), (blend.Depth, blend.Share, blend.ExposedQuantity));
        Assert.Equal((2, 0.25m, Plant.Kg(20m)), (finished.Depth, finished.Share, finished.ExposedQuantity));
    }

    [Fact]
    public void The_material_itself_is_not_among_its_descendants() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(2);
        plant.ReleasedStock(lots[0], 10m);
        plant.Process(lots[1], 10m, lots[0], 10m);

        // Act
        IReadOnlyList<TraceEntry> downstream = plant.Genealogy.TraceDownstream(lots[0]);

        // Assert
        Assert.DoesNotContain(downstream, entry => entry.Lot.Equals(lots[0]));
    }

    [Fact]
    public void Every_sub_lot_of_a_split_is_a_descendant_made_entirely_of_the_material() {
        // Arrange
        Plant     plant = new Plant();
        LotNumber anySource = Any.LotNumber().Base().Generate();
        plant.ReleasedStock(anySource, 100m);
        plant.Split(anySource, 100m, 30m, 60m);

        // Act
        IReadOnlyList<TraceEntry> downstream = plant.Genealogy.TraceDownstream(anySource);

        // Assert
        Assert.Equal([(anySource.SubLot(1), 1, 1m, Plant.Kg(30m)), (anySource.SubLot(2), 1, 1m, Plant.Kg(60m))],
                     downstream.Select(entry => (entry.Lot, entry.Depth, entry.Share, entry.ExposedQuantity)));
    }

    [Fact]
    public void A_descendant_reached_by_several_paths_is_listed_once_at_its_shortest_depth_with_the_sum_of_its_shares() {
        // Arrange: the same graph as upstream, seen from A
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(3);
        LotNumber       anyA = lots[0], anyB = lots[1], anyD = lots[2];
        plant.ReleasedStock(anyA, 200m);
        plant.Process(anyB, 50m, anyA, 50m);
        plant.Blend(anyD, 90m, (anyA, 50m), (anyB, 50m));

        // Act
        IReadOnlyList<TraceEntry> downstream = plant.Genealogy.TraceDownstream(anyA);

        // Assert
        Assert.Equal(2, downstream.Count);
        TraceEntry toD = Assert.Single(downstream, entry => entry.Lot.Equals(anyD));
        Assert.Equal((1, 1m, Plant.Kg(90m)), (toD.Depth, toD.Share, toD.ExposedQuantity));
    }

    [Fact]
    public void Descendants_are_listed_nearest_first_then_by_lot_number() {
        // Arrange
        Plant     plant  = new Plant();
        LotNumber source = new LotNumber("SRC-000001");
        LotNumber nearB  = new LotNumber("BBB-000001");
        LotNumber nearA  = new LotNumber("AAA-000001");
        LotNumber far    = new LotNumber("AAA-000002");
        plant.ReleasedStock(source, 100m);
        plant.Process(nearB, 10m, source, 10m);
        plant.Process(nearA, 10m, source, 10m);
        plant.Process(far, 10m, nearB, 10m);

        // Act
        IReadOnlyList<TraceEntry> downstream = plant.Genealogy.TraceDownstream(source);

        // Assert
        Assert.Equal([nearA, nearB, far], downstream.Select(entry => entry.Lot));
    }

    [Fact]
    public void Tracing_a_material_downstream_that_is_unknown_is_refused() {
        // Arrange
        Plant     plant = new Plant();
        LotNumber anyUnknown = Any.LotNumber().Generate();

        // Act / Assert
        Assert.Throws<KeyNotFoundException>(() => plant.Genealogy.TraceDownstream(anyUnknown));
        Assert.Throws<ArgumentNullException>(() => plant.Genealogy.TraceDownstream(null!));
    }

}
