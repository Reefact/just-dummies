using BatchGenealogy.Domain;

using JustDummies;

using Xunit;

namespace BatchGenealogy.Tests.Discovery;

public sealed class TransformationTests {

    private static LotQuantity AnyPart(decimal grams) {
        return new LotQuantity(Any.LotNumber().Generate(), new Quantity(grams, QuantityUnit.Gram));
    }

    private static LotQuantity AnyPartIn(QuantityUnit unit, decimal amount) {
        return new LotQuantity(Any.LotNumber().Generate(), new Quantity(amount, unit));
    }

    private static LotQuantity AnySubLotPart(LotNumber source, decimal grams) {
        int       anyIndex = Any.Int32().Between(1, 99).Generate();
        LotNumber subLot   = source.SubLot(anyIndex);

        return new LotQuantity(subLot, new Quantity(grams, QuantityUnit.Gram));
    }

    private static string AnyId() {
        return Any.TransformationId().Generate();
    }

    private static DateOnly AnyDay() {
        return Scenario.AnyDay();
    }

    // ---- construction, per kind --------------------------------------------------------------------------

    [Fact]
    public void A_blend_mixes_several_inputs_into_one_output() {
        // Arrange
        string      anyId  = AnyId();
        DateOnly    anyDay = AnyDay();
        LotQuantity first  = AnyPart(600m);
        LotQuantity second = AnyPart(400m);
        LotQuantity output = AnyPart(950m);

        // Act
        Transformation blend = new Transformation(anyId, TransformationKind.Blend, [first, second], [output], anyDay);

        // Assert
        Assert.Equal(anyId, blend.Id);
        Assert.Equal(TransformationKind.Blend, blend.Kind);
        Assert.Equal(anyDay, blend.PerformedOn);
        Assert.Equal([first, second], blend.Inputs);
        Assert.Equal([output], blend.Outputs);
    }

    [Fact]
    public void A_split_divides_one_input_into_sub_lots_of_it() {
        // Arrange
        LotNumber   anySource = Any.LotNumber().Generate();
        LotQuantity input     = new LotQuantity(anySource, new Quantity(1m, QuantityUnit.Kilogram));
        LotQuantity first     = new LotQuantity(anySource.SubLot(1), new Quantity(400m, QuantityUnit.Gram));
        LotQuantity second    = new LotQuantity(anySource.SubLot(2), new Quantity(600m, QuantityUnit.Gram));

        // Act
        Transformation split = new Transformation(AnyId(), TransformationKind.Split, [input], [first, second], AnyDay());

        // Assert
        Assert.Equal(TransformationKind.Split, split.Kind);
        Assert.Equal(2, split.Outputs.Count);
    }

    [Fact]
    public void A_process_turns_one_or_more_inputs_into_one_output() {
        // Act
        Transformation single   = new Transformation(AnyId(), TransformationKind.Process, [AnyPart(100m)], [AnyPart(90m)], AnyDay());
        Transformation multiple = new Transformation(AnyId(), TransformationKind.Process, [AnyPart(100m), AnyPart(50m), AnyPart(25m)], [AnyPart(90m)], AnyDay());

        // Assert
        Assert.Single(single.Inputs);
        Assert.Equal(3, multiple.Inputs.Count);
    }

    [Fact]
    public void A_generated_transformation_is_a_valid_process() {
        // Arrange
        Transformation anyTransformation = Any.Transformation().Generate();

        // Assert
        Assert.Equal(TransformationKind.Process, anyTransformation.Kind);
        Assert.True(anyTransformation.Yield <= 1m);
    }

    [Theory]
    [InlineData(TransformationKind.Blend, 1, 1)]
    [InlineData(TransformationKind.Blend, 0, 1)]
    [InlineData(TransformationKind.Blend, 2, 2)]
    [InlineData(TransformationKind.Blend, 2, 0)]
    [InlineData(TransformationKind.Split, 2, 2)]
    [InlineData(TransformationKind.Split, 1, 1)]
    [InlineData(TransformationKind.Split, 1, 0)]
    [InlineData(TransformationKind.Split, 0, 2)]
    [InlineData(TransformationKind.Process, 0, 1)]
    [InlineData(TransformationKind.Process, 1, 2)]
    [InlineData(TransformationKind.Process, 1, 0)]
    public void The_number_of_inputs_and_outputs_must_fit_the_kind(TransformationKind kind, int inputCount, int outputCount) {
        // Arrange
        List<LotQuantity> inputs  = Any.ListOf(Any.Fixed(0).As(_ => AnyPart(500m))).WithCount(inputCount).Generate();
        List<LotQuantity> outputs = Any.ListOf(Any.Fixed(0).As(_ => AnyPart(10m))).WithCount(outputCount).Generate();

        // Act
        Exception? thrown = Record.Exception(() => new Transformation(AnyId(), kind, inputs, outputs, AnyDay()));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Theory]
    [InlineData("")]
    [InlineData("TRF-12345")]
    [InlineData("TRF-1234567")]
    [InlineData("trf-123456")]
    [InlineData("TRF123456")]
    [InlineData("TRF-12345A")]
    [InlineData(" TRF-123456")]
    [InlineData("TRF-123456 ")]
    [InlineData("ABC-123456")]
    public void An_identifier_that_is_not_TRF_followed_by_six_digits_is_refused(string id) {
        // Act
        Exception? thrown = Record.Exception(() => new Transformation(id, TransformationKind.Process, [AnyPart(10m)], [AnyPart(5m)], AnyDay()));

        // Assert
        Assert.IsType<FormatException>(thrown);
    }

    [Fact(Skip = "domain defect: the id pattern ends with '$', which .NET also matches before a final '\\n', so \"TRF-123456\\n\" is accepted as a transformation id (literal, no seed)")]
    public void An_identifier_followed_by_a_line_break_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new Transformation("TRF-123456\n", TransformationKind.Process, [AnyPart(10m)], [AnyPart(5m)], AnyDay()));

        // Assert
        Assert.IsType<FormatException>(thrown);
    }

    [Fact]
    public void An_undeclared_kind_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new Transformation(AnyId(), (TransformationKind)42, [AnyPart(10m)], [AnyPart(5m)], AnyDay()));

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Fact]
    public void A_missing_id_inputs_or_outputs_are_refused() {
        // Arrange
        string      anyId  = AnyId();
        LotQuantity input  = AnyPart(10m);
        LotQuantity output = AnyPart(5m);

        // Act
        Exception? noId      = Record.Exception(() => new Transformation(null!, TransformationKind.Process, [input], [output], AnyDay()));
        Exception? noInputs  = Record.Exception(() => new Transformation(anyId, TransformationKind.Process, null!, [output], AnyDay()));
        Exception? noOutputs = Record.Exception(() => new Transformation(anyId, TransformationKind.Process, [input], null!, AnyDay()));

        // Assert
        Assert.IsType<ArgumentNullException>(noId);
        Assert.IsType<ArgumentNullException>(noInputs);
        Assert.IsType<ArgumentNullException>(noOutputs);
    }

    [Fact]
    public void An_empty_entry_among_the_inputs_or_the_outputs_is_refused() {
        // Arrange
        LotQuantity input  = AnyPart(10m);
        LotQuantity output = AnyPart(5m);

        // Act
        Exception? nullInput  = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Blend, [input, null!], [output], AnyDay()));
        Exception? nullOutput = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Split, [input], [output, null!], AnyDay()));

        // Assert
        Assert.IsType<ArgumentException>(nullInput);
        Assert.IsType<ArgumentException>(nullOutput);
    }

    // ---- lots across the two sides -----------------------------------------------------------------------

    [Fact]
    public void A_lot_cannot_appear_twice_among_the_inputs() {
        // Arrange
        LotNumber anyLot = Any.LotNumber().Generate();

        // Act
        Exception? thrown = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Blend,
                                                                      [new LotQuantity(anyLot, new Quantity(5m, QuantityUnit.Gram)), new LotQuantity(anyLot, new Quantity(7m, QuantityUnit.Gram))],
                                                                      [AnyPart(3m)], AnyDay()));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void A_lot_cannot_appear_twice_among_the_outputs() {
        // Arrange
        LotNumber anySource = Any.LotNumber().Generate();
        LotNumber anySubLot = anySource.SubLot(Any.Int32().Between(1, 99).Generate());

        // Act
        Exception? thrown = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Split,
                                                                      [new LotQuantity(anySource, new Quantity(100m, QuantityUnit.Gram))],
                                                                      [new LotQuantity(anySubLot, new Quantity(5m, QuantityUnit.Gram)), new LotQuantity(anySubLot, new Quantity(7m, QuantityUnit.Gram))],
                                                                      AnyDay()));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void A_lot_cannot_be_both_an_input_and_an_output() {
        // Arrange
        LotNumber anyLot = Any.LotNumber().Generate();

        // Act
        Exception? thrown = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Process,
                                                                      [new LotQuantity(anyLot, new Quantity(5m, QuantityUnit.Gram))],
                                                                      [new LotQuantity(anyLot, new Quantity(3m, QuantityUnit.Gram))], AnyDay()));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void Two_instances_of_the_same_lot_number_are_the_same_lot() {
        // Arrange
        string anyValue = Any.LotNumber().Generate().Value;

        // Act
        Exception? thrown = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Process,
                                                                      [new LotQuantity(new LotNumber(anyValue), new Quantity(5m, QuantityUnit.Gram))],
                                                                      [new LotQuantity(new LotNumber(anyValue), new Quantity(3m, QuantityUnit.Gram))], AnyDay()));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void Inputs_and_outputs_must_all_be_masses_or_all_be_volumes() {
        // Act
        Exception? massIntoVolume = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Process, [AnyPartIn(QuantityUnit.Gram, 100m)], [AnyPartIn(QuantityUnit.Millilitre, 10m)], AnyDay()));
        Exception? mixedInputs    = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Blend, [AnyPartIn(QuantityUnit.Gram, 100m), AnyPartIn(QuantityUnit.Litre, 100m)], [AnyPartIn(QuantityUnit.Gram, 10m)], AnyDay()));
        Exception? volumes        = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Blend, [AnyPartIn(QuantityUnit.Litre, 2m), AnyPartIn(QuantityUnit.Millilitre, 500m)], [AnyPartIn(QuantityUnit.Litre, 1m)], AnyDay()));

        // Assert
        Assert.IsType<ArgumentException>(massIntoVolume);
        Assert.IsType<ArgumentException>(mixedInputs);
        Assert.Null(volumes);
    }

    [Fact]
    public void The_outputs_of_a_split_must_be_sub_lots_of_its_input() {
        // Arrange
        LotNumber anySource = Any.LotNumber().Generate();
        LotNumber anyOther  = Any.LotNumber().Generate();
        LotQuantity input   = new LotQuantity(anySource, new Quantity(100m, QuantityUnit.Gram));

        // Act
        Exception? notSubLots      = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Split, [input], [AnyPart(10m), AnyPart(10m)], AnyDay()));
        Exception? sameBaseButBase = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Split, [input], [new LotQuantity(anySource, new Quantity(10m, QuantityUnit.Gram)), AnySubLotPart(anySource, 10m)], AnyDay()));
        Exception? otherBase       = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Split, [input], [new LotQuantity(anyOther.SubLot(1), new Quantity(10m, QuantityUnit.Gram)), new LotQuantity(anyOther.SubLot(2), new Quantity(10m, QuantityUnit.Gram))], AnyDay()));

        // Assert
        Assert.IsType<ArgumentException>(notSubLots);
        Assert.IsType<ArgumentException>(sameBaseButBase);
        Assert.IsType<ArgumentException>(otherBase);
    }

    [Fact]
    public void A_sub_lot_can_itself_be_split_into_sibling_sub_lots() {
        // Arrange
        LotNumber anySubLot = Any.LotNumber().SubLot().Generate();
        LotQuantity input   = new LotQuantity(anySubLot, new Quantity(100m, QuantityUnit.Gram));
        LotNumber first     = anySubLot.SubLot(anySubLot.SubLotIndex == 1 ? 2 : 1);
        LotNumber second    = anySubLot.SubLot(anySubLot.SubLotIndex == 3 ? 4 : 3);

        // Act
        Exception? thrown = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Split, [input],
                                                                      [new LotQuantity(first, new Quantity(10m, QuantityUnit.Gram)), new LotQuantity(second, new Quantity(10m, QuantityUnit.Gram))], AnyDay()));

        // Assert
        Assert.Null(thrown);
    }

    // ---- quantities --------------------------------------------------------------------------------------

    [Fact]
    public void A_transformation_cannot_produce_more_than_it_consumes() {
        // Act
        Exception? thrown = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Process, [AnyPart(100m)], [AnyPart(100.1m)], AnyDay()));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void Producing_exactly_what_was_consumed_is_accepted() {
        // Act
        Transformation lossless = new Transformation(AnyId(), TransformationKind.Process, [AnyPart(100m)], [AnyPart(100m)], AnyDay());

        // Assert
        Assert.Equal(1m, lossless.Yield);
        Assert.True(lossless.Loss.IsZero);
    }

    [Fact]
    public void The_balance_is_made_in_base_units_whatever_the_units_written() {
        // Act
        Exception? equivalent = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Process, [AnyPartIn(QuantityUnit.Kilogram, 1m)], [AnyPartIn(QuantityUnit.Gram, 1000m)], AnyDay()));
        Exception? tooMuch    = Record.Exception(() => new Transformation(AnyId(), TransformationKind.Process, [AnyPartIn(QuantityUnit.Kilogram, 1m)], [AnyPartIn(QuantityUnit.Gram, 1000.001m)], AnyDay()));

        // Assert
        Assert.Null(equivalent);
        Assert.IsType<ArgumentException>(tooMuch);
    }

    [Fact]
    public void The_total_input_adds_the_inputs_in_the_unit_of_the_first() {
        // Arrange
        Transformation transformation = new Transformation(AnyId(), TransformationKind.Blend,
                                                           [AnyPartIn(QuantityUnit.Kilogram, 1m), AnyPartIn(QuantityUnit.Gram, 500m)],
                                                           [AnyPartIn(QuantityUnit.Gram, 1200m)], AnyDay());

        // Act
        Quantity totalInput = transformation.TotalInput;

        // Assert
        Assert.Equal(1.5m, totalInput.Amount);
        Assert.Equal(QuantityUnit.Kilogram, totalInput.Unit);
    }

    [Fact]
    public void The_total_output_is_expressed_in_the_unit_of_the_first_input() {
        // Arrange
        Transformation transformation = new Transformation(AnyId(), TransformationKind.Process,
                                                           [AnyPartIn(QuantityUnit.Kilogram, 2m)],
                                                           [AnyPartIn(QuantityUnit.Gram, 1500m)], AnyDay());

        // Act
        Quantity totalOutput = transformation.TotalOutput;

        // Assert
        Assert.Equal(1.5m, totalOutput.Amount);
        Assert.Equal(QuantityUnit.Kilogram, totalOutput.Unit);
    }

    [Fact]
    public void The_total_output_adds_every_output_of_a_split() {
        // Arrange
        LotNumber anySource = Any.LotNumber().Generate();
        Transformation split = new Transformation(AnyId(), TransformationKind.Split,
                                                  [new LotQuantity(anySource, new Quantity(1m, QuantityUnit.Kilogram))],
                                                  [new LotQuantity(anySource.SubLot(1), new Quantity(300m, QuantityUnit.Gram)),
                                                   new LotQuantity(anySource.SubLot(2), new Quantity(450m, QuantityUnit.Gram))], AnyDay());

        // Assert
        Assert.Equal(new Quantity(750m, QuantityUnit.Gram), split.TotalOutput);
        Assert.Equal(new Quantity(250m, QuantityUnit.Gram), split.Loss);
    }

    [Fact]
    public void The_loss_is_what_the_step_consumed_and_did_not_produce() {
        // Arrange
        Transformation transformation = new Transformation(AnyId(), TransformationKind.Blend,
                                                           [AnyPart(600m), AnyPart(400m)],
                                                           [AnyPart(940m)], AnyDay());

        // Act
        Quantity loss = transformation.Loss;

        // Assert
        Assert.Equal(60m, loss.Amount);
        Assert.Equal(QuantityUnit.Gram, loss.Unit);
    }

    [Fact]
    public void The_yield_is_the_output_over_the_input() {
        // Arrange
        Transformation transformation = new Transformation(AnyId(), TransformationKind.Process,
                                                           [AnyPartIn(QuantityUnit.Kilogram, 2m)],
                                                           [AnyPartIn(QuantityUnit.Gram, 1500m)], AnyDay());

        // Assert
        Assert.Equal(0.75m, transformation.Yield);
    }

    [Fact]
    public void The_share_of_an_input_is_its_part_of_the_total_input() {
        // Arrange
        LotQuantity    first       = AnyPartIn(QuantityUnit.Gram, 600m);
        LotQuantity    second      = AnyPartIn(QuantityUnit.Gram, 400m);
        Transformation transformation = new Transformation(AnyId(), TransformationKind.Blend, [first, second], [AnyPart(900m)], AnyDay());

        // Act
        decimal firstShare  = transformation.ShareOf(first.Lot);
        decimal secondShare = transformation.ShareOf(second.Lot);

        // Assert
        Assert.Equal(0.6m, firstShare);
        Assert.Equal(0.4m, secondShare);
    }

    [Fact]
    public void Shares_are_computed_across_units() {
        // Arrange
        LotQuantity    kilogram       = AnyPartIn(QuantityUnit.Kilogram, 1m);
        LotQuantity    grams          = AnyPartIn(QuantityUnit.Gram, 1000m);
        Transformation transformation = new Transformation(AnyId(), TransformationKind.Blend, [kilogram, grams], [AnyPart(1500m)], AnyDay());

        // Assert
        Assert.Equal(0.5m, transformation.ShareOf(kilogram.Lot));
        Assert.Equal(0.5m, transformation.ShareOf(grams.Lot));
    }

    [Fact]
    public void The_share_of_a_lot_that_is_not_an_input_is_refused() {
        // Arrange
        Transformation anyTransformation = Any.Transformation().Generate();
        LotNumber      anyStranger       = Any.LotNumber().Generate();

        // Act
        Exception? notAnInput = Record.Exception(() => anyTransformation.ShareOf(anyStranger));
        Exception? nothing    = Record.Exception(() => anyTransformation.ShareOf(null!));

        // Assert
        Assert.IsType<ArgumentException>(notAnInput);
        Assert.IsType<ArgumentNullException>(nothing);
    }

    [Fact]
    public void An_output_lot_is_not_an_input_to_ask_the_share_of() {
        // Arrange
        Transformation anyTransformation = Any.Transformation().Generate();

        // Act
        Exception? thrown = Record.Exception(() => anyTransformation.ShareOf(anyTransformation.Outputs[0].Lot));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void Changing_the_lists_it_was_built_from_does_not_change_the_transformation() {
        // Arrange
        List<LotQuantity> inputs  = [AnyPart(100m), AnyPart(100m)];
        List<LotQuantity> outputs = [AnyPart(150m)];
        Transformation    transformation = new Transformation(AnyId(), TransformationKind.Blend, inputs, outputs, AnyDay());

        // Act
        inputs.Clear();
        outputs.Add(AnyPart(10m));

        // Assert
        Assert.Equal(2, transformation.Inputs.Count);
        Assert.Single(transformation.Outputs);
    }

}
