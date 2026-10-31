namespace BatchGenealogy.Tests.Expert.Tests;

public sealed class RecordingTests {

    [Fact]
    public void A_lot_number_is_registered_only_once() {
        // Arrange
        Domain.BatchGenealogy genealogy = new(Any.SiteCode().Generate());
        Lot                   anyLot    = Any.Lot().Generate();
        genealogy.Register(anyLot);
        Lot anyOtherLotUnderTheSameNumber = Any.Lot().WithNumber(anyLot.Number).Generate();

        // Act
        Exception? error = Record.Exception(() => genealogy.Register(anyOtherLotUnderTheSameNumber));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void Recording_a_blend_draws_each_input_quantity_from_its_lot() {
        // Arrange
        Plant     plant     = new();
        LotNumber anyApi    = plant.ReleasedRawMaterial(enoughFor: Kg(30m));
        LotNumber anyFiller = plant.ReleasedRawMaterial(enoughFor: Kg(70m));
        Quantity  apiBefore    = plant[anyApi].RemainingQuantity;
        Quantity  fillerBefore = plant[anyFiller].RemainingQuantity;

        // Act
        plant.Blend(Kg(100m), Kg(30m).Of(anyApi), Kg(70m).Of(anyFiller));

        // Assert
        Assert.Equal(apiBefore.Subtract(Kg(30m)), plant[anyApi].RemainingQuantity);
        Assert.Equal(fillerBefore.Subtract(Kg(70m)), plant[anyFiller].RemainingQuantity);
    }

    [Fact]
    public void A_blend_links_its_output_to_each_input_with_that_input_s_share() {
        // Arrange
        Plant     plant     = new();
        LotNumber anyApi    = plant.ReleasedRawMaterial(enoughFor: Kg(30m));
        LotNumber anyFiller = plant.ReleasedRawMaterial(enoughFor: Kg(70m));

        // Act
        LotNumber blend = plant.Blend(Kg(98m), Kg(30m).Of(anyApi), Kg(70m).Of(anyFiller));

        // Assert
        Assert.Collection(plant.Genealogy.Links.OrderBy(link => link.Share),
                          link => { Assert.Equal((anyApi, blend, 0.3m), (link.Source, link.Target, link.Share)); },
                          link => { Assert.Equal((anyFiller, blend, 0.7m), (link.Source, link.Target, link.Share)); });
    }

    [Fact]
    public void A_refused_record_leaves_every_input_untouched() {
        // Arrange
        Plant     plant       = new();
        LotNumber anyApi      = plant.ReleasedRawMaterial(enoughFor: Kg(30m));
        LotNumber anyFiller   = plant.ReleasedRawMaterial(enoughFor: Kg(70m));
        Quantity  apiBefore   = plant[anyApi].RemainingQuantity;
        DateOnly  anyDay      = plant.NextStepDate();
        Lot       anyRegistered95Kg = plant.RegisteredOutput(Kg(95m), anyDay);
        Transformation producing100Kg = new(plant.NextTransformationId(), TransformationKind.Blend,
                                            [new LotQuantity(anyApi, Kg(30m)), new LotQuantity(anyFiller, Kg(70m))],
                                            [new LotQuantity(anyRegistered95Kg.Number, Kg(100m))],
                                            anyDay);

        // Act
        Exception? error = Record.Exception(() => plant.Genealogy.Record(producing100Kg));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(apiBefore, plant[anyApi].RemainingQuantity);
        Assert.Empty(plant.Genealogy.Transformations);
        Assert.Empty(plant.Genealogy.Links);
    }

    [Fact]
    public void An_output_lot_is_manufactured_on_the_day_of_the_step_that_produced_it() {
        // Arrange
        Plant     plant  = new();
        LotNumber anyApi = plant.ReleasedRawMaterial(enoughFor: Kg(30m));
        DateOnly  anyDay = plant.NextStepDate();
        Lot       anyOutputMadeTheDayAfter = plant.RegisteredOutput(Kg(30m), anyDay.AddDays(1));
        Transformation process = new(plant.NextTransformationId(), TransformationKind.Process,
                                     [new LotQuantity(anyApi, Kg(30m))],
                                     [new LotQuantity(anyOutputMadeTheDayAfter.Number, Kg(30m))],
                                     anyDay);

        // Act
        Exception? error = Record.Exception(() => plant.Genealogy.Record(process));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

    [Fact]
    public void A_split_cannot_turn_a_lot_into_another_material() {
        // Arrange
        Plant        plant      = new();
        LotNumber    anyApi     = plant.ReleasedRawMaterial(enoughFor: Kg(100m));
        MaterialCode apiMaterial = plant[anyApi].Material;
        DateOnly     anyDay      = plant.NextStepDate();
        Lot anySameMaterial  = Any.Lot().WithNumber(anyApi.SubLot(1)).WithMaterial(apiMaterial).WithInitialQuantity(Kg(50m)).WithManufacturedOn(anyDay).Generate();
        Lot anyOtherMaterial = Any.Lot().WithNumber(anyApi.SubLot(2))
                                  .WithMaterial(material => material.WithValue(code => code.DifferentFrom(apiMaterial.Value)))
                                  .WithInitialQuantity(Kg(50m)).WithManufacturedOn(anyDay).Generate();
        plant.Genealogy.Register(anySameMaterial);
        plant.Genealogy.Register(anyOtherMaterial);
        Transformation split = new(plant.NextTransformationId(), TransformationKind.Split,
                                   [new LotQuantity(anyApi, Kg(100m))],
                                   [new LotQuantity(anySameMaterial.Number, Kg(50m)), new LotQuantity(anyOtherMaterial.Number, Kg(50m))],
                                   anyDay);

        // Act
        Exception? error = Record.Exception(() => plant.Genealogy.Record(split));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
    }

}
