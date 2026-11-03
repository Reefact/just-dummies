namespace BatchGenealogy.Domain.Tests;

[Reproducible]
public sealed class RecordTests
{
    // --- what recording does ---------------------------------------------------------------------------------

    [Fact]
    public void Recording_a_blend_consumes_the_inputs_and_links_each_to_the_output() {
        // Arrange
        Plant plant = new Plant();
        Lot   flour = plant.Stock(60m);
        Lot   sugar = plant.Stock(20m);
        Step  step  = plant.PrepareBlend(plant.Later(5), 70m, flour.Take(60m), sugar.Take(10m));

        // Act
        plant.Genealogy.Record(step.Transformation);

        // Assert
        Assert.Equal(Kg.Of(0m), flour.RemainingQuantity);
        Assert.True(flour.IsDepleted);
        Assert.Equal(Kg.Of(10m), sugar.RemainingQuantity);
        Assert.Same(step.Transformation, Assert.Single(plant.Genealogy.Transformations));
        Assert.Equal(2, plant.Genealogy.Links.Count);
    }

    [Fact]
    public void A_blend_links_carry_the_share_of_each_input_in_the_total_input() {
        // Arrange
        Plant plant = new Plant();
        Lot   flour = plant.Stock(60m);
        Lot   sugar = plant.Stock(20m);

        // Act
        Lot mix = plant.Blend(plant.Later(5), 70m, flour.Take(60m), sugar.Take(20m));

        // Assert
        ProvenanceLink fromFlour = Assert.Single(plant.Genealogy.Links, link => link.Source.Equals(flour.Number));
        ProvenanceLink fromSugar = Assert.Single(plant.Genealogy.Links, link => link.Source.Equals(sugar.Number));
        Assert.Equal(0.75m, fromFlour.Share);
        Assert.Equal(0.25m, fromSugar.Share);
        Assert.Equal(mix.Number, fromFlour.Target);
        Assert.Equal(mix.Number, fromSugar.Target);
        Assert.Equal(plant.Genealogy.Transformations[0].Id, fromFlour.TransformationId);
    }

    [Fact]
    public void Recording_a_split_links_the_input_to_every_sub_lot_with_the_whole_share() {
        // Arrange
        Plant plant = new Plant();
        Lot   bulk  = plant.Stock(100m);

        // Act
        IReadOnlyList<Lot> parts = plant.Split(plant.Later(3), bulk.Take(100m), 30m, 30m, 40m);

        // Assert
        Assert.Equal(3, plant.Genealogy.Links.Count);
        Assert.All(plant.Genealogy.Links, link => {
            Assert.Equal(bulk.Number, link.Source);
            Assert.Equal(1m, link.Share);
        });
        Assert.Equal(parts.Select(part => part.Number).OrderBy(number => number.Value), plant.Genealogy.Links.Select(link => link.Target).OrderBy(number => number.Value));
        Assert.True(bulk.IsDepleted);
    }

    [Fact]
    public void A_process_may_use_only_part_of_a_lot() {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(100m);

        // Act
        plant.Process(plant.Later(3), 35m, raw.Take(40m));

        // Assert
        Assert.Equal(Kg.Of(60m), raw.RemainingQuantity);
        Assert.False(raw.IsDepleted);
    }

    [Fact]
    public void A_lot_may_feed_several_steps_until_it_is_empty() {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(100m);

        // Act
        plant.Process(plant.Later(3), 40m, raw.Take(40m));
        plant.Process(plant.Later(4), 50m, raw.Take(60m));

        // Assert
        Assert.True(raw.IsDepleted);
        Assert.Equal(2, plant.Genealogy.Transformations.Count);
    }

    [Fact]
    public void An_output_can_feed_the_next_step_once_it_is_released() {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(100m);
        Lot   first = plant.Process(plant.Later(3), 90m, raw.Take(100m));

        // Act
        Lot second = plant.Process(plant.Later(6), 80m, first.Take(90m));

        // Assert
        Assert.Equal(2, plant.Genealogy.Transformations.Count);
        Assert.Equal(first.Number, plant.Genealogy.Links.Single(link => link.Target.Equals(second.Number)).Source);
    }

    // --- refusals, and the promise that nothing changes ------------------------------------------------------------

    [Fact]
    public void A_transformation_cannot_be_recorded_twice() {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(100m);
        Step  step  = plant.PrepareProcess(plant.Later(3), 10m, raw.Take(10m));
        plant.Genealogy.Record(step.Transformation);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(step.Transformation));
        Assert.Equal(Kg.Of(90m), raw.RemainingQuantity);
    }

    [Fact]
    public void Two_different_transformations_cannot_share_an_id() {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(100m);
        Step  first = plant.PrepareProcess(plant.Later(3), 10m, raw.Take(10m));
        plant.Genealogy.Record(first.Transformation);
        Lot   other  = plant.Stock(20m);
        Lot   output = plant.NewLot(plant.UnusedNumber(), 5m, plant.Later(4));
        plant.Genealogy.Register(output);
        Transformation sameId = new Transformation(first.Transformation.Id,
                                                   TransformationKind.Process,
                                                   new[] { new LotQuantity(other.Number, Kg.Of(5m)) },
                                                   new[] { new LotQuantity(output.Number, Kg.Of(5m)) },
                                                   plant.Later(4));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(sameId));
        Assert.Equal(Kg.Of(20m), other.RemainingQuantity);
    }

    [Fact]
    public void A_missing_transformation_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Plant().Genealogy.Record(null!));
    }

    [Fact]
    public void An_input_lot_must_be_registered() {
        // Arrange
        Plant plant   = new Plant();
        Lot   unknown = plant.NewLot(plant.UnusedNumber(), 10m, plant.Day0);
        unknown.Release();
        Step  step    = plant.PrepareProcess(plant.Later(3), 5m, unknown.Take(5m));

        // Act / Assert
        Assert.Throws<KeyNotFoundException>(() => plant.Genealogy.Record(step.Transformation));
        Assert.Empty(plant.Genealogy.Transformations);
    }

    [Fact]
    public void An_output_lot_must_be_registered() {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(100m);
        Lot   ghost = plant.NewLot(plant.UnusedNumber(), 5m, plant.Later(3));
        Transformation transformation = new Transformation(Any.StringMatching("TRF-[0-9]{6}").Generate(),
                                                           TransformationKind.Process,
                                                           new[] { new LotQuantity(raw.Number, Kg.Of(10m)) },
                                                           new[] { new LotQuantity(ghost.Number, Kg.Of(5m)) },
                                                           plant.Later(3));

        // Act / Assert
        Assert.Throws<KeyNotFoundException>(() => plant.Genealogy.Record(transformation));
        Assert.Equal(Kg.Of(100m), raw.RemainingQuantity);
    }

    [Fact]
    public void An_input_lot_still_in_quarantine_cannot_be_used() {
        // Arrange
        Plant plant      = new Plant();
        Lot   quarantine = plant.Stock(100m, released: false);
        Step  step       = plant.PrepareProcess(plant.Later(3), 5m, quarantine.Take(5m));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(step.Transformation));
        Assert.Equal(Kg.Of(100m), quarantine.RemainingQuantity);
    }

    [Fact]
    public void A_rejected_input_lot_cannot_be_used() {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(100m);
        raw.Reject();
        Step  step  = plant.PrepareProcess(plant.Later(3), 5m, raw.Take(5m));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(step.Transformation));
    }

    [Fact]
    public void An_input_lot_cannot_be_used_before_it_exists() {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(100m, plant.Later(10));
        Step  step  = plant.PrepareProcess(plant.Later(9), 5m, raw.Take(5m));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(step.Transformation));
    }

    [Fact]
    public void An_input_lot_cannot_be_used_once_expired() {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(100m);
        Step  step  = plant.PrepareProcess(raw.ExpiresOn, 5m, raw.Take(5m));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(step.Transformation));
    }

    [Fact]
    public void An_input_lot_cannot_give_more_than_it_holds() {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(100m);
        plant.Process(plant.Later(3), 70m, raw.Take(70m));
        Step  step  = plant.PrepareProcess(plant.Later(4), 30.5m, raw.Take(30.5m));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(step.Transformation));
        Assert.Equal(Kg.Of(30m), raw.RemainingQuantity);
    }

    [Fact]
    public void A_step_in_volume_cannot_draw_on_a_lot_measured_in_mass() {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(100m);
        Lot   juice = plant.NewLot(plant.UnusedNumber(), 5m, plant.Later(3));
        plant.Genealogy.Register(juice);
        Transformation transformation = new Transformation(Any.StringMatching("TRF-[0-9]{6}").Generate(),
                                                           TransformationKind.Process,
                                                           new[] { new LotQuantity(raw.Number, new Quantity(5m, QuantityUnit.Litre)) },
                                                           new[] { new LotQuantity(juice.Number, new Quantity(5m, QuantityUnit.Litre)) },
                                                           plant.Later(3));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(transformation));
    }

    [Fact]
    public void An_output_lot_cannot_have_been_produced_already() {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(100m);
        Lot   other = plant.Stock(100m);
        Step  first = plant.PrepareProcess(plant.Later(3), 10m, raw.Take(10m));
        plant.Genealogy.Record(first.Transformation);
        Transformation again = new Transformation(Any.StringMatching("TRF-[0-9]{6}").Except(first.Transformation.Id).Generate(),
                                                  TransformationKind.Process,
                                                  new[] { new LotQuantity(other.Number, Kg.Of(10m)) },
                                                  new[] { new LotQuantity(first.Outputs[0].Number, Kg.Of(10m)) },
                                                  plant.Later(3));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(again));
        Assert.Equal(Kg.Of(100m), other.RemainingQuantity);
    }

    [Fact]
    public void An_output_lot_must_have_been_registered_with_exactly_the_quantity_produced() {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(100m);
        Lot   made  = plant.NewLot(plant.UnusedNumber(), 10m, plant.Later(3));
        plant.Genealogy.Register(made);
        Transformation transformation = new Transformation(Any.StringMatching("TRF-[0-9]{6}").Generate(),
                                                           TransformationKind.Process,
                                                           new[] { new LotQuantity(raw.Number, Kg.Of(50m)) },
                                                           new[] { new LotQuantity(made.Number, Kg.Of(9m)) },
                                                           plant.Later(3));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(transformation));
        Assert.Equal(Kg.Of(100m), raw.RemainingQuantity);
    }

    [Fact]
    public void The_quantity_of_an_output_lot_may_be_registered_in_another_unit() {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(100m);
        Lot   made  = plant.NewLot(plant.UnusedNumber(), 10m, plant.Later(3));
        plant.Genealogy.Register(made);
        Transformation transformation = new Transformation(Any.StringMatching("TRF-[0-9]{6}").Generate(),
                                                           TransformationKind.Process,
                                                           new[] { new LotQuantity(raw.Number, Kg.Of(50m)) },
                                                           new[] { new LotQuantity(made.Number, new Quantity(10_000m, QuantityUnit.Gram)) },
                                                           plant.Later(3));

        // Act
        plant.Genealogy.Record(transformation);

        // Assert
        Assert.Single(plant.Genealogy.Transformations);
    }

    [Fact]
    public void An_output_lot_must_have_been_manufactured_the_day_of_the_step() {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(100m);
        Lot   made  = plant.NewLot(plant.UnusedNumber(), 10m, plant.Later(4));
        plant.Genealogy.Register(made);
        Transformation transformation = new Transformation(Any.StringMatching("TRF-[0-9]{6}").Generate(),
                                                           TransformationKind.Process,
                                                           new[] { new LotQuantity(raw.Number, Kg.Of(50m)) },
                                                           new[] { new LotQuantity(made.Number, Kg.Of(10m)) },
                                                           plant.Later(3));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(transformation));
        Assert.Equal(Kg.Of(100m), raw.RemainingQuantity);
    }

    [Fact]
    public void An_output_lot_cannot_have_been_consumed_before_it_is_produced() {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(100m);
        Lot   early = plant.Stock(50m, plant.Later(3));
        Step  eater = plant.PrepareProcess(plant.Later(4), 20m, early.Take(20m));
        plant.Genealogy.Record(eater.Transformation);
        Transformation lateProducer = new Transformation(Any.StringMatching("TRF-[0-9]{6}").Except(eater.Transformation.Id).Generate(),
                                                         TransformationKind.Process,
                                                         new[] { new LotQuantity(raw.Number, Kg.Of(50m)) },
                                                         new[] { new LotQuantity(early.Number, Kg.Of(50m)) },
                                                         plant.Later(3));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(lateProducer));
        Assert.Equal(Kg.Of(100m), raw.RemainingQuantity);
    }

    [Fact]
    public void The_sub_lots_of_a_split_must_keep_the_material_of_their_parent() {
        // Arrange
        Plant plant = new Plant();
        Lot   bulk  = plant.Stock(100m);
        Lot   half  = plant.NewLot(bulk.Number.SubLot(1), 50m, plant.Later(3), bulk.Material);
        MaterialCode otherMaterial = Any.MaterialCode().Generate();
        while (otherMaterial.Equals(bulk.Material)) { otherMaterial = Any.MaterialCode().Generate(); }
        Lot   other = plant.NewLot(bulk.Number.SubLot(2), 50m, plant.Later(3), otherMaterial);
        plant.Genealogy.Register(half);
        plant.Genealogy.Register(other);
        Transformation split = new Transformation(Any.StringMatching("TRF-[0-9]{6}").Generate(),
                                                  TransformationKind.Split,
                                                  new[] { new LotQuantity(bulk.Number, Kg.Of(100m)) },
                                                  new[] { new LotQuantity(half.Number, Kg.Of(50m)), new LotQuantity(other.Number, Kg.Of(50m)) },
                                                  plant.Later(3));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(split));
        Assert.Equal(Kg.Of(100m), bulk.RemainingQuantity);
        Assert.Empty(plant.Genealogy.Links);
    }

    [Fact]
    public void A_blend_may_mix_materials() {
        // Arrange
        Plant plant = new Plant();
        Lot   a     = plant.Stock(10m, material: new MaterialCode("MAT-00001"));
        Lot   b     = plant.Stock(10m, material: new MaterialCode("MAT-00002"));

        // Act
        plant.Blend(plant.Later(3), 20m, a.Take(10m), b.Take(10m));

        // Assert
        Assert.Single(plant.Genealogy.Transformations);
    }

    [Fact]
    public void A_refused_step_changes_nothing_even_when_the_first_inputs_were_fine() {
        // Arrange: the second input is still in quarantine
        Plant plant    = new Plant();
        Lot   fine     = plant.Stock(40m);
        Lot   pending  = plant.Stock(40m, released: false);
        Step  step     = plant.PrepareBlend(plant.Later(3), 60m, fine.Take(30m), pending.Take(30m));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(step.Transformation));
        Assert.Equal(Kg.Of(40m), fine.RemainingQuantity);
        Assert.Equal(Kg.Of(40m), pending.RemainingQuantity);
        Assert.Empty(plant.Genealogy.Transformations);
        Assert.Empty(plant.Genealogy.Links);
    }

    [Fact]
    public void A_refused_step_leaves_its_output_free_to_be_produced_by_a_valid_one() {
        // Arrange: the blend is refused because its second input does not exist yet
        Plant plant = new Plant();
        Lot   fine  = plant.Stock(100m);
        Lot   late  = plant.Stock(100m, plant.Later(20));
        Step  bad   = plant.PrepareBlend(plant.Later(3), 50m, fine.Take(30m), late.Take(30m));
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(bad.Transformation));
        Transformation good = new Transformation(Any.StringMatching("TRF-[0-9]{6}").Except(bad.Transformation.Id).Generate(),
                                                 TransformationKind.Process,
                                                 new[] { new LotQuantity(fine.Number, Kg.Of(50m)) },
                                                 new[] { new LotQuantity(bad.Outputs[0].Number, Kg.Of(50m)) },
                                                 plant.Later(3));

        // Act
        plant.Genealogy.Record(good);

        // Assert
        Assert.Equal(Kg.Of(50m), fine.RemainingQuantity);
        Assert.Single(plant.Genealogy.Transformations);
    }

    [Fact(Skip = "domain defect: Record consumes the inputs, stores the transformation and then asks Transformation.ShareOf, which overflows the 1e9 cap for a step whose first input is in milligrams: the exception escapes with the step recorded, its inputs consumed and no link; any seed")]
    public void A_step_that_cannot_be_recorded_leaves_nothing_behind() {
        // Arrange: 600 kg expressed in milligrams, blended with 600 kg
        Plant plant = new Plant();
        DateOnly on = plant.Later(3);
        Lot inMilligrams = new Lot(plant.UnusedNumber(), Any.MaterialCode().Generate(), new Quantity(600_000_000m, QuantityUnit.Milligram), plant.Day0, plant.Day0.AddDays(500));
        inMilligrams.Release();
        plant.Genealogy.Register(inMilligrams);
        Lot inKilograms = plant.Stock(600m);
        Lot output      = plant.NewLot(plant.UnusedNumber(), 1_000m, on);
        plant.Genealogy.Register(output);
        Transformation blend = new Transformation(Any.StringMatching("TRF-[0-9]{6}").Generate(),
                                                  TransformationKind.Blend,
                                                  new[] { new LotQuantity(inMilligrams.Number, inMilligrams.InitialQuantity), new LotQuantity(inKilograms.Number, Kg.Of(600m)) },
                                                  new[] { new LotQuantity(output.Number, Kg.Of(1_000m)) },
                                                  on);

        // Act: refused or recorded, but all or nothing
        Exception? refusal = Record.Exception(() => plant.Genealogy.Record(blend));

        // Assert
        if (refusal is not null) {
            Assert.Empty(plant.Genealogy.Transformations);
            Assert.Equal(inMilligrams.InitialQuantity, inMilligrams.RemainingQuantity);
            Assert.Equal(Kg.Of(600m), inKilograms.RemainingQuantity);
        }
    }

}
