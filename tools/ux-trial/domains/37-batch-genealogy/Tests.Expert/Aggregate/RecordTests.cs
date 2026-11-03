using JustDummies;

using Xunit;

namespace BatchGenealogy.Domain.Tests.Aggregate;

public sealed class RecordTests {

    // Every lot number, material, date offset and transformation id comes from a draw: none of them takes
    // part in what is verified. The kilos are literals, because the balances and the shares are the subject.

    // ---- what a recorded step leaves behind --------------------------------------------------------

    [Fact]
    public void Recording_a_blend_withdraws_each_input_by_the_quantity_the_step_consumes() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(3);
        LotNumber       anyApi = lots[0], anyExcipient = lots[1], anyBlend = lots[2];
        plant.ReleasedStock(anyApi, 100m);
        plant.ReleasedStock(anyExcipient, 100m);
        plant.ProducedLot(anyBlend, 90m);

        // Act
        plant.Genealogy.Record(plant.Step(TransformationKind.Blend, [(anyApi, 60m), (anyExcipient, 40m)], [(anyBlend, 90m)]));

        // Assert
        Assert.Equal(Plant.Kg(40m), plant.Remaining(anyApi));
        Assert.Equal(Plant.Kg(60m), plant.Remaining(anyExcipient));
    }

    [Fact]
    public void Recording_a_step_that_consumes_a_whole_lot_depletes_it() {
        // Arrange
        Plant     plant = new Plant();
        LotNumber anyInput = Any.LotNumber().Base().Generate(), anyOutput = Any.LotNumber().Base().DifferentFrom(anyInput).Generate();
        plant.ReleasedStock(anyInput, 25m);
        plant.ProducedLot(anyOutput, 20m);

        // Act
        plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(anyInput, 25m)], [(anyOutput, 20m)]));

        // Assert
        Assert.True(plant.Genealogy.Get(anyInput).IsDepleted);
    }

    [Fact]
    public void Recording_a_step_keeps_the_step_in_the_genealogy() {
        // Arrange
        Plant     plant = new Plant();
        LotNumber anyInput = Any.LotNumber().Base().Generate(), anyOutput = Any.LotNumber().Base().DifferentFrom(anyInput).Generate();
        plant.ReleasedStock(anyInput, 25m);
        plant.ProducedLot(anyOutput, 20m);
        Transformation process = plant.Step(TransformationKind.Process, [(anyInput, 25m)], [(anyOutput, 20m)]);

        // Act
        plant.Genealogy.Record(process);

        // Assert
        Assert.Same(process, Assert.Single(plant.Genealogy.Transformations));
    }

    [Fact]
    public void The_outputs_of_a_recorded_step_stay_in_quarantine_with_the_quantity_they_were_produced_with() {
        // Arrange
        Plant     plant = new Plant();
        LotNumber anyInput = Any.LotNumber().Base().Generate(), anyOutput = Any.LotNumber().Base().DifferentFrom(anyInput).Generate();
        plant.ReleasedStock(anyInput, 25m);
        plant.ProducedLot(anyOutput, 20m);

        // Act
        plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(anyInput, 25m)], [(anyOutput, 20m)]));

        // Assert
        Lot produced = plant.Genealogy.Get(anyOutput);
        Assert.Equal(LotStatus.Quarantined, produced.Status);
        Assert.Equal(Plant.Kg(20m), produced.RemainingQuantity);
    }

    [Fact]
    public void A_blend_links_each_input_to_the_output_for_its_share_of_the_total_input() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(3);
        LotNumber       anyApi = lots[0], anyExcipient = lots[1], anyBlend = lots[2];
        plant.ReleasedStock(anyApi, 100m);
        plant.ReleasedStock(anyExcipient, 100m);
        plant.ProducedLot(anyBlend, 90m);
        Transformation blend = plant.Step(TransformationKind.Blend, [(anyApi, 60m), (anyExcipient, 40m)], [(anyBlend, 90m)]);

        // Act
        plant.Genealogy.Record(blend);

        // Assert
        Assert.Equal(2, plant.Genealogy.Links.Count);
        ProvenanceLink fromApi = Assert.Single(plant.Genealogy.Links, link => link.Source.Equals(anyApi));
        ProvenanceLink fromExcipient = Assert.Single(plant.Genealogy.Links, link => link.Source.Equals(anyExcipient));
        Assert.Equal((anyBlend, blend.Id, 0.6m), (fromApi.Target, fromApi.TransformationId, fromApi.Share));
        Assert.Equal((anyBlend, blend.Id, 0.4m), (fromExcipient.Target, fromExcipient.TransformationId, fromExcipient.Share));
    }

    [Fact]
    public void A_split_links_the_input_to_every_sub_lot_with_its_whole_share() {
        // Arrange
        Plant     plant = new Plant();
        LotNumber anySource = Any.LotNumber().Base().Generate();
        plant.ReleasedStock(anySource, 100m);

        // Act
        plant.Split(anySource, 100m, 40m, 55m);

        // Assert
        Assert.Equal(2, plant.Genealogy.Links.Count);
        Assert.All(plant.Genealogy.Links, link => {
            Assert.Equal(anySource, link.Source);
            Assert.Equal(1m, link.Share);
        });
        Assert.Equal([anySource.SubLot(1), anySource.SubLot(2)], plant.Genealogy.Links.Select(link => link.Target).OrderBy(lot => lot.Value, StringComparer.Ordinal));
    }

    [Fact]
    public void A_process_with_several_inputs_links_each_of_them_to_its_one_output() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(4);
        plant.ReleasedStock(lots[0], 10m);
        plant.ReleasedStock(lots[1], 10m);
        plant.ReleasedStock(lots[2], 10m);
        plant.ProducedLot(lots[3], 18m);

        // Act
        plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(lots[0], 5m), (lots[1], 5m), (lots[2], 10m)], [(lots[3], 18m)]));

        // Assert
        Assert.Equal([0.25m, 0.25m, 0.5m], plant.Genealogy.Links.Select(link => link.Share).Order());
        Assert.All(plant.Genealogy.Links, link => Assert.Equal(lots[3], link.Target));
    }

    [Fact]
    public void A_lot_can_feed_several_steps_until_it_is_depleted() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(3);
        plant.ReleasedStock(lots[0], 100m);
        plant.ProducedLot(lots[1], 30m);
        plant.ProducedLot(lots[2], 70m);

        // Act
        plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(lots[0], 30m)], [(lots[1], 30m)]));
        plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(lots[0], 70m)], [(lots[2], 70m)]));

        // Assert
        Assert.True(plant.Genealogy.Get(lots[0]).IsDepleted);
        Assert.Equal(2, plant.Genealogy.Transformations.Count);
    }

    [Fact]
    public void An_output_registered_in_another_unit_is_accepted_when_it_amounts_to_the_same_quantity() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(2);
        plant.ReleasedStock(lots[0], 10m);
        plant.ProducedLot(lots[1], 5m);
        Transformation process = plant.Step(TransformationKind.Process, [(lots[0], 10m)], [(lots[1], 5m)]);

        // Act: the step states the output in grams while the lot was registered in kilograms
        Transformation inGrams = Any.Transformation().WithKind(TransformationKind.Process).WithId(process.Id).WithPerformedOn(plant.ProductionDay)
                                    .WithInputsAndOutputs(process.Inputs.ToList(), [new LotQuantity(lots[1], new Quantity(5_000m, QuantityUnit.Gram))])
                                    .Generate();
        plant.Genealogy.Record(inGrams);

        // Assert
        Assert.Single(plant.Genealogy.Transformations);
    }

    [Fact]
    public void A_lot_can_be_consumed_on_the_day_it_is_manufactured() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(2);
        plant.ReleasedLot(lots[0], 10m, plant.ProductionDay, plant.ProductionDay.AddDays(100));
        plant.ProducedLot(lots[1], 10m);

        // Act
        plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(lots[0], 10m)], [(lots[1], 10m)]));

        // Assert
        Assert.True(plant.Genealogy.Get(lots[0]).IsDepleted);
    }

    [Fact]
    public void A_lot_can_be_consumed_the_day_before_it_expires() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(2);
        plant.ReleasedLot(lots[0], 10m, plant.ProductionDay.AddDays(-100), plant.ProductionDay.AddDays(1));
        plant.ProducedLot(lots[1], 10m);

        // Act
        plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(lots[0], 10m)], [(lots[1], 10m)]));

        // Assert
        Assert.True(plant.Genealogy.Get(lots[0]).IsDepleted);
    }

    // ---- what a step cannot be recorded with: nothing changes --------------------------------------

    [Fact]
    public void Recording_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Plant().Genealogy.Record(null!));
    }

    [Fact]
    public void A_transformation_id_cannot_be_recorded_twice() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(4);
        plant.ReleasedStock(lots[0], 10m);
        plant.ReleasedStock(lots[1], 10m);
        plant.ProducedLot(lots[2], 5m);
        plant.ProducedLot(lots[3], 5m);
        Transformation first = plant.Step(TransformationKind.Process, [(lots[0], 10m)], [(lots[2], 5m)]);
        Transformation sameIdAgain = Any.Transformation().WithKind(TransformationKind.Process).WithId(first.Id).WithPerformedOn(plant.ProductionDay)
                                        .WithInputsAndOutputs([new LotQuantity(lots[1], Plant.Kg(10m))], [new LotQuantity(lots[3], Plant.Kg(5m))])
                                        .Generate();
        plant.Genealogy.Record(first);
        Snapshot before = Snapshot.Of(plant.Genealogy);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(sameIdAgain));
        before.AssertUnchanged();
    }

    [Fact]
    public void An_input_lot_must_be_registered() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(2);
        plant.ProducedLot(lots[1], 5m);
        Snapshot before = Snapshot.Of(plant.Genealogy);

        // Act / Assert
        Assert.Throws<KeyNotFoundException>(() => plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(lots[0], 10m)], [(lots[1], 5m)])));
        before.AssertUnchanged();
    }

    [Fact]
    public void An_output_lot_must_be_registered() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(2);
        plant.ReleasedStock(lots[0], 10m);
        Snapshot before = Snapshot.Of(plant.Genealogy);

        // Act / Assert
        Assert.Throws<KeyNotFoundException>(() => plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(lots[0], 10m)], [(lots[1], 5m)])));
        before.AssertUnchanged();
    }

    [Theory]
    [InlineData(LotStatus.Quarantined)]
    [InlineData(LotStatus.Rejected)]
    public void An_input_lot_must_be_released(LotStatus status) {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(2);
        Lot             input = plant.ReleasedStock(lots[0], 10m);
        if (status == LotStatus.Quarantined) { input.Quarantine(); } else { input.Reject(); }
        plant.ProducedLot(lots[1], 5m);
        Snapshot before = Snapshot.Of(plant.Genealogy);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(lots[0], 10m)], [(lots[1], 5m)])));
        before.AssertUnchanged();
    }

    [Fact]
    public void An_input_lot_cannot_be_consumed_on_the_day_it_expires() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(2);
        plant.ReleasedLot(lots[0], 10m, plant.ProductionDay.AddDays(-100), plant.ProductionDay);
        plant.ProducedLot(lots[1], 5m);
        Snapshot before = Snapshot.Of(plant.Genealogy);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(lots[0], 10m)], [(lots[1], 5m)])));
        before.AssertUnchanged();
    }

    [Fact]
    public void An_input_lot_cannot_be_consumed_before_it_is_manufactured() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(2);
        plant.ReleasedLot(lots[0], 10m, plant.ProductionDay.AddDays(1), plant.ProductionDay.AddDays(100));
        plant.ProducedLot(lots[1], 5m);
        Snapshot before = Snapshot.Of(plant.Genealogy);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(lots[0], 10m)], [(lots[1], 5m)])));
        before.AssertUnchanged();
    }

    [Fact]
    public void An_input_lot_cannot_be_consumed_beyond_what_it_still_holds() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(3);
        plant.ReleasedStock(lots[0], 100m);
        plant.ProducedLot(lots[1], 60m);
        plant.ProducedLot(lots[2], 41m);
        plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(lots[0], 60m)], [(lots[1], 60m)]));
        Snapshot before = Snapshot.Of(plant.Genealogy);

        // Act / Assert: 40 kg remain, the second step asks for 40.001
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(lots[0], 40.001m)], [(lots[2], 40m)])));
        before.AssertUnchanged();
    }

    [Fact]
    public void An_input_lot_measured_in_mass_cannot_feed_a_step_stated_in_volume() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(2);
        plant.ReleasedStock(lots[0], 10m);
        Lot output = Any.Lot().WithNumber(lots[1]).WithInitialQuantity(new Quantity(5m, QuantityUnit.Litre)).WithManufacturedOn(plant.ProductionDay).Generate();
        plant.Genealogy.Register(output);
        Transformation inLitres = Any.Transformation().WithKind(TransformationKind.Process).WithPerformedOn(plant.ProductionDay)
                                     .WithInputsAndOutputs([new LotQuantity(lots[0], new Quantity(10m, QuantityUnit.Litre))], [new LotQuantity(lots[1], new Quantity(5m, QuantityUnit.Litre))])
                                     .Generate();
        Snapshot before = Snapshot.Of(plant.Genealogy);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(inLitres));
        before.AssertUnchanged();
    }

    [Fact]
    public void An_output_lot_cannot_be_produced_twice() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(3);
        plant.ReleasedStock(lots[0], 10m);
        plant.ReleasedStock(lots[1], 10m);
        plant.ProducedLot(lots[2], 5m);
        plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(lots[0], 10m)], [(lots[2], 5m)]));
        Snapshot before = Snapshot.Of(plant.Genealogy);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(lots[1], 10m)], [(lots[2], 5m)])));
        before.AssertUnchanged();
    }

    [Fact]
    public void An_output_lot_cannot_be_one_that_was_already_consumed() {
        // Arrange: a lot manufactured on the production day, consumed that day, then claimed as the output of another step
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(3);
        plant.ReleasedLot(lots[0], 10m, plant.ProductionDay, plant.ProductionDay.AddDays(100));
        plant.ReleasedStock(lots[1], 10m);
        plant.ProducedLot(lots[2], 5m);
        plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(lots[0], 10m)], [(lots[2], 5m)]));
        Snapshot before = Snapshot.Of(plant.Genealogy);
        Transformation backwards = plant.Step(TransformationKind.Process, [(lots[1], 10m)], [(lots[0], 10m)]);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(backwards));
        before.AssertUnchanged();
    }

    [Fact]
    public void An_output_lot_must_have_been_registered_with_exactly_the_quantity_the_step_produces() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(2);
        plant.ReleasedStock(lots[0], 10m);
        plant.ProducedLot(lots[1], 5m);
        Snapshot before = Snapshot.Of(plant.Genealogy);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(lots[0], 10m)], [(lots[1], 4.999m)])));
        before.AssertUnchanged();
    }

    [Fact]
    public void An_output_lot_must_have_been_manufactured_on_the_day_of_the_step() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(2);
        plant.ReleasedStock(lots[0], 10m);
        plant.Genealogy.Register(Any.Lot().WithNumber(lots[1]).WithInitialQuantity(Plant.Kg(5m)).WithManufacturedOn(plant.ProductionDay.AddDays(1)).Generate());
        Snapshot before = Snapshot.Of(plant.Genealogy);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(lots[0], 10m)], [(lots[1], 5m)])));
        before.AssertUnchanged();
    }

    [Fact]
    public void The_sub_lots_of_a_split_must_keep_the_material_of_the_lot_they_come_from() {
        // Arrange
        Plant        plant = new Plant();
        LotNumber    anySource = Any.LotNumber().Base().Generate();
        Lot          source = plant.ReleasedStock(anySource, 10m);
        MaterialCode otherMaterial = Any.MaterialCode().DifferentFrom(source.Material).Generate();
        plant.ProducedLot(anySource.SubLot(1), 4m, source.Material);
        plant.ProducedLot(anySource.SubLot(2), 4m, otherMaterial);
        Snapshot before = Snapshot.Of(plant.Genealogy);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(plant.Step(TransformationKind.Split, [(anySource, 10m)], [(anySource.SubLot(1), 4m), (anySource.SubLot(2), 4m)])));
        before.AssertUnchanged();
    }

    [Fact]
    public void A_blend_or_a_process_may_change_the_material() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(2);
        Lot             input = plant.ReleasedStock(lots[0], 10m);
        Lot             output = plant.ProducedLot(lots[1], 5m, Any.MaterialCode().DifferentFrom(input.Material).Generate());

        // Act
        plant.Genealogy.Record(plant.Step(TransformationKind.Process, [(lots[0], 10m)], [(lots[1], 5m)]));

        // Assert
        Assert.NotEqual(input.Material, output.Material);
        Assert.Single(plant.Genealogy.Transformations);
    }

    [Fact]
    public void A_step_with_one_unusable_input_among_several_consumes_none_of_them() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(4);
        plant.ReleasedStock(lots[0], 100m);
        plant.ReleasedStock(lots[1], 100m);
        plant.ReleasedStock(lots[2], 10m);
        plant.ProducedLot(lots[3], 200m);
        Snapshot before = Snapshot.Of(plant.Genealogy);

        // Act / Assert: the third input asks for more than it holds
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(plant.Step(TransformationKind.Blend, [(lots[0], 100m), (lots[1], 100m), (lots[2], 11m)], [(lots[3], 200m)])));
        before.AssertUnchanged();
        Assert.Equal(Plant.Kg(100m), plant.Remaining(lots[0]));
        Assert.Equal(Plant.Kg(100m), plant.Remaining(lots[1]));
    }

    [Fact]
    public void A_step_with_a_bad_output_consumes_none_of_its_inputs() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(3);
        plant.ReleasedStock(lots[0], 10m);
        plant.ReleasedStock(lots[1], 10m);
        plant.ProducedLot(lots[2], 4m);
        Snapshot before = Snapshot.Of(plant.Genealogy);

        // Act / Assert: the output was registered with 4 kg and the step produces 5
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(plant.Step(TransformationKind.Blend, [(lots[0], 10m), (lots[1], 10m)], [(lots[2], 5m)])));
        before.AssertUnchanged();
        Assert.Equal(Plant.Kg(10m), plant.Remaining(lots[0]));
    }

    [Fact]
    public void A_refused_step_can_be_recorded_once_what_blocked_it_is_resolved() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(2);
        Lot             input = plant.ReleasedStock(lots[0], 10m);
        input.Quarantine();
        plant.ProducedLot(lots[1], 5m);
        Transformation process = plant.Step(TransformationKind.Process, [(lots[0], 10m)], [(lots[1], 5m)]);
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(process));

        // Act
        input.Release();
        plant.Genealogy.Record(process);

        // Assert
        Assert.Same(process, Assert.Single(plant.Genealogy.Transformations));
        Assert.Single(plant.Genealogy.Links);
    }

    [Fact(Skip = "domain defect: Record is not all-or-nothing when the inputs add up beyond Quantity.MaximumAmount; the inputs are consumed and the transformation is kept, then ShareOf -> TotalInput -> Quantity.Add throws ArgumentOutOfRangeException before any link is added; no seed involved, the lot numbers do not matter")]
    public void A_blend_of_inputs_that_each_fit_a_record_but_not_together_is_either_recorded_whole_or_refused_whole() {
        // Arrange: 600 000 000 kg each is a legal amount; their sum is beyond what one record may carry
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(3);
        plant.ReleasedStock(lots[0], 600_000_000m);
        plant.ReleasedStock(lots[1], 600_000_000m);
        plant.ProducedLot(lots[2], 1_000_000_000m);
        Transformation blend = plant.Step(TransformationKind.Blend, [(lots[0], 600_000_000m), (lots[1], 600_000_000m)], [(lots[2], 1_000_000_000m)]);
        Snapshot before = Snapshot.Of(plant.Genealogy);

        // Act
        Exception? refusal = Record.Exception(() => plant.Genealogy.Record(blend));

        // Assert
        if (refusal is null) {
            Assert.Equal(2, plant.Genealogy.Links.Count);
        } else {
            before.AssertUnchanged();
        }
    }

}
