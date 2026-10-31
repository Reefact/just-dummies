using BatchGenealogy.Domain;

using JustDummies;

using Xunit;

using static GenealogyTests.Site;

namespace GenealogyTests;

public sealed class TransformationTests {

    [Fact]
    public void A_blend_needs_at_least_two_inputs() {
        // Arrange
        LotQuantity anyInput  = Arbitrary.LotQuantity().WithQuantity(Kg(10m)).Generate();
        LotQuantity anyOutput = Arbitrary.LotQuantity().WithQuantity(Kg(9m)).Generate();

        // Act
        Exception? error = Record.Exception(() => NewTransformation(TransformationKind.Blend, [anyInput], [anyOutput]));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_split_produces_sub_lots_of_its_input_only() {
        // Arrange
        LotNumber anyInputLot   = Arbitrary.LotNumber().Generate();
        LotNumber anyStrangeLot = Arbitrary.LotNumber().Generate().SubLot(2);

        // Act
        Exception? error = Record.Exception(() => NewTransformation(
            TransformationKind.Split,
            [Part(anyInputLot, Kg(10m))],
            [Part(anyInputLot.SubLot(1), Kg(5m)), Part(anyStrangeLot, Kg(5m))]));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_transformation_cannot_produce_more_than_it_consumes() {
        // Arrange
        LotQuantity anyInput  = Arbitrary.LotQuantity().WithQuantity(Kg(10m)).Generate();
        LotQuantity anyOutput = Arbitrary.LotQuantity().WithQuantity(new Quantity(10_001m, QuantityUnit.Gram)).Generate();

        // Act
        Exception? error = Record.Exception(() => NewTransformation(TransformationKind.Process, [anyInput], [anyOutput]));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_transformation_does_not_mix_mass_and_volume() {
        // Arrange
        LotQuantity anyMassInput   = Arbitrary.LotQuantity().WithQuantity(q => q.WithUnit(QuantityUnit.Kilogram)).Generate();
        LotQuantity anyVolumeInput = Arbitrary.LotQuantity().WithQuantity(q => q.WithUnit(QuantityUnit.Litre)).Generate();
        LotQuantity anyOutput      = Arbitrary.LotQuantity().WithQuantity(new Quantity(1m, QuantityUnit.Milligram)).Generate();

        // Act
        Exception? error = Record.Exception(() => NewTransformation(TransformationKind.Blend, [anyMassInput, anyVolumeInput], [anyOutput]));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void The_loss_and_the_yield_compare_what_came_out_with_what_went_in() {
        // Arrange
        LotQuantity anyApi       = Arbitrary.LotQuantity().WithQuantity(Kg(60m)).Generate();
        LotQuantity anyExcipient = Arbitrary.LotQuantity().WithQuantity(new Quantity(40_000m, QuantityUnit.Gram)).Generate();
        LotQuantity anyBlend     = Arbitrary.LotQuantity().WithQuantity(Kg(95m)).Generate();
        Transformation blend = NewTransformation(TransformationKind.Blend, [anyApi, anyExcipient], [anyBlend]);

        // Act
        Quantity loss = blend.Loss;

        // Assert
        Assert.Equal(Kg(5m), loss);
        Assert.Equal(0.95m, blend.Yield);
    }

    [Fact]
    public void An_input_s_share_is_its_part_of_the_total_input() {
        // Arrange
        LotNumber   anyApi       = Arbitrary.LotNumber().Generate();
        LotQuantity anyExcipient = Arbitrary.LotQuantity().WithQuantity(Kg(75m)).Generate();
        LotQuantity anyBlend     = Arbitrary.LotQuantity().WithQuantity(Kg(100m)).Generate();
        Transformation blend = NewTransformation(TransformationKind.Blend, [Part(anyApi, Kg(25m)), anyExcipient], [anyBlend]);

        // Act
        decimal share = blend.ShareOf(anyApi);

        // Assert
        Assert.Equal(0.25m, share);
    }

    private static Transformation NewTransformation(TransformationKind kind, LotQuantity[] inputs, LotQuantity[] outputs) {
        return new Transformation(Arbitrary.TransformationId().Generate(), kind, inputs, outputs, Arbitrary.ProductionDay().Generate());
    }

}
