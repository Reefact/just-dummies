namespace BatchGenealogy.Tests.Expert.Tests;

/// <summary>
///     The quantities and shares are what these tests are about, so they are literals. Lot numbers,
///     materials, dates and transformation ids are the plant's dummies.
/// </summary>
public sealed class TracingTests {

    [Fact]
    public void A_product_carries_its_raw_material_in_the_proportion_of_every_step_in_between() {
        // Arrange
        Plant     plant      = new();
        LotNumber anyApi     = plant.ReleasedRawMaterial(enoughFor: Kg(25m));
        LotNumber anyFiller  = plant.ReleasedRawMaterial(enoughFor: Kg(75m));
        LotNumber blend      = plant.Blend(Kg(100m), Kg(25m).Of(anyApi), Kg(75m).Of(anyFiller));
        LotNumber anyCoating = plant.ReleasedRawMaterial(enoughFor: Kg(20m));
        LotNumber tablets    = plant.Process(Kg(96m), Kg(80m).Of(blend), Kg(20m).Of(anyCoating));

        // Act
        decimal share = plant.Genealogy.ShareOf(anyApi, tablets);

        // Assert
        Assert.Equal(0.2m, share);
    }

    [Fact]
    public void A_material_reaching_a_product_by_two_paths_counts_on_both() {
        // Arrange: the API is split, and each half goes its own way before the two meet again.
        Plant                    plant     = new();
        LotNumber                anyApi    = plant.ReleasedRawMaterial(enoughFor: Kg(100m));
        IReadOnlyList<LotNumber> halves    = plant.Split(anyApi, Kg(60m), Kg(40m));
        LotNumber                anyFiller = plant.ReleasedRawMaterial(enoughFor: Kg(100m));
        LotNumber                granulate = plant.Blend(Kg(120m), Kg(60m).Of(halves[0]), Kg(60m).Of(anyFiller));
        LotNumber                premix    = plant.Blend(Kg(80m), Kg(40m).Of(halves[1]), Kg(40m).Of(anyFiller));
        LotNumber                final     = plant.Blend(Kg(190m), Kg(120m).Of(granulate), Kg(80m).Of(premix));

        // Act
        decimal share = plant.Genealogy.ShareOf(anyApi, final);

        // Assert
        Assert.Equal(0.5m, share);
    }

    [Fact]
    public void Tracing_a_product_upstream_lists_its_components_nearest_first_with_what_it_contains_of_each() {
        // Arrange
        Plant     plant     = new();
        LotNumber anyApi    = plant.ReleasedRawMaterial(enoughFor: Kg(25m));
        LotNumber anyFiller = plant.ReleasedRawMaterial(enoughFor: Kg(75m));
        LotNumber blend     = plant.Blend(Kg(100m), Kg(25m).Of(anyApi), Kg(75m).Of(anyFiller));
        LotNumber tablets   = plant.Process(Kg(90m), Kg(100m).Of(blend));

        // Act
        IReadOnlyList<TraceEntry> trace = plant.Genealogy.TraceUpstream(tablets);

        // Assert
        Assert.Equal(3, trace.Count);
        Assert.Equal((blend, 1, 1m, Kg(90m)), Row(trace.Single(entry => entry.Lot.Equals(blend))));
        Assert.Equal((anyApi, 2, 0.25m, Kg(22.5m)), Row(trace.Single(entry => entry.Lot.Equals(anyApi))));
        Assert.Equal((anyFiller, 2, 0.75m, Kg(67.5m)), Row(trace.Single(entry => entry.Lot.Equals(anyFiller))));
        Assert.Equal(blend, trace[0].Lot);
    }

    [Fact]
    public void Tracing_a_raw_material_downstream_reaches_every_product_made_from_it_and_nothing_else() {
        // Arrange
        Plant     plant       = new();
        LotNumber anyApi      = plant.ReleasedRawMaterial(enoughFor: Kg(100m));
        IReadOnlyList<LotNumber> parts = plant.Split(anyApi, Kg(50m), Kg(50m));
        LotNumber anyFiller   = plant.ReleasedRawMaterial(enoughFor: Kg(200m));
        LotNumber blendA      = plant.Blend(Kg(100m), Kg(50m).Of(parts[0]), Kg(50m).Of(anyFiller));
        LotNumber blendB      = plant.Blend(Kg(200m), Kg(50m).Of(parts[1]), Kg(150m).Of(anyFiller));
        LotNumber anyOther    = plant.ReleasedRawMaterial(enoughFor: Kg(10m));
        LotNumber unrelated   = plant.Process(Kg(10m), Kg(10m).Of(anyOther));

        // Act
        IReadOnlyList<TraceEntry> trace = plant.Genealogy.TraceDownstream(anyApi);

        // Assert
        Assert.Equal(new HashSet<LotNumber> { parts[0], parts[1], blendA, blendB }, trace.Select(entry => entry.Lot).ToHashSet());
        Assert.DoesNotContain(trace, entry => entry.Lot.Equals(unrelated));
        Assert.Equal(Kg(50m), trace.Single(entry => entry.Lot.Equals(blendA)).ExposedQuantity);
        Assert.Equal(Kg(50m), trace.Single(entry => entry.Lot.Equals(blendB)).ExposedQuantity);
    }

    private static (LotNumber, int, decimal, Quantity) Row(TraceEntry entry) {
        return (entry.Lot, entry.Depth, entry.Share, entry.ExposedQuantity);
    }

}
