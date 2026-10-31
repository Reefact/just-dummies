namespace BatchGenealogy.Tests.Expert.Tests;

public sealed class TransformationTests {

    // Distinct whole-lot numbers for the sides of one step: a lot appears once per step.
    private static LotNumber[] AnyDistinctLots(int count) {
        return Any.ArrayOf(Any.LotNumber().OfAWholeLot()).WithCount(count).Distinct().Generate();
    }

    private static string   AnyId()  => Any.TransformationId().Generate();
    private static DateOnly AnyDay() => Any.DateOnly().Between(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)).Generate();

    [Fact]
    public void A_blend_needs_at_least_two_inputs() {
        // Arrange
        LotNumber[] anyLots = AnyDistinctLots(2);

        // Act
        Exception? error = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Blend,
                                                                      [new LotQuantity(anyLots[0], Kg(100m))],
                                                                      [new LotQuantity(anyLots[1], Kg(100m))],
                                                                      AnyDay()));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_split_only_produces_sub_lots_of_the_lot_it_divides() {
        // Arrange
        LotNumber[] anyLots    = AnyDistinctLots(2);
        LotNumber   anySource  = anyLots[0];
        LotNumber   anyStranger = anyLots[1].SubLot(1);

        // Act
        Exception? error = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Split,
                                                                      [new LotQuantity(anySource, Kg(100m))],
                                                                      [new LotQuantity(anySource.SubLot(1), Kg(50m)), new LotQuantity(anyStranger, Kg(50m))],
                                                                      AnyDay()));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_step_cannot_produce_more_than_it_consumes() {
        // Arrange
        LotNumber[] anyLots = AnyDistinctLots(2);

        // Act
        Exception? error = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Process,
                                                                      [new LotQuantity(anyLots[0], Kg(100m))],
                                                                      [new LotQuantity(anyLots[1], G(100_001m))],
                                                                      AnyDay()));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_step_cannot_mix_a_weighed_input_with_a_measured_one() {
        // Arrange
        LotNumber[] anyLots = AnyDistinctLots(3);

        // Act
        Exception? error = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Blend,
                                                                      [new LotQuantity(anyLots[0], Kg(10m)), new LotQuantity(anyLots[1], L(10m))],
                                                                      [new LotQuantity(anyLots[2], Kg(5m))],
                                                                      AnyDay()));

        // Assert
        Assert.IsType<ArgumentException>(error);
    }

    [Fact]
    public void A_step_loses_what_it_does_not_yield() {
        // Arrange
        LotNumber[] anyLots = AnyDistinctLots(3);

        // Act
        Transformation blend = new(AnyId(), TransformationKind.Blend,
                                   [new LotQuantity(anyLots[0], Kg(150m)), new LotQuantity(anyLots[1], G(50_000m))],
                                   [new LotQuantity(anyLots[2], Kg(190m))],
                                   AnyDay());

        // Assert
        Assert.Equal(Kg(10m), blend.Loss);
        Assert.Equal(0.95m, blend.Yield);
    }

    [Fact]
    public void Each_input_makes_up_its_share_of_the_total_input_whatever_its_unit() {
        // Arrange
        LotNumber[] anyLots = AnyDistinctLots(3);

        // Act
        Transformation blend = new(AnyId(), TransformationKind.Blend,
                                   [new LotQuantity(anyLots[0], Kg(150m)), new LotQuantity(anyLots[1], G(50_000m))],
                                   [new LotQuantity(anyLots[2], Kg(190m))],
                                   AnyDay());

        // Assert
        Assert.Equal(0.75m, blend.ShareOf(anyLots[0]));
        Assert.Equal(0.25m, blend.ShareOf(anyLots[1]));
    }

}
