using BatchGenealogy.Domain;

using JustDummies;

using Xunit;

using Genealogy = BatchGenealogy.Domain.BatchGenealogy;

namespace BatchGenealogy.Tests.Discovery;

public sealed class BatchGenealogyRecordTests {

    /// <summary>What a refused <c>Record</c> must leave exactly as it found it.</summary>
    private sealed class Snapshot {

        private readonly int        _transformations;
        private readonly int        _links;
        private readonly int        _lots;
        private readonly Lot[]      _watched;
        private readonly Quantity[] _remaining;
        private readonly LotStatus[] _statuses;

        public Snapshot(Genealogy genealogy, params Lot[] watched) {
            _transformations = genealogy.Transformations.Count;
            _links           = genealogy.Links.Count;
            _lots            = genealogy.Lots.Count;
            _watched         = watched;
            _remaining       = watched.Select(lot => lot.RemainingQuantity).ToArray();
            _statuses        = watched.Select(lot => lot.Status).ToArray();
        }

        public void AssertUnchanged(Genealogy genealogy) {
            Assert.Equal(_transformations, genealogy.Transformations.Count);
            Assert.Equal(_links, genealogy.Links.Count);
            Assert.Equal(_lots, genealogy.Lots.Count);
            for (int i = 0; i < _watched.Length; i++) {
                Assert.Equal(_remaining[i], _watched[i].RemainingQuantity);
                Assert.Equal(_statuses[i], _watched[i].Status);
            }
        }

    }

    // ---- the site ----------------------------------------------------------------------------------------

    [Fact]
    public void A_genealogy_starts_empty_and_keeps_its_site_code() {
        // Act
        Genealogy genealogy = new Genealogy("PARI");

        // Assert
        Assert.Equal("PARI", genealogy.SiteCode);
        Assert.Empty(genealogy.Lots);
        Assert.Empty(genealogy.Transformations);
        Assert.Empty(genealogy.Links);
    }

    [Fact]
    public void Any_four_upper_case_letters_make_a_site_code() {
        // Arrange
        string anySiteCode = Any.StringMatching("[A-Z]{4}").Generate();

        // Act
        Genealogy genealogy = new Genealogy(anySiteCode);

        // Assert
        Assert.Equal(anySiteCode, genealogy.SiteCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("ABCDE")]
    [InlineData("abcd")]
    [InlineData("AbCD")]
    [InlineData("AB1D")]
    [InlineData("AB D")]
    [InlineData("ÉCOL")]
    public void A_site_code_that_is_not_four_upper_case_letters_is_refused(string siteCode) {
        // Act
        Exception? thrown = Record.Exception(() => new Genealogy(siteCode));

        // Assert
        Assert.IsType<FormatException>(thrown);
    }

    [Fact]
    public void A_missing_site_code_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new Genealogy(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    // ---- registering and finding lots --------------------------------------------------------------------

    [Fact]
    public void A_registered_lot_can_be_found_by_its_number() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        Lot       anyLot    = Any.Lot().Generate();

        // Act
        genealogy.Register(anyLot);

        // Assert
        Assert.Same(anyLot, genealogy.Get(anyLot.Number));
        Assert.Single(genealogy.Lots);
    }

    [Fact]
    public void A_lot_is_found_by_the_value_of_its_number_not_by_the_instance() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        Lot       anyLot    = Any.Lot().Generate();
        genealogy.Register(anyLot);

        // Act
        Lot found = genealogy.Get(new LotNumber(anyLot.Number.Value));

        // Assert
        Assert.Same(anyLot, found);
    }

    [Fact]
    public void A_lot_number_cannot_be_registered_twice() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        Lot       anyLot    = Any.Lot().Generate();
        Lot       sameNumber = Any.Lot().WithNumber(new LotNumber(anyLot.Number.Value)).Generate();
        genealogy.Register(anyLot);

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Register(sameNumber));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Same(anyLot, genealogy.Get(anyLot.Number));
        Assert.Single(genealogy.Lots);
    }

    [Fact]
    public void Several_lots_can_be_registered() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        List<Lot> anyLots   = Any.ListOf(Any.Lot()).WithCountBetween(2, 6).Distinct().Generate();

        // Act
        foreach (Lot lot in anyLots) { genealogy.Register(lot); }

        // Assert
        Assert.Equal(anyLots.Count, genealogy.Lots.Count);
    }

    [Fact]
    public void Looking_for_an_unknown_lot_fails_with_a_missing_key() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        LotNumber anyNumber = Any.LotNumber().Generate();

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Get(anyNumber));

        // Assert
        Assert.IsType<KeyNotFoundException>(thrown);
    }

    [Fact]
    public void Registering_or_getting_nothing_is_refused() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();

        // Act
        Exception? register = Record.Exception(() => genealogy.Register(null!));
        Exception? get      = Record.Exception(() => genealogy.Get(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(register);
        Assert.IsType<ArgumentNullException>(get);
    }

    // ---- recording a transformation ----------------------------------------------------------------------

    [Fact]
    public void Recording_a_blend_consumes_its_inputs_and_links_the_output_to_each_of_them() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        DateOnly  anyDay    = Scenario.AnyDay();
        Lot       first     = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(1000m));
        Lot       second    = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(500m));
        Lot       product   = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(900m));
        Transformation blend = Scenario.Blend(anyDay, [Scenario.Part(first, Scenario.Grams(600m)), Scenario.Part(second, Scenario.Grams(400m))], Scenario.Part(product, Scenario.Grams(900m)));

        // Act
        genealogy.Record(blend);

        // Assert
        Assert.Same(blend, Assert.Single(genealogy.Transformations));
        Assert.Equal(Scenario.Grams(400m), first.RemainingQuantity);
        Assert.Equal(Scenario.Grams(100m), second.RemainingQuantity);
        Assert.Equal(2, genealogy.Links.Count);
        ProvenanceLink fromFirst  = Assert.Single(genealogy.Links, link => link.Source.Equals(first.Number));
        ProvenanceLink fromSecond = Assert.Single(genealogy.Links, link => link.Source.Equals(second.Number));
        Assert.Equal(product.Number, fromFirst.Target);
        Assert.Equal(0.6m, fromFirst.Share);
        Assert.Equal(product.Number, fromSecond.Target);
        Assert.Equal(0.4m, fromSecond.Share);
        Assert.Equal(blend.Id, fromFirst.TransformationId);
    }

    [Fact]
    public void Recording_a_split_links_every_sub_lot_to_the_whole_input() {
        // Arrange
        Genealogy    genealogy = Scenario.AnyGenealogy();
        DateOnly     anyDay    = Scenario.AnyDay();
        MaterialCode anyMaterial = Any.MaterialCode().Generate();
        Lot          source    = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(1000m), material: anyMaterial);
        Lot          first     = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(400m), source.Number.SubLot(1), anyMaterial);
        Lot          second    = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(500m), source.Number.SubLot(2), anyMaterial);

        // Act
        genealogy.Record(Scenario.Split(anyDay, Scenario.Part(source, Scenario.Grams(1000m)), [Scenario.Part(first, Scenario.Grams(400m)), Scenario.Part(second, Scenario.Grams(500m))]));

        // Assert
        Assert.True(source.IsDepleted);
        Assert.Equal(2, genealogy.Links.Count);
        Assert.All(genealogy.Links, link => {
            Assert.Equal(source.Number, link.Source);
            Assert.Equal(1m, link.Share);
        });
    }

    [Fact]
    public void Recording_a_process_with_a_loss_leaves_the_output_at_the_quantity_produced() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        DateOnly  anyDay    = Scenario.AnyDay();
        Lot       input     = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Litres(10m));
        Lot       output    = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Litres(8m));

        // Act
        genealogy.Record(Scenario.Process(anyDay, [Scenario.Part(input, Scenario.Litres(10m))], Scenario.Part(output, Scenario.Litres(8m))));

        // Assert
        Assert.True(input.IsDepleted);
        Assert.Equal(Scenario.Litres(8m), output.RemainingQuantity);
        Assert.Equal(LotStatus.Quarantined, output.Status);
    }

    [Fact]
    public void An_output_registered_in_another_unit_is_accepted_when_the_quantities_are_equal() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        DateOnly  anyDay    = Scenario.AnyDay();
        Lot       input     = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(5000m));
        Lot       output    = Scenario.ProducedLotOn(genealogy, anyDay, new Quantity(1m, QuantityUnit.Kilogram));

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Record(Scenario.Process(anyDay, [Scenario.Part(input, Scenario.Grams(3000m))], Scenario.Part(output, Scenario.Grams(1000m)))));

        // Assert
        Assert.Null(thrown);
    }

    [Fact]
    public void The_output_of_a_step_can_feed_the_next_one_once_released() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        DateOnly  firstDay  = Scenario.AnyDay();
        DateOnly  secondDay = Scenario.AnyDayAfter(firstDay);
        Lot       input     = Scenario.ReleasedLotOn(genealogy, firstDay, Scenario.Grams(1000m));
        Lot       middle    = Scenario.ProducedLotOn(genealogy, firstDay, Scenario.Grams(900m));
        Lot       last      = Scenario.ProducedLotOn(genealogy, secondDay, Scenario.Grams(800m));
        genealogy.Record(Scenario.Process(firstDay, [Scenario.Part(input, Scenario.Grams(1000m))], Scenario.Part(middle, Scenario.Grams(900m))));
        middle.Release();

        // Act
        genealogy.Record(Scenario.Process(secondDay, [Scenario.Part(middle, Scenario.Grams(900m))], Scenario.Part(last, Scenario.Grams(800m))));

        // Assert
        Assert.Equal(2, genealogy.Transformations.Count);
        Assert.Equal(2, genealogy.Links.Count);
        Assert.True(middle.IsDepleted);
    }

    [Fact]
    public void An_output_that_is_still_in_quarantine_cannot_feed_the_next_step() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        DateOnly  firstDay  = Scenario.AnyDay();
        DateOnly  secondDay = Scenario.AnyDayAfter(firstDay);
        Lot       input     = Scenario.ReleasedLotOn(genealogy, firstDay, Scenario.Grams(1000m));
        Lot       middle    = Scenario.ProducedLotOn(genealogy, firstDay, Scenario.Grams(900m));
        Lot       last      = Scenario.ProducedLotOn(genealogy, secondDay, Scenario.Grams(800m));
        genealogy.Record(Scenario.Process(firstDay, [Scenario.Part(input, Scenario.Grams(1000m))], Scenario.Part(middle, Scenario.Grams(900m))));
        Snapshot before = new Snapshot(genealogy, middle, last);

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Record(Scenario.Process(secondDay, [Scenario.Part(middle, Scenario.Grams(900m))], Scenario.Part(last, Scenario.Grams(800m)))));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        before.AssertUnchanged(genealogy);
    }

    // ---- recording: refusals leave nothing changed -------------------------------------------------------

    [Fact]
    public void A_transformation_id_cannot_be_recorded_twice() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        DateOnly  anyDay    = Scenario.AnyDay();
        string    anyId     = Scenario.AnyTransformationId();
        Lot       firstIn   = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(100m));
        Lot       firstOut  = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(90m));
        Lot       secondIn  = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(100m));
        Lot       secondOut = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(90m));
        genealogy.Record(new Transformation(anyId, TransformationKind.Process, [Scenario.Part(firstIn, Scenario.Grams(100m))], [Scenario.Part(firstOut, Scenario.Grams(90m))], anyDay));
        Snapshot before = new Snapshot(genealogy, secondIn, secondOut);

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Record(new Transformation(anyId, TransformationKind.Process, [Scenario.Part(secondIn, Scenario.Grams(100m))], [Scenario.Part(secondOut, Scenario.Grams(90m))], anyDay)));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        before.AssertUnchanged(genealogy);
    }

    [Fact]
    public void A_transformation_on_an_unregistered_input_is_refused() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        DateOnly  anyDay    = Scenario.AnyDay();
        Lot       unknown   = Any.Lot().Generate();
        Lot       output    = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(90m));
        Snapshot  before    = new Snapshot(genealogy, output);

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Record(Scenario.Process(anyDay, [Scenario.Part(unknown, Scenario.Grams(100m))], Scenario.Part(output, Scenario.Grams(90m)))));

        // Assert
        Assert.IsType<KeyNotFoundException>(thrown);
        before.AssertUnchanged(genealogy);
    }

    [Fact]
    public void A_transformation_on_an_unregistered_output_is_refused() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        DateOnly  anyDay    = Scenario.AnyDay();
        Lot       input     = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(100m));
        Lot       unknown   = Any.Lot().Generate();
        Snapshot  before    = new Snapshot(genealogy, input);

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Record(Scenario.Process(anyDay, [Scenario.Part(input, Scenario.Grams(100m))], Scenario.Part(unknown, Scenario.Grams(90m)))));

        // Assert
        Assert.IsType<KeyNotFoundException>(thrown);
        before.AssertUnchanged(genealogy);
    }

    [Fact]
    public void An_input_still_in_quarantine_cannot_be_consumed() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        DateOnly  anyDay    = Scenario.AnyDay();
        Lot       input     = Scenario.QuarantinedLotOn(genealogy, anyDay, Scenario.Grams(100m));
        Lot       output    = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(90m));
        Snapshot  before    = new Snapshot(genealogy, input, output);

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Record(Scenario.Process(anyDay, [Scenario.Part(input, Scenario.Grams(100m))], Scenario.Part(output, Scenario.Grams(90m)))));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        before.AssertUnchanged(genealogy);
    }

    [Fact]
    public void An_input_that_expired_by_the_date_of_the_step_cannot_be_consumed() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        DateOnly  anyDay    = Scenario.AnyDay();
        Lot       input     = Any.Lot().WithManufacturedOn(anyDay.AddDays(-30)).WithExpiresOn(anyDay).WithInitialQuantity(Scenario.Grams(100m)).Generate();
        input.Release();
        genealogy.Register(input);
        Lot      output = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(90m));
        Snapshot before = new Snapshot(genealogy, input, output);

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Record(Scenario.Process(anyDay, [Scenario.Part(input, Scenario.Grams(100m))], Scenario.Part(output, Scenario.Grams(90m)))));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        before.AssertUnchanged(genealogy);
    }

    [Fact]
    public void An_input_made_after_the_date_of_the_step_cannot_be_consumed() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        DateOnly  anyDay    = Scenario.AnyDay();
        Lot       input     = Any.Lot().WithManufacturedOn(anyDay.AddDays(1)).WithExpiresOn(anyDay.AddDays(200)).WithInitialQuantity(Scenario.Grams(100m)).Generate();
        input.Release();
        genealogy.Register(input);
        Lot      output = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(90m));
        Snapshot before = new Snapshot(genealogy, input, output);

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Record(Scenario.Process(anyDay, [Scenario.Part(input, Scenario.Grams(100m))], Scenario.Part(output, Scenario.Grams(90m)))));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        before.AssertUnchanged(genealogy);
    }

    [Fact]
    public void An_input_cannot_give_more_than_it_has_left() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        DateOnly  anyDay    = Scenario.AnyDay();
        Lot       input     = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(100m));
        Lot       output    = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(90m));
        Snapshot  before    = new Snapshot(genealogy, input, output);

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Record(Scenario.Process(anyDay, [Scenario.Part(input, Scenario.Grams(100.5m))], Scenario.Part(output, Scenario.Grams(90m)))));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        before.AssertUnchanged(genealogy);
    }

    [Fact]
    public void An_input_measured_in_volume_cannot_be_consumed_in_mass() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        DateOnly  anyDay    = Scenario.AnyDay();
        Lot       input     = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Litres(10m));
        Lot       output    = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(90m));
        Snapshot  before    = new Snapshot(genealogy, input, output);

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Record(Scenario.Process(anyDay, [Scenario.Part(input, Scenario.Grams(100m))], Scenario.Part(output, Scenario.Grams(90m)))));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        before.AssertUnchanged(genealogy);
    }

    [Fact]
    public void An_output_cannot_be_produced_twice() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        DateOnly  anyDay    = Scenario.AnyDay();
        Lot       firstIn   = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(100m));
        Lot       secondIn  = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(100m));
        Lot       output    = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(90m));
        genealogy.Record(Scenario.Process(anyDay, [Scenario.Part(firstIn, Scenario.Grams(100m))], Scenario.Part(output, Scenario.Grams(90m))));
        Snapshot before = new Snapshot(genealogy, secondIn, output);

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Record(Scenario.Process(anyDay, [Scenario.Part(secondIn, Scenario.Grams(100m))], Scenario.Part(output, Scenario.Grams(90m)))));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        before.AssertUnchanged(genealogy);
    }

    [Fact]
    public void An_output_must_have_been_registered_with_the_quantity_the_step_produces() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        DateOnly  anyDay    = Scenario.AnyDay();
        Lot       input     = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(100m));
        Lot       output    = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(80m));
        Snapshot  before    = new Snapshot(genealogy, input, output);

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Record(Scenario.Process(anyDay, [Scenario.Part(input, Scenario.Grams(100m))], Scenario.Part(output, Scenario.Grams(90m)))));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        before.AssertUnchanged(genealogy);
    }

    [Fact]
    public void An_output_must_have_been_manufactured_on_the_date_of_the_step() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        DateOnly  anyDay    = Scenario.AnyDay();
        Lot       input     = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(100m));
        Lot       output    = Scenario.ProducedLotOn(genealogy, anyDay.AddDays(1), Scenario.Grams(90m));
        Snapshot  before    = new Snapshot(genealogy, input, output);

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Record(Scenario.Process(anyDay, [Scenario.Part(input, Scenario.Grams(100m))], Scenario.Part(output, Scenario.Grams(90m)))));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        before.AssertUnchanged(genealogy);
    }

    [Fact]
    public void A_lot_already_consumed_cannot_be_produced_afterwards() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        DateOnly  anyDay    = Scenario.AnyDay();
        Lot       shared    = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(100m));
        Lot       firstOut  = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(40m));
        Lot       other     = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(100m));
        genealogy.Record(Scenario.Process(anyDay, [Scenario.Part(shared, Scenario.Grams(50m))], Scenario.Part(firstOut, Scenario.Grams(40m))));
        Snapshot before = new Snapshot(genealogy, shared, other);

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Record(Scenario.Process(anyDay, [Scenario.Part(other, Scenario.Grams(100m))], Scenario.Part(shared, Scenario.Grams(100m)))));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        before.AssertUnchanged(genealogy);
    }

    [Fact]
    public void A_split_must_keep_the_material_of_its_input() {
        // Arrange
        Genealogy    genealogy   = Scenario.AnyGenealogy();
        DateOnly     anyDay      = Scenario.AnyDay();
        MaterialCode anyMaterial = Any.MaterialCode().Generate();
        MaterialCode otherMaterial = Any.MaterialCode().WithValue(v => v.DifferentFrom(anyMaterial.Value)).Generate();
        Lot          source      = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(1000m), material: anyMaterial);
        Lot          kept        = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(400m), source.Number.SubLot(1), anyMaterial);
        Lot          changed     = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(500m), source.Number.SubLot(2), otherMaterial);
        Snapshot     before      = new Snapshot(genealogy, source, kept, changed);

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Record(Scenario.Split(anyDay, Scenario.Part(source, Scenario.Grams(1000m)), [Scenario.Part(kept, Scenario.Grams(400m)), Scenario.Part(changed, Scenario.Grams(500m))])));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        before.AssertUnchanged(genealogy);
    }

    [Fact]
    public void A_blend_may_change_the_material() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        DateOnly  anyDay    = Scenario.AnyDay();
        Lot       first     = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(100m));
        Lot       second    = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(100m));
        Lot       product   = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(190m));

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Record(Scenario.Blend(anyDay, [Scenario.Part(first, Scenario.Grams(100m)), Scenario.Part(second, Scenario.Grams(100m))], Scenario.Part(product, Scenario.Grams(190m)))));

        // Assert
        Assert.Null(thrown);
    }

    [Fact]
    public void When_the_second_input_is_short_the_first_one_is_not_consumed_either() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();
        DateOnly  anyDay    = Scenario.AnyDay();
        Lot       plenty    = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(1000m));
        Lot       scarce    = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(10m));
        Lot       product   = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(500m));
        Snapshot  before    = new Snapshot(genealogy, plenty, scarce, product);

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Record(Scenario.Blend(anyDay, [Scenario.Part(plenty, Scenario.Grams(400m)), Scenario.Part(scarce, Scenario.Grams(100m))], Scenario.Part(product, Scenario.Grams(500m)))));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        before.AssertUnchanged(genealogy);
    }

    [Fact]
    public void When_the_second_output_is_refused_nothing_has_been_consumed() {
        // Arrange
        Genealogy    genealogy   = Scenario.AnyGenealogy();
        DateOnly     anyDay      = Scenario.AnyDay();
        MaterialCode anyMaterial = Any.MaterialCode().Generate();
        Lot          source      = Scenario.ReleasedLotOn(genealogy, anyDay, Scenario.Grams(1000m), material: anyMaterial);
        Lot          first       = Scenario.ProducedLotOn(genealogy, anyDay, Scenario.Grams(400m), source.Number.SubLot(1), anyMaterial);
        Lot          wrongDate   = Scenario.ProducedLotOn(genealogy, anyDay.AddDays(2), Scenario.Grams(500m), source.Number.SubLot(2), anyMaterial);
        Snapshot     before      = new Snapshot(genealogy, source, first, wrongDate);

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Record(Scenario.Split(anyDay, Scenario.Part(source, Scenario.Grams(1000m)), [Scenario.Part(first, Scenario.Grams(400m)), Scenario.Part(wrongDate, Scenario.Grams(500m))])));

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        before.AssertUnchanged(genealogy);
    }

    [Fact]
    public void Recording_nothing_is_refused() {
        // Arrange
        Genealogy genealogy = Scenario.AnyGenealogy();

        // Act
        Exception? thrown = Record.Exception(() => genealogy.Record(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

}
