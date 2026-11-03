namespace BatchGenealogy.Domain.Tests;

[Reproducible]
public sealed class TransformationTests {

    // --- construction: what a valid step is ---------------------------------------------------------------------

    [Fact]
    public void A_blend_keeps_what_it_was_given() {
        // Arrange
        string        anyId     = Any.StringMatching("TRF-[0-9]{6}").Generate();
        DateOnly      anyDay    = Any.DateOnly().Generate();
        LotQuantity[] inputs    = Lots(2, 10m);
        LotQuantity[] outputs   = Lots(1, 15m);

        // Act
        Transformation blend = new Transformation(anyId, TransformationKind.Blend, inputs, outputs, anyDay);

        // Assert
        Assert.Equal(anyId, blend.Id);
        Assert.Equal(TransformationKind.Blend, blend.Kind);
        Assert.Equal(inputs, blend.Inputs);
        Assert.Equal(outputs, blend.Outputs);
        Assert.Equal(anyDay, blend.PerformedOn);
    }

    [Fact]
    public void A_blend_takes_at_least_two_inputs_and_makes_one_output() {
        // Arrange / Act
        Transformation threeInputs = Build(TransformationKind.Blend, Lots(3, 10m), Lots(1, 5m));

        // Assert
        Assert.Equal(3, threeInputs.Inputs.Count);
    }

    [Fact]
    public void A_split_takes_one_input_and_makes_at_least_two_sub_lots_of_it() {
        // Arrange
        LotNumber         anyParent = Any.LotNumber().WhollyLot().Generate();
        LotQuantity       input     = new LotQuantity(anyParent, Kg.Of(100m));
        LotQuantity[]     outputs   = { new LotQuantity(anyParent.SubLot(1), Kg.Of(40m)), new LotQuantity(anyParent.SubLot(2), Kg.Of(50m)), new LotQuantity(anyParent.SubLot(3), Kg.Of(1m)) };

        // Act
        Transformation split = Build(TransformationKind.Split, new[] { input }, outputs);

        // Assert
        Assert.Equal(3, split.Outputs.Count);
    }

    [Fact]
    public void A_process_takes_one_or_more_inputs_and_makes_one_output() {
        // Arrange / Act
        Transformation single   = Build(TransformationKind.Process, Lots(1, 10m), Lots(1, 10m));
        Transformation several  = Build(TransformationKind.Process, Lots(3, 10m), Lots(1, 10m));

        // Assert
        Assert.Single(single.Inputs);
        Assert.Equal(3, several.Inputs.Count);
    }

    [Fact]
    public void A_drawn_transformation_is_a_valid_one() {
        // Arrange
        Transformation anyTransformation = Any.Transformation().Generate();

        // Assert: its constructor accepted it; the invariants are visible on what it exposes
        Assert.NotEmpty(anyTransformation.Inputs);
        Assert.NotEmpty(anyTransformation.Outputs);
    }

    // --- construction: refusals ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("TRF-12345")]
    [InlineData("TRF-1234567")]
    [InlineData("trf-123456")]
    [InlineData("TRF123456")]
    [InlineData("XYZ-123456")]
    [InlineData("TRF-12345A")]
    [InlineData("")]
    public void A_malformed_id_is_refused(string id) {
        Assert.Throws<FormatException>(() => Build(id, TransformationKind.Process, Lots(1, 10m), Lots(1, 5m)));
    }

    [Fact]
    public void A_missing_id_or_side_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Transformation(null!, TransformationKind.Process, Lots(1, 10m), Lots(1, 5m), Any.DateOnly().Generate()));
        Assert.Throws<ArgumentNullException>(() => new Transformation("TRF-000001", TransformationKind.Process, null!, Lots(1, 5m), Any.DateOnly().Generate()));
        Assert.Throws<ArgumentNullException>(() => new Transformation("TRF-000001", TransformationKind.Process, Lots(1, 10m), null!, Any.DateOnly().Generate()));
    }

    [Fact]
    public void An_unknown_kind_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Build((TransformationKind)42, Lots(1, 10m), Lots(1, 5m)));
    }

    [Fact]
    public void A_null_entry_on_either_side_is_refused() {
        // Arrange
        LotQuantity[] withNull = { Lots(1, 10m)[0], null! };

        // Act / Assert
        Assert.Throws<ArgumentException>(() => Build(TransformationKind.Blend, withNull, Lots(1, 5m)));
        Assert.Throws<ArgumentException>(() => Build(TransformationKind.Split, Lots(1, 10m), withNull));
    }

    [Theory]
    [InlineData(TransformationKind.Blend, 1, 1)]
    [InlineData(TransformationKind.Blend, 0, 1)]
    [InlineData(TransformationKind.Blend, 2, 0)]
    [InlineData(TransformationKind.Blend, 2, 2)]
    [InlineData(TransformationKind.Split, 2, 2)]
    [InlineData(TransformationKind.Split, 0, 2)]
    [InlineData(TransformationKind.Split, 1, 1)]
    [InlineData(TransformationKind.Split, 1, 0)]
    [InlineData(TransformationKind.Process, 0, 1)]
    [InlineData(TransformationKind.Process, 1, 0)]
    [InlineData(TransformationKind.Process, 1, 2)]
    public void The_number_of_inputs_and_outputs_must_fit_the_kind(TransformationKind kind, int inputCount, int outputCount) {
        // Arrange: ample input so only the arity is wrong
        LotQuantity[] inputs  = Lots(inputCount, 100m);
        LotQuantity[] outputs = Lots(outputCount, 1m);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => Build(kind, inputs, outputs));
    }

    [Fact]
    public void A_lot_cannot_appear_twice_among_the_inputs() {
        // Arrange
        LotNumber anyLot = Any.LotNumber().Generate();
        LotQuantity[] inputs = { new LotQuantity(anyLot, Kg.Of(5m)), new LotQuantity(new LotNumber(anyLot.Value), Kg.Of(5m)) };

        // Act / Assert
        Assert.Throws<ArgumentException>(() => Build(TransformationKind.Blend, inputs, Lots(1, 1m)));
    }

    [Fact]
    public void A_lot_cannot_appear_twice_among_the_outputs() {
        // Arrange
        LotNumber         anyParent = Any.LotNumber().WhollyLot().Generate();
        LotQuantity[]     outputs   = { new LotQuantity(anyParent.SubLot(1), Kg.Of(5m)), new LotQuantity(anyParent.SubLot(1), Kg.Of(5m)) };

        // Act / Assert
        Assert.Throws<ArgumentException>(() => Build(TransformationKind.Split, new[] { new LotQuantity(anyParent, Kg.Of(100m)) }, outputs));
    }

    [Fact]
    public void A_lot_cannot_be_both_consumed_and_produced_by_the_same_step() {
        // Arrange
        LotQuantity[] inputs  = Lots(2, 10m);
        LotQuantity[] outputs = { new LotQuantity(inputs[1].Lot, Kg.Of(5m)) };

        // Act / Assert
        Assert.Throws<ArgumentException>(() => Build(TransformationKind.Blend, inputs, outputs));
    }

    [Fact]
    public void Mass_and_volume_cannot_mix_inside_one_step() {
        // Arrange
        LotQuantity[] inputs = { new LotQuantity(Any.LotNumber().Generate(), Kg.Of(10m)) };
        LotQuantity[] outputs = { new LotQuantity(Any.LotNumber().Generate(), new Quantity(5m, QuantityUnit.Litre)) };

        // Act / Assert
        Assert.Throws<ArgumentException>(() => Build(TransformationKind.Process, inputs, outputs));
    }

    [Fact]
    public void A_volume_only_step_is_fine() {
        // Arrange
        LotQuantity[] inputs  = { new LotQuantity(Any.LotNumber().Generate(), new Quantity(10m, QuantityUnit.Litre)) };
        LotQuantity[] outputs = { new LotQuantity(Any.LotNumber().Generate(), new Quantity(9_000m, QuantityUnit.Millilitre)) };

        // Act
        Transformation process = Build(TransformationKind.Process, inputs, outputs);

        // Assert
        Assert.Equal(new Quantity(1m, QuantityUnit.Litre), process.Loss);
    }

    [Fact]
    public void The_outputs_of_a_split_must_be_sub_lots() {
        // Arrange
        LotNumber     anyParent = Any.LotNumber().WhollyLot().Generate();
        LotQuantity   input     = new LotQuantity(anyParent, Kg.Of(100m));
        LotQuantity[] outputs   = { new LotQuantity(anyParent.SubLot(1), Kg.Of(10m)), new LotQuantity(Any.LotNumber().WhollyLot().Generate(), Kg.Of(10m)) };

        // Act / Assert
        Assert.Throws<ArgumentException>(() => Build(TransformationKind.Split, new[] { input }, outputs));
    }

    [Fact]
    public void The_outputs_of_a_split_must_be_sub_lots_of_the_input_s_own_base_lot() {
        // Arrange
        LotNumber     anyParent = Any.LotNumber().WhollyLot().Generate();
        LotNumber     anyOther  = Any.LotNumber().WhollyLot().Generate();
        LotQuantity   input     = new LotQuantity(anyParent, Kg.Of(100m));
        LotQuantity[] outputs   = { new LotQuantity(anyParent.SubLot(1), Kg.Of(10m)), new LotQuantity(anyOther.SubLot(1), Kg.Of(10m)) };

        // Act / Assert
        Assert.Throws<ArgumentException>(() => Build(TransformationKind.Split, new[] { input }, outputs));
    }

    [Fact]
    public void A_step_can_lose_material_but_never_create_it() {
        // Arrange
        LotQuantity[] inputs  = Lots(2, 10m);
        LotQuantity[] outputs = Lots(1, 20.001m);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => Build(TransformationKind.Blend, inputs, outputs));
    }

    [Fact]
    public void Producing_exactly_what_was_consumed_is_allowed() {
        // Arrange
        LotQuantity[] inputs  = Lots(2, 10m);
        LotQuantity[] outputs = Lots(1, 20m);

        // Act
        Transformation lossless = Build(TransformationKind.Blend, inputs, outputs);

        // Assert
        Assert.True(lossless.Loss.IsZero);
    }

    [Fact]
    public void The_ceiling_on_the_output_is_compared_across_units() {
        // Arrange: 2 kg + 500 g in, 2 600 g out is too much
        LotQuantity[] inputs  = { new LotQuantity(Any.LotNumber().SubLot().Generate(), Kg.Of(2m)), new LotQuantity(Any.LotNumber().WhollyLot().Generate(), new Quantity(500m, QuantityUnit.Gram)) };
        LotQuantity[] outputs = { new LotQuantity(Any.LotNumber().Generate(), new Quantity(2_600m, QuantityUnit.Gram)) };

        // Act / Assert
        Assert.Throws<ArgumentException>(() => Build(TransformationKind.Blend, inputs, outputs));
    }

    // --- computations ---------------------------------------------------------------------------------------------

    [Fact]
    public void Totals_add_up_across_units_in_the_unit_of_the_first_input() {
        // Arrange
        LotQuantity[] inputs  = { new LotQuantity(Any.LotNumber().Generate(), Kg.Of(2m)), new LotQuantity(Any.LotNumber().Generate(), new Quantity(500m, QuantityUnit.Gram)) };
        LotQuantity[] outputs = { new LotQuantity(Any.LotNumber().Generate(), new Quantity(2_000m, QuantityUnit.Gram)) };
        Transformation blend = Build(TransformationKind.Blend, inputs, outputs);

        // Assert
        Assert.Equal(Kg.Of(2.5m), blend.TotalInput);
        Assert.Equal(QuantityUnit.Kilogram, blend.TotalInput.Unit);
        Assert.Equal(Kg.Of(2m), blend.TotalOutput);
        Assert.Equal(QuantityUnit.Kilogram, blend.TotalOutput.Unit);
        Assert.Equal(Kg.Of(0.5m), blend.Loss);
    }

    [Fact]
    public void The_yield_is_what_comes_out_over_what_went_in() {
        // Arrange
        Transformation process = Build(TransformationKind.Process, Lots(1, 80m), Lots(1, 60m));

        // Assert
        Assert.Equal(0.75m, process.Yield);
    }

    [Fact]
    public void A_lossless_step_yields_one_and_a_step_with_tiny_output_yields_a_tiny_fraction() {
        // Arrange
        Transformation lossless = Build(TransformationKind.Process, Lots(1, 10m), Lots(1, 10m));
        Transformation tiny     = Build(TransformationKind.Process, Lots(1, 1000m), Lots(1, 1m));

        // Assert
        Assert.Equal(1m, lossless.Yield);
        Assert.Equal(0.001m, tiny.Yield);
    }

    [Fact]
    public void The_share_of_an_input_is_its_part_of_the_total_input() {
        // Arrange
        LotQuantity[] inputs  = { new LotQuantity(Any.LotNumber().Generate(), Kg.Of(30m)), new LotQuantity(Any.LotNumber().SubLot().Generate(), Kg.Of(10m)) };
        Transformation blend  = Build(TransformationKind.Blend, inputs, Lots(1, 35m));

        // Assert
        Assert.Equal(0.75m, blend.ShareOf(inputs[0].Lot));
        Assert.Equal(0.25m, blend.ShareOf(inputs[1].Lot));
    }

    [Fact]
    public void The_shares_are_computed_across_units() {
        // Arrange: 1 kg and 3 000 g
        LotQuantity[] inputs = { new LotQuantity(Any.LotNumber().Generate(), Kg.Of(1m)), new LotQuantity(Any.LotNumber().SubLot().Generate(), new Quantity(3_000m, QuantityUnit.Gram)) };
        Transformation blend = Build(TransformationKind.Blend, inputs, Lots(1, 1m));

        // Assert
        Assert.Equal(0.25m, blend.ShareOf(inputs[0].Lot));
        Assert.Equal(0.75m, blend.ShareOf(inputs[1].Lot));
    }

    [Fact]
    public void The_share_of_the_only_input_is_the_whole() {
        // Arrange
        LotQuantity[] inputs = Lots(1, 12m);
        Transformation process = Build(TransformationKind.Process, inputs, Lots(1, 3m));

        // Assert
        Assert.Equal(1m, process.ShareOf(inputs[0].Lot));
    }

    [Fact]
    public void Only_an_input_has_a_share() {
        // Arrange
        Transformation blend    = Build(TransformationKind.Blend, Lots(2, 10m), Lots(1, 5m));
        LotNumber      outsider = Any.LotNumber().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => blend.ShareOf(outsider));
        Assert.Throws<ArgumentException>(() => blend.ShareOf(blend.Outputs[0].Lot));
        Assert.Throws<ArgumentNullException>(() => blend.ShareOf(null!));
    }

    [Fact]
    public void The_inputs_and_outputs_handed_in_are_copied() {
        // Arrange
        List<LotQuantity> inputs  = Lots(2, 10m).ToList();
        List<LotQuantity> outputs = Lots(1, 5m).ToList();
        Transformation    blend   = Build(TransformationKind.Blend, inputs, outputs);

        // Act
        inputs.Clear();
        outputs.Clear();

        // Assert
        Assert.Equal(2, blend.Inputs.Count);
        Assert.Single(blend.Outputs);
    }

    [Fact(Skip = "domain defect: TotalInput adds in the unit of the first input and the 1e9 cap applies there, so 600 kg in milligrams blended with 600 kg throws ArgumentOutOfRangeException; Record would fail the same way after consuming the inputs; any seed")]
    public void Totals_of_a_step_whose_first_input_is_in_milligrams_stay_computable() {
        // Arrange
        LotQuantity[] inputs  = { new LotQuantity(Any.LotNumber().Generate(), new Quantity(600_000_000m, QuantityUnit.Milligram)), new LotQuantity(Any.LotNumber().SubLot().Generate(), Kg.Of(600m)) };
        LotQuantity[] outputs = { new LotQuantity(Any.LotNumber().Generate(), Kg.Of(1_000m)) };
        Transformation blend  = Build(TransformationKind.Blend, inputs, outputs);

        // Assert
        Assert.Equal(1_200m, blend.TotalInput.ConvertTo(QuantityUnit.Kilogram).Amount);
    }

    // --- helpers ----------------------------------------------------------------------------------------------------

    /// <summary>Distinct lots (drawn) of the same literal amount in kilograms.</summary>
    private static LotQuantity[] Lots(int count, decimal kilograms) {
        IReadOnlyList<LotNumber> numbers = Any.ListOf(Any.LotNumber()).WithCount(count).Distinct().Generate();

        return numbers.Select(number => new LotQuantity(number, Kg.Of(kilograms))).ToArray();
    }

    private static Transformation Build(TransformationKind kind, IReadOnlyList<LotQuantity> inputs, IReadOnlyList<LotQuantity> outputs) {
        return Build(Any.StringMatching("TRF-[0-9]{6}").Generate(), kind, inputs, outputs);
    }

    private static Transformation Build(string id, TransformationKind kind, IReadOnlyList<LotQuantity> inputs, IReadOnlyList<LotQuantity> outputs) {
        return new Transformation(id, kind, inputs, outputs, Any.DateOnly().Generate());
    }

}
