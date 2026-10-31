using BatchGenealogy.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

using static BatchGenealogy.Tests.Discovery.Plant;

namespace BatchGenealogy.Tests.Discovery;

[Reproducible]
public sealed class TransformationTests {

    [Fact]
    public void A_blend_loses_what_it_does_not_yield() {
        // Arrange
        List<LotNumber> anyLots = AnyDistinctLotNumbers(3);
        string          anyId   = AnyTransformationId().Generate();
        DateOnly        anyDay  = AnyProductionDay();

        // Act
        Transformation blend = new Transformation(anyId, TransformationKind.Blend,
                                                  [new LotQuantity(anyLots[0], Kg(60m)), new LotQuantity(anyLots[1], Kg(40m))],
                                                  [new LotQuantity(anyLots[2], Kg(95m))],
                                                  anyDay);

        // Assert
        Assert.Equal(Kg(5m), blend.Loss);
        Assert.Equal(0.95m, blend.Yield);
    }

    [Fact]
    public void Each_input_makes_up_its_share_of_the_total_input() {
        // Arrange
        List<LotNumber> anyLots = AnyDistinctLotNumbers(3);
        Transformation blend = new Transformation(AnyTransformationId().Generate(), TransformationKind.Blend,
                                                  [new LotQuantity(anyLots[0], Kg(60m)), new LotQuantity(anyLots[1], new Quantity(40_000m, QuantityUnit.Gram))],
                                                  [new LotQuantity(anyLots[2], Kg(100m))],
                                                  AnyProductionDay());

        // Act
        decimal share = blend.ShareOf(anyLots[0]);

        // Assert
        Assert.Equal(0.6m, share);
    }

    [Fact]
    public void A_transformation_cannot_produce_more_than_it_consumes() {
        // Arrange
        List<LotNumber> anyLots = AnyDistinctLotNumbers(2);

        // Act
        Exception? refusal = Record.Exception(() => new Transformation(AnyTransformationId().Generate(), TransformationKind.Process,
                                                                       [new LotQuantity(anyLots[0], Kg(10m))],
                                                                       [new LotQuantity(anyLots[1], Kg(10.1m))],
                                                                       AnyProductionDay()));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_blend_needs_at_least_two_inputs() {
        // Arrange
        List<LotNumber> anyLots = AnyDistinctLotNumbers(2);

        // Act
        Exception? refusal = Record.Exception(() => new Transformation(AnyTransformationId().Generate(), TransformationKind.Blend,
                                                                       [new LotQuantity(anyLots[0], Kg(10m))],
                                                                       [new LotQuantity(anyLots[1], Kg(10m))],
                                                                       AnyProductionDay()));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_split_only_produces_sub_lots_of_its_input() {
        // Arrange
        List<LotNumber> anyLots   = AnyDistinctLotNumbers(2);
        LotNumber       anySource = anyLots[0];
        LotNumber       anyOther  = anyLots[1];

        // Act
        Exception? refusal = Record.Exception(() => new Transformation(AnyTransformationId().Generate(), TransformationKind.Split,
                                                                       [new LotQuantity(anySource, Kg(10m))],
                                                                       [new LotQuantity(anySource.SubLot(1), Kg(5m)), new LotQuantity(anyOther.SubLot(2), Kg(5m))],
                                                                       AnyProductionDay()));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_transformation_stays_in_one_dimension() {
        // Arrange
        List<LotNumber> anyLots = AnyDistinctLotNumbers(2);

        // Act
        Exception? refusal = Record.Exception(() => new Transformation(AnyTransformationId().Generate(), TransformationKind.Process,
                                                                       [new LotQuantity(anyLots[0], Kg(10m))],
                                                                       [new LotQuantity(anyLots[1], new Quantity(1m, QuantityUnit.Litre))],
                                                                       AnyProductionDay()));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

}
