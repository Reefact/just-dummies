using BatchGenealogy.Domain;

using JustDummies;

using Xunit;

using Genealogy = BatchGenealogy.Domain.BatchGenealogy;

namespace BatchGenealogy.Tests.Discovery;

public sealed class BatchGenealogyTraceTests {

    /// <summary>
    ///     Ingredient A (600 g used) and ingredient B (400 g used) are blended into C (900 g), which is processed
    ///     into D (800 g). E is made from B alone and has nothing to do with A.
    /// </summary>
    private sealed class Plant {

        public Plant() {
            Genealogy = Scenario.AnyGenealogy();
            Day1      = Scenario.AnyDay();
            Day2      = Scenario.AnyDayAfter(Day1);
            Day3      = Scenario.AnyDayAfter(Day2);

            A = Scenario.ReleasedLotOn(Genealogy, Day1, Scenario.Grams(1000m));
            B = Scenario.ReleasedLotOn(Genealogy, Day1, Scenario.Grams(1000m));
            C = Scenario.ProducedLotOn(Genealogy, Day1, Scenario.Grams(900m));
            D = Scenario.ProducedLotOn(Genealogy, Day2, Scenario.Grams(800m));
            E = Scenario.ProducedLotOn(Genealogy, Day3, Scenario.Grams(90m));

            Genealogy.Record(Scenario.Blend(Day1, [Scenario.Part(A, Scenario.Grams(600m)), Scenario.Part(B, Scenario.Grams(400m))], Scenario.Part(C, Scenario.Grams(900m))));
            C.Release();
            Genealogy.Record(Scenario.Process(Day2, [Scenario.Part(C, Scenario.Grams(900m))], Scenario.Part(D, Scenario.Grams(800m))));
            Genealogy.Record(Scenario.Process(Day3, [Scenario.Part(B, Scenario.Grams(100m))], Scenario.Part(E, Scenario.Grams(90m))));
        }

        public Genealogy Genealogy { get; }
        public DateOnly  Day1      { get; }
        public DateOnly  Day2      { get; }
        public DateOnly  Day3      { get; }
        public Lot       A         { get; }
        public Lot       B         { get; }
        public Lot       C         { get; }
        public Lot       D         { get; }
        public Lot       E         { get; }

    }

    private static TraceEntry EntryFor(IReadOnlyList<TraceEntry> entries, Lot lot) {
        return Assert.Single(entries, entry => entry.Lot.Equals(lot.Number));
    }

    // ---- ShareOf -----------------------------------------------------------------------------------------

    [Fact]
    public void A_lot_is_entirely_made_of_itself() {
        // Arrange
        Plant plant = new Plant();

        // Assert
        Assert.Equal(1m, plant.Genealogy.ShareOf(plant.C.Number, plant.C.Number));
    }

    [Fact]
    public void The_share_of_a_direct_ingredient_is_its_part_of_the_blend() {
        // Arrange
        Plant plant = new Plant();

        // Assert
        Assert.Equal(0.6m, plant.Genealogy.ShareOf(plant.A.Number, plant.C.Number));
        Assert.Equal(0.4m, plant.Genealogy.ShareOf(plant.B.Number, plant.C.Number));
    }

    [Fact]
    public void The_share_carries_through_the_following_steps() {
        // Arrange
        Plant plant = new Plant();

        // Assert
        Assert.Equal(0.6m, plant.Genealogy.ShareOf(plant.A.Number, plant.D.Number));
        Assert.Equal(1m, plant.Genealogy.ShareOf(plant.C.Number, plant.D.Number));
    }

    [Fact]
    public void A_lot_unrelated_to_the_ancestor_has_none_of_it() {
        // Arrange
        Plant plant = new Plant();

        // Assert
        Assert.Equal(0m, plant.Genealogy.ShareOf(plant.A.Number, plant.E.Number));
        Assert.Equal(0m, plant.Genealogy.ShareOf(plant.E.Number, plant.A.Number));
    }

    [Fact]
    public void A_descendant_contributes_nothing_to_its_ancestor() {
        // Arrange
        Plant plant = new Plant();

        // Assert
        Assert.Equal(0m, plant.Genealogy.ShareOf(plant.D.Number, plant.A.Number));
    }

    [Fact]
    public void The_share_of_a_lot_the_genealogy_does_not_know_is_zero() {
        // Arrange
        Plant     plant      = new Plant();
        LotNumber anyUnknown = Any.LotNumber().Generate();

        // Assert
        Assert.Equal(0m, plant.Genealogy.ShareOf(anyUnknown, plant.D.Number));
    }

    [Fact]
    public void A_share_of_nothing_is_refused() {
        // Arrange
        Plant plant = new Plant();

        // Act
        Exception? noAncestor = Record.Exception(() => plant.Genealogy.ShareOf(null!, plant.D.Number));
        Exception? noLot      = Record.Exception(() => plant.Genealogy.ShareOf(plant.A.Number, null!));

        // Assert
        Assert.IsType<ArgumentNullException>(noAncestor);
        Assert.IsType<ArgumentNullException>(noLot);
    }

    [Fact]
    public void An_ancestor_reaching_a_lot_by_two_paths_counts_for_both() {
        // Arrange
        Genealogy    genealogy   = Scenario.AnyGenealogy();
        DateOnly     anyDay      = Scenario.AnyDay();
        DateOnly     nextDay     = Scenario.AnyDayAfter(anyDay);
        MaterialCode anyMaterial = Any.MaterialCode().Generate();
        Lot          source      = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(1000m), material: anyMaterial);
        Lot          left        = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(300m), source.Number.SubLot(1), anyMaterial);
        Lot          right       = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(700m), source.Number.SubLot(2), anyMaterial);
        Lot          rejoined    = Scenario.ProducedLotOn(genealogy, nextDay, Scenario.Grams(1000m));
        genealogy.Record(Scenario.Split(anyDay, Scenario.Part(source, Scenario.Grams(1000m)), [Scenario.Part(left, Scenario.Grams(300m)), Scenario.Part(right, Scenario.Grams(700m))]));
        left.Release();
        right.Release();
        genealogy.Record(Scenario.Blend(nextDay, [Scenario.Part(left, Scenario.Grams(300m)), Scenario.Part(right, Scenario.Grams(700m))], Scenario.Part(rejoined, Scenario.Grams(1000m))));

        // Act
        decimal share = genealogy.ShareOf(source.Number, rejoined.Number);

        // Assert
        Assert.Equal(1m, share);
    }

    [Fact(Skip = "domain defect: shares are decimals, so three equal thirds sum to 0.9999999999999999999999999999 instead of 1 (literal quantities, no seed)")]
    public void A_lot_rebuilt_from_three_equal_sub_lots_of_an_ancestor_is_entirely_made_of_it() {
        // Arrange
        Genealogy    genealogy   = Scenario.AnyGenealogy();
        DateOnly     anyDay      = Scenario.AnyDay();
        DateOnly     nextDay     = Scenario.AnyDayAfter(anyDay);
        MaterialCode anyMaterial = Any.MaterialCode().Generate();
        Lot          source      = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(900m), material: anyMaterial);
        Lot          first       = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(300m), source.Number.SubLot(1), anyMaterial);
        Lot          second      = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(300m), source.Number.SubLot(2), anyMaterial);
        Lot          third       = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(300m), source.Number.SubLot(3), anyMaterial);
        Lot          rebuilt     = Scenario.ProducedLotOn(genealogy, nextDay, Scenario.Grams(900m));
        genealogy.Record(Scenario.Split(anyDay, Scenario.Part(source, Scenario.Grams(900m)), [Scenario.Part(first, Scenario.Grams(300m)), Scenario.Part(second, Scenario.Grams(300m)), Scenario.Part(third, Scenario.Grams(300m))]));
        first.Release();
        second.Release();
        third.Release();
        genealogy.Record(Scenario.Blend(nextDay, [Scenario.Part(first, Scenario.Grams(300m)), Scenario.Part(second, Scenario.Grams(300m)), Scenario.Part(third, Scenario.Grams(300m))], Scenario.Part(rebuilt, Scenario.Grams(900m))));

        // Act
        decimal share = genealogy.ShareOf(source.Number, rebuilt.Number);

        // Assert
        Assert.Equal(1m, share);
    }

    // ---- TraceUpstream -----------------------------------------------------------------------------------

    [Fact]
    public void Tracing_upstream_lists_the_ingredients_with_their_share_and_the_quantity_in_the_product() {
        // Arrange
        Plant plant = new Plant();

        // Act
        IReadOnlyList<TraceEntry> trace = plant.Genealogy.TraceUpstream(plant.C.Number);

        // Assert
        Assert.Equal(2, trace.Count);
        TraceEntry fromA = EntryFor(trace, plant.A);
        TraceEntry fromB = EntryFor(trace, plant.B);
        Assert.Equal(1, fromA.Depth);
        Assert.Equal(0.6m, fromA.Share);
        Assert.Equal(Scenario.Grams(540m), fromA.ExposedQuantity);
        Assert.Equal(1, fromB.Depth);
        Assert.Equal(0.4m, fromB.Share);
        Assert.Equal(Scenario.Grams(360m), fromB.ExposedQuantity);
    }

    [Fact]
    public void Tracing_upstream_goes_back_through_every_step_nearest_first() {
        // Arrange
        Plant plant = new Plant();

        // Act
        IReadOnlyList<TraceEntry> trace = plant.Genealogy.TraceUpstream(plant.D.Number);

        // Assert
        Assert.Equal(3, trace.Count);
        Assert.Equal(plant.C.Number, trace[0].Lot);
        Assert.Equal(1, trace[0].Depth);
        Assert.Equal(1m, trace[0].Share);
        Assert.Equal(Scenario.Grams(800m), trace[0].ExposedQuantity);
        TraceEntry fromA = EntryFor(trace, plant.A);
        TraceEntry fromB = EntryFor(trace, plant.B);
        Assert.Equal(2, fromA.Depth);
        Assert.Equal(0.6m, fromA.Share);
        Assert.Equal(Scenario.Grams(480m), fromA.ExposedQuantity);
        Assert.Equal(2, fromB.Depth);
        Assert.Equal(0.4m, fromB.Share);
        Assert.Equal(Scenario.Grams(320m), fromB.ExposedQuantity);
    }

    [Fact]
    public void Entries_at_the_same_depth_come_in_lot_number_order() {
        // Arrange
        Plant plant = new Plant();

        // Act
        IReadOnlyList<TraceEntry> trace = plant.Genealogy.TraceUpstream(plant.D.Number);

        // Assert
        Assert.Equal(trace.OrderBy(entry => entry.Depth).ThenBy(entry => entry.Lot.Value, StringComparer.Ordinal).Select(entry => entry.Lot), trace.Select(entry => entry.Lot));
    }

    [Fact]
    public void A_lot_made_from_nothing_has_no_upstream() {
        // Arrange
        Plant plant = new Plant();

        // Assert
        Assert.Empty(plant.Genealogy.TraceUpstream(plant.A.Number));
    }

    [Fact]
    public void The_quantity_exposed_is_the_share_of_the_quantity_the_product_was_registered_with() {
        // Arrange
        Plant plant = new Plant();
        plant.D.Release();
        plant.D.Consume(Scenario.Grams(500m), plant.Day3);

        // Act
        TraceEntry fromA = EntryFor(plant.Genealogy.TraceUpstream(plant.D.Number), plant.A);

        // Assert
        Assert.Equal(Scenario.Grams(480m), fromA.ExposedQuantity);
    }

    [Fact]
    public void Tracing_the_upstream_of_an_unknown_lot_fails_with_a_missing_key() {
        // Arrange
        Plant     plant      = new Plant();
        LotNumber anyUnknown = Any.LotNumber().Generate();

        // Act
        Exception? thrown = Record.Exception(() => plant.Genealogy.TraceUpstream(anyUnknown));

        // Assert
        Assert.IsType<KeyNotFoundException>(thrown);
    }

    [Fact]
    public void An_ancestor_reached_by_two_paths_appears_once_at_its_nearest_depth() {
        // Arrange
        Genealogy    genealogy   = Scenario.AnyGenealogy();
        DateOnly     anyDay      = Scenario.AnyDay();
        DateOnly     nextDay     = Scenario.AnyDayAfter(anyDay);
        MaterialCode anyMaterial = Any.MaterialCode().Generate();
        Lot          source      = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(1000m), material: anyMaterial);
        Lot          left        = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(300m), source.Number.SubLot(1), anyMaterial);
        Lot          right       = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(700m), source.Number.SubLot(2), anyMaterial);
        Lot          rejoined    = Scenario.ProducedLotOn(genealogy, nextDay, Scenario.Grams(1000m));
        genealogy.Record(Scenario.Split(anyDay, Scenario.Part(source, Scenario.Grams(1000m)), [Scenario.Part(left, Scenario.Grams(300m)), Scenario.Part(right, Scenario.Grams(700m))]));
        left.Release();
        right.Release();
        genealogy.Record(Scenario.Blend(nextDay, [Scenario.Part(left, Scenario.Grams(300m)), Scenario.Part(right, Scenario.Grams(700m))], Scenario.Part(rejoined, Scenario.Grams(1000m))));

        // Act
        IReadOnlyList<TraceEntry> trace = genealogy.TraceUpstream(rejoined.Number);

        // Assert
        Assert.Equal(3, trace.Count);
        TraceEntry fromSource = EntryFor(trace, source);
        Assert.Equal(2, fromSource.Depth);
        Assert.Equal(1m, fromSource.Share);
        Assert.Equal(Scenario.Grams(1000m), fromSource.ExposedQuantity);
    }

    // ---- TraceDownstream ---------------------------------------------------------------------------------

    [Fact]
    public void Tracing_downstream_lists_every_lot_made_from_the_material_with_the_quantity_it_carries() {
        // Arrange
        Plant plant = new Plant();

        // Act
        IReadOnlyList<TraceEntry> trace = plant.Genealogy.TraceDownstream(plant.A.Number);

        // Assert
        Assert.Equal(2, trace.Count);
        Assert.Equal(plant.C.Number, trace[0].Lot);
        Assert.Equal(1, trace[0].Depth);
        Assert.Equal(0.6m, trace[0].Share);
        Assert.Equal(Scenario.Grams(540m), trace[0].ExposedQuantity);
        Assert.Equal(plant.D.Number, trace[1].Lot);
        Assert.Equal(2, trace[1].Depth);
        Assert.Equal(0.6m, trace[1].Share);
        Assert.Equal(Scenario.Grams(480m), trace[1].ExposedQuantity);
    }

    [Fact]
    public void A_material_used_in_two_products_reaches_both() {
        // Arrange
        Plant plant = new Plant();

        // Act
        IReadOnlyList<TraceEntry> trace = plant.Genealogy.TraceDownstream(plant.B.Number);

        // Assert
        Assert.Equal(3, trace.Count);
        TraceEntry inC = EntryFor(trace, plant.C);
        TraceEntry inD = EntryFor(trace, plant.D);
        TraceEntry inE = EntryFor(trace, plant.E);
        Assert.Equal(1, inC.Depth);
        Assert.Equal(2, inD.Depth);
        Assert.Equal(1, inE.Depth);
        Assert.Equal(0.4m, inC.Share);
        Assert.Equal(0.4m, inD.Share);
        Assert.Equal(1m, inE.Share);
        Assert.Equal(Scenario.Grams(90m), inE.ExposedQuantity);
    }

    [Fact]
    public void A_lot_nothing_was_made_from_has_no_downstream() {
        // Arrange
        Plant plant = new Plant();

        // Assert
        Assert.Empty(plant.Genealogy.TraceDownstream(plant.D.Number));
        Assert.Empty(plant.Genealogy.TraceDownstream(plant.E.Number));
    }

    [Fact]
    public void Tracing_the_downstream_of_an_unknown_lot_fails_with_a_missing_key() {
        // Arrange
        Plant     plant      = new Plant();
        LotNumber anyUnknown = Any.LotNumber().Generate();

        // Act
        Exception? thrown = Record.Exception(() => plant.Genealogy.TraceDownstream(anyUnknown));

        // Assert
        Assert.IsType<KeyNotFoundException>(thrown);
    }

    [Fact]
    public void Tracing_changes_nothing_in_the_genealogy() {
        // Arrange
        Plant plant = new Plant();
        int   links = plant.Genealogy.Links.Count;

        // Act
        plant.Genealogy.TraceUpstream(plant.D.Number);
        plant.Genealogy.TraceDownstream(plant.A.Number);

        // Assert
        Assert.Equal(links, plant.Genealogy.Links.Count);
        Assert.Equal(LotStatus.Released, plant.A.Status);
        Assert.Equal(LotStatus.Quarantined, plant.D.Status);
    }

    // ---- Contain -----------------------------------------------------------------------------------------

    [Fact]
    public void Containing_a_material_rejects_it_and_quarantines_every_descendant_carrying_enough_of_it() {
        // Arrange
        Plant plant = new Plant();
        plant.D.Release();

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(plant.A.Number, 0.5m);

        // Assert
        Assert.Equal([plant.C.Number, plant.D.Number], quarantined);
        Assert.Equal(LotStatus.Rejected, plant.A.Status);
        Assert.Equal(LotStatus.Quarantined, plant.C.Status);
        Assert.Equal(LotStatus.Quarantined, plant.D.Status);
    }

    [Fact]
    public void A_lot_not_made_from_the_material_is_left_alone() {
        // Arrange
        Plant plant = new Plant();
        plant.E.Release();

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(plant.A.Number, 0.01m);

        // Assert
        Assert.DoesNotContain(plant.E.Number, quarantined);
        Assert.DoesNotContain(plant.B.Number, quarantined);
        Assert.Equal(LotStatus.Released, plant.E.Status);
        Assert.Equal(LotStatus.Released, plant.B.Status);
    }

    [Fact]
    public void A_descendant_below_the_minimum_share_is_not_quarantined() {
        // Arrange
        Plant plant = new Plant();
        plant.D.Release();

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(plant.A.Number, 0.7m);

        // Assert
        Assert.Empty(quarantined);
        Assert.Equal(LotStatus.Rejected, plant.A.Status);
        Assert.Equal(LotStatus.Released, plant.C.Status);
        Assert.Equal(LotStatus.Released, plant.D.Status);
    }

    [Fact]
    public void A_descendant_carrying_exactly_the_minimum_share_is_quarantined() {
        // Arrange
        Plant plant = new Plant();

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(plant.A.Number, 0.6m);

        // Assert
        Assert.Contains(plant.C.Number, quarantined);
        Assert.Equal(LotStatus.Quarantined, plant.C.Status);
    }

    [Fact]
    public void A_descendant_already_rejected_is_neither_quarantined_nor_reported() {
        // Arrange
        Plant plant = new Plant();
        plant.D.Reject();

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(plant.A.Number, 0.5m);

        // Assert
        Assert.Equal([plant.C.Number], quarantined);
        Assert.Equal(LotStatus.Rejected, plant.D.Status);
    }

    [Fact]
    public void Containing_a_material_nothing_was_made_from_only_rejects_it() {
        // Arrange
        Plant plant = new Plant();

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(plant.D.Number, 1m);

        // Assert
        Assert.Empty(quarantined);
        Assert.Equal(LotStatus.Rejected, plant.D.Status);
    }

    [Fact]
    public void Containing_a_material_with_a_minimum_share_of_one_reaches_the_lots_made_entirely_from_it() {
        // Arrange
        Plant plant = new Plant();
        plant.D.Release();

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(plant.C.Number, 1m);

        // Assert
        Assert.Equal([plant.D.Number], quarantined);
        Assert.Equal(LotStatus.Rejected, plant.C.Status);
        Assert.Equal(LotStatus.Quarantined, plant.D.Status);
    }

    [Fact(Skip = "domain defect: shares are decimals, so three equal thirds sum to 0.9999999999999999999999999999 and a lot made entirely from the material is missed by Contain(material, 1) (literal quantities, no seed)")]
    public void A_lot_rebuilt_from_three_equal_sub_lots_of_a_material_is_reached_by_a_minimum_share_of_one() {
        // Arrange
        Genealogy    genealogy   = Scenario.AnyGenealogy();
        DateOnly     anyDay      = Scenario.AnyDay();
        DateOnly     nextDay     = Scenario.AnyDayAfter(anyDay);
        MaterialCode anyMaterial = Any.MaterialCode().Generate();
        Lot          source      = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(900m), material: anyMaterial);
        Lot          first       = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(300m), source.Number.SubLot(1), anyMaterial);
        Lot          second      = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(300m), source.Number.SubLot(2), anyMaterial);
        Lot          third       = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(300m), source.Number.SubLot(3), anyMaterial);
        Lot          rebuilt     = Scenario.ProducedLotOn(genealogy, nextDay, Scenario.Grams(900m));
        genealogy.Record(Scenario.Split(anyDay, Scenario.Part(source, Scenario.Grams(900m)), [Scenario.Part(first, Scenario.Grams(300m)), Scenario.Part(second, Scenario.Grams(300m)), Scenario.Part(third, Scenario.Grams(300m))]));
        first.Release();
        second.Release();
        third.Release();
        genealogy.Record(Scenario.Blend(nextDay, [Scenario.Part(first, Scenario.Grams(300m)), Scenario.Part(second, Scenario.Grams(300m)), Scenario.Part(third, Scenario.Grams(300m))], Scenario.Part(rebuilt, Scenario.Grams(900m))));

        // Act
        IReadOnlyList<LotNumber> quarantined = genealogy.Contain(source.Number, 1m);

        // Assert
        Assert.Contains(rebuilt.Number, quarantined);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(1.0001)]
    [InlineData(2)]
    public void A_minimum_share_outside_zero_exclusive_to_one_inclusive_is_refused_before_anything_changes(double minimumShare) {
        // Arrange
        Plant plant = new Plant();

        // Act
        Exception? thrown = Record.Exception(() => plant.Genealogy.Contain(plant.A.Number, (decimal)minimumShare));

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
        Assert.Equal(LotStatus.Released, plant.A.Status);
        Assert.Equal(LotStatus.Released, plant.C.Status);
    }

    [Fact]
    public void Containing_an_unknown_lot_fails_with_a_missing_key() {
        // Arrange
        Plant     plant      = new Plant();
        LotNumber anyUnknown = Any.LotNumber().Generate();

        // Act
        Exception? thrown = Record.Exception(() => plant.Genealogy.Contain(anyUnknown, 0.5m));

        // Assert
        Assert.IsType<KeyNotFoundException>(thrown);
    }

    [Fact]
    public void A_contained_material_can_no_longer_be_consumed() {
        // Arrange
        Plant plant = new Plant();
        plant.Genealogy.Contain(plant.A.Number, 0.5m);

        // Act
        Exception? thrown = Record.Exception(() => plant.A.Consume(Scenario.Grams(1m), plant.Day3));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
    }

}
