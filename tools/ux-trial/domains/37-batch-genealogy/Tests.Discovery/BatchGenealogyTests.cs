using BatchGenealogy.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

using static BatchGenealogy.Tests.Discovery.Plant;

using Genealogy = BatchGenealogy.Domain.BatchGenealogy;

namespace BatchGenealogy.Tests.Discovery;

[Reproducible]
public sealed class BatchGenealogyTests {

    [Fact]
    public void A_site_code_is_four_upper_case_letters() {
        // Arrange
        const string notASiteCode = "Lyo1";

        // Act
        Exception? refusal = Record.Exception(() => new Genealogy(notASiteCode));

        // Assert
        Assert.IsType<FormatException>(refusal);
    }

    [Fact]
    public void A_lot_number_is_registered_once() {
        // Arrange
        Genealogy anySite   = AnySite();
        Lot       anyLot    = Any.Lot().Generate();
        Lot       anySameNo = Any.Lot().WithNumber(anyLot.Number).Generate();
        anySite.Register(anyLot);

        // Act
        Exception? refusal = Record.Exception(() => anySite.Register(anySameNo));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Single(anySite.Lots);
    }

    [Fact]
    public void An_unknown_lot_cannot_be_looked_up() {
        // Arrange
        Genealogy anySite   = AnySite();
        LotNumber anyNumber = Any.LotNumber().Generate();

        // Act
        Exception? refusal = Record.Exception(() => anySite.Get(anyNumber));

        // Assert
        Assert.IsType<KeyNotFoundException>(refusal);
    }

    [Fact]
    public void Recording_a_blend_consumes_its_inputs_and_links_them_to_the_output() {
        // Arrange
        Genealogy       anySite = AnySite();
        DateOnly        anyDay  = AnyProductionDay();
        List<LotNumber> anyLots = AnyDistinctLotNumbers(3);
        Lot             first   = RegisterReleased(anySite, anyLots[0], Kg(100m), anyDay);
        Lot             second  = RegisterReleased(anySite, anyLots[1], Kg(50m), anyDay);
        Register(anySite, anyLots[2], Kg(90m), anyDay.AddDays(1));
        Transformation blend = new Transformation(AnyTransformationId().Generate(), TransformationKind.Blend,
                                                  [new LotQuantity(first.Number, Kg(60m)), new LotQuantity(second.Number, Kg(40m))],
                                                  [new LotQuantity(anyLots[2], Kg(90m))],
                                                  anyDay.AddDays(1));

        // Act
        anySite.Record(blend);

        // Assert
        Assert.Equal(Kg(40m), first.RemainingQuantity);
        Assert.Equal(Kg(10m), second.RemainingQuantity);
        Assert.Equal([0.6m, 0.4m], anySite.Links.Select(link => link.Share));
        Assert.All(anySite.Links, link => Assert.Equal(anyLots[2], link.Target));
    }

    [Fact]
    public void An_output_registered_with_another_quantity_is_refused_and_nothing_is_consumed() {
        // Arrange
        Genealogy       anySite = AnySite();
        DateOnly        anyDay  = AnyProductionDay();
        List<LotNumber> anyLots = AnyDistinctLotNumbers(2);
        Lot             input   = RegisterReleased(anySite, anyLots[0], Kg(100m), anyDay);
        Register(anySite, anyLots[1], Kg(80m), anyDay);
        Transformation process = new Transformation(AnyTransformationId().Generate(), TransformationKind.Process,
                                                    [new LotQuantity(input.Number, Kg(100m))],
                                                    [new LotQuantity(anyLots[1], Kg(95m))],
                                                    anyDay);

        // Act
        Exception? refusal = Record.Exception(() => anySite.Record(process));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Equal(Kg(100m), input.RemainingQuantity);
        Assert.Empty(anySite.Links);
    }

    [Fact]
    public void A_split_keeps_the_material_of_the_lot_it_divides() {
        // Arrange
        Genealogy    anySite          = AnySite();
        DateOnly     anyDay           = AnyProductionDay();
        MaterialCode anyMaterial      = Any.MaterialCode().Generate();
        MaterialCode anyOtherMaterial = Any.MaterialCode().WithValue(code => code.DifferentFrom(anyMaterial.Value)).Generate();
        LotNumber    anySource        = Any.LotNumber().Generate();
        RegisterReleased(anySite, anySource, Kg(100m), anyDay, anyMaterial);
        Register(anySite, anySource.SubLot(1), Kg(50m), anyDay, anyMaterial);
        Register(anySite, anySource.SubLot(2), Kg(50m), anyDay, anyOtherMaterial);
        Transformation split = new Transformation(AnyTransformationId().Generate(), TransformationKind.Split,
                                                  [new LotQuantity(anySource, Kg(100m))],
                                                  [new LotQuantity(anySource.SubLot(1), Kg(50m)), new LotQuantity(anySource.SubLot(2), Kg(50m))],
                                                  anyDay);

        // Act
        Exception? refusal = Record.Exception(() => anySite.Record(split));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_consumed_lot_cannot_be_produced_afterwards() {
        // Arrange
        Genealogy       anySite = AnySite();
        DateOnly        anyDay  = AnyProductionDay();
        List<LotNumber> anyIds  = AnyDistinctLotNumbers(3);
        List<string>    anyTrf  = Any.ListOf(AnyTransformationId()).WithCount(2).Distinct().Generate();
        Lot             raw     = RegisterReleased(anySite, anyIds[0], Kg(100m), anyDay);
        Lot             other   = RegisterReleased(anySite, anyIds[1], Kg(100m), anyDay);
        Register(anySite, anyIds[2], Kg(50m), anyDay);
        anySite.Record(new Transformation(anyTrf[0], TransformationKind.Process,
                                          [new LotQuantity(raw.Number, Kg(50m))], [new LotQuantity(anyIds[2], Kg(50m))], anyDay));
        Transformation backwards = new Transformation(anyTrf[1], TransformationKind.Process,
                                                      [new LotQuantity(other.Number, Kg(60m))], [new LotQuantity(raw.Number, Kg(50m))], anyDay);

        // Act
        Exception? refusal = Record.Exception(() => anySite.Record(backwards));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void Tracing_a_sub_lot_upstream_finds_every_material_and_how_much_of_each_it_contains() {
        // Arrange
        Genealogy       anySite  = AnySite();
        DateOnly        anyDay   = AnyProductionDay();
        List<LotNumber> anyLots  = AnyDistinctLotNumbers(3);
        List<string>    anyTrfs  = Any.ListOf(AnyTransformationId()).WithCount(2).Distinct().Generate();
        LotNumber       api      = anyLots[0];
        LotNumber       excipient = anyLots[1];
        LotNumber       blend    = anyLots[2];
        RegisterReleased(anySite, api, Kg(60m), anyDay);
        RegisterReleased(anySite, excipient, Kg(40m), anyDay);
        Lot blendLot = RegisterReleased(anySite, blend, Kg(100m), anyDay);
        anySite.Record(new Transformation(anyTrfs[0], TransformationKind.Blend,
                                          [new LotQuantity(api, Kg(60m)), new LotQuantity(excipient, Kg(40m))],
                                          [new LotQuantity(blend, Kg(100m))], anyDay));
        Register(anySite, blend.SubLot(1), Kg(50m), anyDay.AddDays(1), blendLot.Material);
        Register(anySite, blend.SubLot(2), Kg(50m), anyDay.AddDays(1), blendLot.Material);
        anySite.Record(new Transformation(anyTrfs[1], TransformationKind.Split,
                                          [new LotQuantity(blend, Kg(100m))],
                                          [new LotQuantity(blend.SubLot(1), Kg(50m)), new LotQuantity(blend.SubLot(2), Kg(50m))],
                                          anyDay.AddDays(1)));

        // Act
        IReadOnlyList<TraceEntry> upstream = anySite.TraceUpstream(blend.SubLot(1));

        // Assert
        Assert.Equal(3, upstream.Count);
        TraceEntry viaBlend     = Assert.Single(upstream, entry => entry.Lot.Equals(blend));
        TraceEntry viaApi       = Assert.Single(upstream, entry => entry.Lot.Equals(api));
        TraceEntry viaExcipient = Assert.Single(upstream, entry => entry.Lot.Equals(excipient));
        Assert.Equal((1, 1m, Kg(50m)), (viaBlend.Depth, viaBlend.Share, viaBlend.ExposedQuantity));
        Assert.Equal((2, 0.6m, Kg(30m)), (viaApi.Depth, viaApi.Share, viaApi.ExposedQuantity));
        Assert.Equal((2, 0.4m, Kg(20m)), (viaExcipient.Depth, viaExcipient.Share, viaExcipient.ExposedQuantity));
    }

    [Fact]
    public void A_material_reaching_a_product_through_two_sub_lots_counts_both_paths() {
        // Arrange
        Genealogy       anySite = AnySite();
        DateOnly        anyDay  = AnyProductionDay();
        List<LotNumber> anyLots = AnyDistinctLotNumbers(3);
        List<string>    anyTrfs = Any.ListOf(AnyTransformationId()).WithCount(2).Distinct().Generate();
        LotNumber       api     = anyLots[0];
        LotNumber       filler  = anyLots[1];
        LotNumber       tablets = anyLots[2];
        Lot             apiLot  = RegisterReleased(anySite, api, Kg(100m), anyDay);
        RegisterReleased(anySite, filler, Kg(100m), anyDay);
        Register(anySite, api.SubLot(1), Kg(50m), anyDay, apiLot.Material).Release();
        Register(anySite, api.SubLot(2), Kg(50m), anyDay, apiLot.Material).Release();
        anySite.Record(new Transformation(anyTrfs[0], TransformationKind.Split,
                                          [new LotQuantity(api, Kg(100m))],
                                          [new LotQuantity(api.SubLot(1), Kg(50m)), new LotQuantity(api.SubLot(2), Kg(50m))], anyDay));
        Register(anySite, tablets, Kg(190m), anyDay.AddDays(1));
        anySite.Record(new Transformation(anyTrfs[1], TransformationKind.Blend,
                                          [new LotQuantity(api.SubLot(1), Kg(50m)), new LotQuantity(api.SubLot(2), Kg(50m)), new LotQuantity(filler, Kg(100m))],
                                          [new LotQuantity(tablets, Kg(190m))], anyDay.AddDays(1)));

        // Act
        IReadOnlyList<TraceEntry> downstream = anySite.TraceDownstream(api);

        // Assert
        TraceEntry reached = Assert.Single(downstream, entry => entry.Lot.Equals(tablets));
        Assert.Equal(2, reached.Depth);
        Assert.Equal(0.5m, reached.Share);
        Assert.Equal(Kg(95m), reached.ExposedQuantity);
    }

    [Fact]
    public void Containment_rejects_the_material_and_quarantines_the_lots_carrying_enough_of_it() {
        // Arrange
        Genealogy       anySite = AnySite();
        DateOnly        anyDay  = AnyProductionDay();
        List<LotNumber> anyLots = AnyDistinctLotNumbers(5);
        List<string>    anyTrfs = Any.ListOf(AnyTransformationId()).WithCount(2).Distinct().Generate();
        LotNumber       suspect = anyLots[0];
        LotNumber       fillerA = anyLots[1];
        LotNumber       fillerB = anyLots[2];
        LotNumber       rich    = anyLots[3];
        LotNumber       trace   = anyLots[4];
        RegisterReleased(anySite, suspect, Kg(100m), anyDay);
        RegisterReleased(anySite, fillerA, Kg(100m), anyDay);
        RegisterReleased(anySite, fillerB, Kg(1000m), anyDay);
        Lot richLot  = RegisterReleased(anySite, rich, Kg(100m), anyDay);
        Lot traceLot = RegisterReleased(anySite, trace, Kg(1000m), anyDay);
        anySite.Record(new Transformation(anyTrfs[0], TransformationKind.Blend,
                                          [new LotQuantity(suspect, Kg(50m)), new LotQuantity(fillerA, Kg(50m))],
                                          [new LotQuantity(rich, Kg(100m))], anyDay));
        anySite.Record(new Transformation(anyTrfs[1], TransformationKind.Blend,
                                          [new LotQuantity(suspect, Kg(10m)), new LotQuantity(fillerB, Kg(990m))],
                                          [new LotQuantity(trace, Kg(1000m))], anyDay));

        // Act
        IReadOnlyList<LotNumber> quarantined = anySite.Contain(suspect, minimumShare: 0.05m);

        // Assert
        Assert.Equal([rich], quarantined);
        Assert.Equal(LotStatus.Rejected, anySite.Get(suspect).Status);
        Assert.Equal(LotStatus.Quarantined, richLot.Status);
        Assert.Equal(LotStatus.Released, traceLot.Status);
    }

    [Fact]
    public void Containment_leaves_an_already_rejected_descendant_rejected() {
        // Arrange
        Genealogy       anySite = AnySite();
        DateOnly        anyDay  = AnyProductionDay();
        List<LotNumber> anyLots = AnyDistinctLotNumbers(2);
        LotNumber       suspect = anyLots[0];
        LotNumber       product = anyLots[1];
        RegisterReleased(anySite, suspect, Kg(100m), anyDay);
        Lot productLot = RegisterReleased(anySite, product, Kg(80m), anyDay);
        anySite.Record(new Transformation(AnyTransformationId().Generate(), TransformationKind.Process,
                                          [new LotQuantity(suspect, Kg(100m))], [new LotQuantity(product, Kg(80m))], anyDay));
        productLot.Reject();

        // Act
        IReadOnlyList<LotNumber> quarantined = anySite.Contain(suspect, minimumShare: 1m);

        // Assert
        Assert.Empty(quarantined);
        Assert.Equal(LotStatus.Rejected, productLot.Status);
    }

}
