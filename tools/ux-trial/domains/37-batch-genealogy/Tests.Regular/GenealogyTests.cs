using BatchGenealogy.Domain;

using JustDummies;

using Xunit;

using static GenealogyTests.Site;

namespace GenealogyTests;

public sealed class RegistrationTests {

    [Fact]
    public void A_site_code_is_four_upper_case_letters() {
        // Arrange
        const string threeLetters = "LYO";

        // Act
        Exception? error = Record.Exception(() => new BatchGenealogy.Domain.BatchGenealogy(threeLetters));

        // Assert
        Assert.IsType<FormatException>(error);
    }

    [Fact]
    public void A_lot_number_is_registered_once() {
        // Arrange
        Site      site       = new();
        LotNumber anyNumber  = Arbitrary.LotNumber().Generate();
        site.Genealogy.Register(Arbitrary.Lot().WithNumber(anyNumber).Generate());

        // Act
        Exception? error = Record.Exception(() => site.Genealogy.Register(Arbitrary.Lot().WithNumber(anyNumber).Generate()));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void An_unknown_lot_cannot_be_looked_up() {
        // Arrange
        Site      site      = new();
        LotNumber anyNumber = Arbitrary.LotNumber().Generate();

        // Act
        Exception? error = Record.Exception(() => site.Genealogy.Get(anyNumber));

        // Assert
        Assert.IsType<KeyNotFoundException>(error);
    }

}

public sealed class RecordingTests {

    [Fact]
    public void Recording_a_blend_consumes_its_inputs_and_links_each_to_the_output_with_its_share() {
        // Arrange
        Site      site         = new();
        LotNumber anyApi       = site.ReleasedLot(Kg(50m));
        LotNumber anyExcipient = site.ReleasedLot(Kg(100m));
        LotNumber anyBlend     = site.OutputLot(Kg(78m));

        // Act
        site.Record(TransformationKind.Blend, [Part(anyApi, Kg(20m)), Part(anyExcipient, Kg(60m))], [Part(anyBlend, Kg(78m))]);

        // Assert
        Assert.Equal(Kg(30m), site.Genealogy.Get(anyApi).RemainingQuantity);
        Assert.Equal(Kg(40m), site.Genealogy.Get(anyExcipient).RemainingQuantity);
        Assert.Equal([0.25m, 0.75m], site.Genealogy.Links.Select(link => link.Share).Order());
    }

    [Fact]
    public void An_output_registered_with_another_quantity_is_refused_and_nothing_is_consumed() {
        // Arrange
        Site      site      = new();
        LotNumber anyInput  = site.ReleasedLot(Kg(50m));
        LotNumber anyOutput = site.OutputLot(Kg(40m));
        Transformation process = new(Arbitrary.TransformationId().Generate(), TransformationKind.Process,
                                     [Part(anyInput, Kg(50m))], [Part(anyOutput, Kg(45m))], site.Day);

        // Act
        Exception? error = Record.Exception(() => site.Genealogy.Record(process));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(Kg(50m), site.Genealogy.Get(anyInput).RemainingQuantity);
        Assert.Empty(site.Genealogy.Links);
    }

    [Fact]
    public void An_output_manufactured_on_another_day_is_refused() {
        // Arrange
        Site      site      = new();
        LotNumber anyInput  = site.ReleasedLot(Kg(50m));
        Lot       anyOutput = Arbitrary.Lot().WithInitialQuantity(Kg(40m)).WithManufacturedOn(site.Day.AddDays(1)).Generate();
        site.Genealogy.Register(anyOutput);
        Transformation process = new(Arbitrary.TransformationId().Generate(), TransformationKind.Process,
                                     [Part(anyInput, Kg(50m))], [Part(anyOutput.Number, Kg(40m))], site.Day);

        // Act
        Exception? error = Record.Exception(() => site.Genealogy.Record(process));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_quarantined_input_cannot_enter_a_transformation() {
        // Arrange
        Site      site      = new();
        Lot       anyInput  = Arbitrary.Lot().WithInitialQuantity(Kg(50m)).Generate();
        LotNumber anyOutput = site.OutputLot(Kg(40m));
        site.Genealogy.Register(anyInput);
        Transformation process = new(Arbitrary.TransformationId().Generate(), TransformationKind.Process,
                                     [Part(anyInput.Number, Kg(50m))], [Part(anyOutput, Kg(40m))], site.Day);

        // Act
        Exception? error = Record.Exception(() => site.Genealogy.Record(process));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_split_keeps_the_material_of_the_lot_it_divides() {
        // Arrange
        Site         site        = new();
        MaterialCode anyMaterial = Arbitrary.MaterialCode().Generate();
        Lot          anyBulk     = Arbitrary.Lot().WithInitialQuantity(Kg(100m)).WithMaterial(anyMaterial).Generate();
        site.Genealogy.Register(anyBulk);
        anyBulk.Release();
        LotNumber sameMaterial  = site.OutputLot(Kg(50m), anyBulk.Number.SubLot(1), anyMaterial);
        LotNumber otherMaterial = site.OutputLot(Kg(50m), anyBulk.Number.SubLot(2), Arbitrary.MaterialCode().WithValue(v => v.Except(anyMaterial.Value)).Generate());
        Transformation split = new(Arbitrary.TransformationId().Generate(), TransformationKind.Split,
                                   [Part(anyBulk.Number, Kg(100m))], [Part(sameMaterial, Kg(50m)), Part(otherMaterial, Kg(50m))], site.Day);

        // Act
        Exception? error = Record.Exception(() => site.Genealogy.Record(split));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

}

public sealed class TracingTests {

    // raw --split--> raw.01 --blend(with other)--> granulate --blend(with raw.02)--> final
    //           \--> raw.02 ------------------------------------------------/
    private sealed class Diamond {

        public Diamond() {
            MaterialCode anyMaterial = Arbitrary.MaterialCode().Generate();
            Lot          anyRaw      = Arbitrary.Lot().WithInitialQuantity(Kg(100m)).WithMaterial(anyMaterial).Generate();
            Site.Genealogy.Register(anyRaw);
            anyRaw.Release();
            Raw   = anyRaw.Number;
            Other = Site.ReleasedLot(Kg(60m));

            LotNumber first  = Site.OutputLot(Kg(40m), Raw.SubLot(1), anyMaterial);
            LotNumber second = Site.OutputLot(Kg(60m), Raw.SubLot(2), anyMaterial);
            Site.Record(TransformationKind.Split, [Part(Raw, Kg(100m))], [Part(first, Kg(40m)), Part(second, Kg(60m))]);

            Granulate = Site.OutputLot(Kg(80m));
            Site.Record(TransformationKind.Blend, [Part(first, Kg(40m)), Part(Other, Kg(40m))], [Part(Granulate, Kg(80m))]);

            Final = Site.OutputLot(Kg(100m));
            Site.Record(TransformationKind.Blend, [Part(Granulate, Kg(80m)), Part(second, Kg(20m))], [Part(Final, Kg(100m))]);
        }

        public Site      Site { get; } = new();
        public LotNumber Raw { get; }
        public LotNumber Other { get; }
        public LotNumber Granulate { get; }
        public LotNumber Final { get; }

    }

    [Fact]
    public void A_material_reaching_a_product_by_two_paths_counts_both() {
        // Arrange
        Diamond diamond = new();

        // Act
        decimal share = diamond.Site.Genealogy.ShareOf(diamond.Raw, diamond.Final);

        // Assert
        // 80% granulate x 50% raw + 20% raw directly.
        Assert.Equal(0.6m, share);
    }

    [Fact]
    public void Tracing_upstream_lists_every_ancestor_nearest_first_with_the_quantity_the_product_holds() {
        // Arrange
        Diamond diamond = new();

        // Act
        IReadOnlyList<TraceEntry> upstream = diamond.Site.Genealogy.TraceUpstream(diamond.Final);

        // Assert
        Assert.Equal([1, 1, 2, 2, 2], upstream.Select(entry => entry.Depth));
        TraceEntry raw = Assert.Single(upstream, entry => entry.Lot.Equals(diamond.Raw));
        Assert.Equal(Kg(60m), raw.ExposedQuantity);
    }

    [Fact]
    public void Tracing_downstream_reports_what_each_descendant_carries_of_the_material() {
        // Arrange
        Diamond diamond = new();

        // Act
        IReadOnlyList<TraceEntry> downstream = diamond.Site.Genealogy.TraceDownstream(diamond.Other);

        // Assert
        Assert.Equal([diamond.Granulate, diamond.Final], downstream.Select(entry => entry.Lot));
        Assert.Equal(Kg(40m), downstream[0].ExposedQuantity);
        Assert.Equal(Kg(40m), downstream[1].ExposedQuantity);
    }

    [Fact]
    public void A_raw_material_has_no_upstream() {
        // Arrange
        Diamond diamond = new();

        // Act
        IReadOnlyList<TraceEntry> upstream = diamond.Site.Genealogy.TraceUpstream(diamond.Other);

        // Assert
        Assert.Empty(upstream);
    }

    [Fact]
    public void Containment_rejects_the_material_and_quarantines_the_lots_carrying_enough_of_it() {
        // Arrange
        Diamond diamond = new();

        // Act
        IReadOnlyList<LotNumber> quarantined = diamond.Site.Genealogy.Contain(diamond.Other, 0.45m);

        // Assert
        // The granulate is half `other`; the final product only 40%.
        Assert.Equal([diamond.Granulate], quarantined);
        Assert.Equal(LotStatus.Rejected, diamond.Site.Genealogy.Get(diamond.Other).Status);
        Assert.Equal(LotStatus.Released, diamond.Site.Genealogy.Get(diamond.Final).Status);
    }

    [Fact]
    public void Containment_leaves_an_already_rejected_descendant_rejected() {
        // Arrange
        Diamond diamond = new();
        diamond.Site.Genealogy.Get(diamond.Granulate).Reject();

        // Act
        IReadOnlyList<LotNumber> quarantined = diamond.Site.Genealogy.Contain(diamond.Raw, 0.01m);

        // Assert
        Assert.DoesNotContain(diamond.Granulate, quarantined);
        Assert.Contains(diamond.Final, quarantined);
        Assert.Equal(LotStatus.Rejected, diamond.Site.Genealogy.Get(diamond.Granulate).Status);
    }

}
