using JustDummies;

using Xunit;

namespace BatchGenealogy.Domain.Tests.Aggregate;

public sealed class ContainTests {

    // A graph of a bad API lot: one blend where it is 40 %, one where it is 10 %, and one final lot made
    // from the first blend alone. The thresholds the tests give are literals; the lot numbers are not.

    private sealed class Incident {

        public Incident() {
            Plant = new Plant();
            List<LotNumber> lots = Plant.DistinctLotNumbers(7);
            Bad = lots[0];
            Good1 = lots[1];
            Good2 = lots[2];
            HeavyBlend = lots[3];
            LightBlend = lots[4];
            Finished = lots[5];
            Unrelated = lots[6];
            Plant.ReleasedStock(Bad, 100m);
            Plant.ReleasedStock(Good1, 100m);
            Plant.ReleasedStock(Good2, 100m);
            Plant.ReleasedStock(Unrelated, 100m);
            Plant.Blend(HeavyBlend, 100m, (Bad, 40m), (Good1, 60m));   // 40 % of the bad lot
            Plant.Blend(LightBlend, 100m, (Bad, 10m), (Good2, 90m));   // 10 % of the bad lot
            Plant.Process(Finished, 50m, HeavyBlend, 50m);             // 40 % of the bad lot, a step further
        }

        public Plant Plant { get; }
        public LotNumber Bad { get; }
        public LotNumber Good1 { get; }
        public LotNumber Good2 { get; }
        public LotNumber HeavyBlend { get; }
        public LotNumber LightBlend { get; }
        public LotNumber Finished { get; }
        public LotNumber Unrelated { get; }

        public LotStatus StatusOf(LotNumber lot) => Plant.Genealogy.Get(lot).Status;

    }

    [Fact]
    public void Containing_a_material_rejects_it() {
        // Arrange
        Incident incident = new Incident();

        // Act
        incident.Plant.Genealogy.Contain(incident.Bad, 0.2m);

        // Assert
        Assert.Equal(LotStatus.Rejected, incident.StatusOf(incident.Bad));
    }

    [Fact]
    public void Containing_a_material_quarantines_the_descendants_that_carry_at_least_the_threshold_and_returns_them_nearest_first() {
        // Arrange
        Incident incident = new Incident();

        // Act
        IReadOnlyList<LotNumber> quarantined = incident.Plant.Genealogy.Contain(incident.Bad, 0.2m);

        // Assert
        Assert.Equal([incident.HeavyBlend, incident.Finished], quarantined);
        Assert.Equal(LotStatus.Quarantined, incident.StatusOf(incident.HeavyBlend));
        Assert.Equal(LotStatus.Quarantined, incident.StatusOf(incident.Finished));
    }

    [Fact]
    public void Descendants_below_the_threshold_are_left_released() {
        // Arrange
        Incident incident = new Incident();

        // Act
        incident.Plant.Genealogy.Contain(incident.Bad, 0.2m);

        // Assert
        Assert.Equal(LotStatus.Released, incident.StatusOf(incident.LightBlend));
    }

    [Fact]
    public void Lots_that_are_not_descendants_of_the_material_are_left_alone() {
        // Arrange
        Incident incident = new Incident();

        // Act
        incident.Plant.Genealogy.Contain(incident.Bad, 0.0000001m);

        // Assert
        Assert.Equal(LotStatus.Released, incident.StatusOf(incident.Good1));
        Assert.Equal(LotStatus.Released, incident.StatusOf(incident.Good2));
        Assert.Equal(LotStatus.Released, incident.StatusOf(incident.Unrelated));
    }

    [Fact]
    public void A_descendant_carrying_exactly_the_threshold_is_quarantined() {
        // Arrange
        Incident incident = new Incident();

        // Act
        IReadOnlyList<LotNumber> quarantined = incident.Plant.Genealogy.Contain(incident.Bad, 0.4m);

        // Assert
        Assert.Equal([incident.HeavyBlend, incident.Finished], quarantined);
    }

    [Fact]
    public void A_descendant_carrying_a_hair_less_than_the_threshold_is_not_quarantined() {
        // Arrange
        Incident incident = new Incident();

        // Act
        IReadOnlyList<LotNumber> quarantined = incident.Plant.Genealogy.Contain(incident.Bad, 0.4000000000000000000000000001m);

        // Assert
        Assert.Empty(quarantined);
    }

    [Fact]
    public void The_lowest_threshold_reaches_every_descendant() {
        // Arrange
        Incident incident = new Incident();

        // Act
        IReadOnlyList<LotNumber> quarantined = incident.Plant.Genealogy.Contain(incident.Bad, 0.0000000000000000000000000001m);

        // Assert
        Assert.Equal(new HashSet<LotNumber> { incident.HeavyBlend, incident.LightBlend, incident.Finished }, quarantined.ToHashSet());
    }

    [Fact]
    public void A_threshold_of_one_reaches_only_the_lots_made_entirely_of_the_material() {
        // Arrange
        Plant     plant = new Plant();
        LotNumber anyBad = Any.LotNumber().Base().Generate();
        LotNumber anyOther = Any.LotNumber().Base().DifferentFrom(anyBad).Generate();
        plant.ReleasedStock(anyBad, 100m);
        plant.ReleasedStock(anyOther, 100m);
        plant.Split(anyBad, 100m, 50m, 50m);
        LotNumber anyMixed = Any.LotNumber().Base().DifferentFrom(anyBad).DifferentFrom(anyOther).Generate();
        plant.Blend(anyMixed, 100m, (anyBad.SubLot(1), 50m), (anyOther, 50m));

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(anyBad, 1m);

        // Assert
        Assert.Equal([anyBad.SubLot(1), anyBad.SubLot(2)], quarantined);
        Assert.Equal(LotStatus.Released, plant.Genealogy.Get(anyMixed).Status);
    }

    [Fact]
    public void Shares_through_several_paths_are_added_before_the_threshold_is_applied() {
        // Arrange: each of the two sub-lots is only a third of the merge, but together they are two thirds
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(3);
        plant.ReleasedStock(lots[0], 100m);
        plant.ReleasedStock(lots[1], 100m);
        plant.Split(lots[0], 100m, 50m, 50m);
        plant.Blend(lots[2], 150m, (lots[0].SubLot(1), 50m), (lots[0].SubLot(2), 50m), (lots[1], 50m));

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(lots[0], 0.6m);

        // Assert
        Assert.Contains(lots[2], quarantined);
    }

    [Fact]
    public void A_descendant_that_is_already_rejected_stays_rejected_and_is_not_reported() {
        // Arrange
        Incident incident = new Incident();
        incident.Plant.Genealogy.Get(incident.HeavyBlend).Reject();

        // Act
        IReadOnlyList<LotNumber> quarantined = incident.Plant.Genealogy.Contain(incident.Bad, 0.2m);

        // Assert
        Assert.Equal([incident.Finished], quarantined);
        Assert.Equal(LotStatus.Rejected, incident.StatusOf(incident.HeavyBlend));
    }

    [Fact]
    public void A_descendant_that_is_already_in_quarantine_is_reported_with_the_others() {
        // Arrange
        Incident incident = new Incident();
        incident.Plant.Genealogy.Get(incident.Finished).Quarantine();

        // Act
        IReadOnlyList<LotNumber> quarantined = incident.Plant.Genealogy.Contain(incident.Bad, 0.2m);

        // Assert
        Assert.Equal([incident.HeavyBlend, incident.Finished], quarantined);
    }

    [Fact]
    public void A_depleted_descendant_is_quarantined_too_since_the_material_still_went_into_it() {
        // Arrange
        Plant           plant = new Plant();
        List<LotNumber> lots  = Plant.DistinctLotNumbers(3);
        plant.ReleasedStock(lots[0], 100m);
        plant.Process(lots[1], 90m, lots[0], 100m);
        plant.Process(lots[2], 80m, lots[1], 90m);
        Assert.True(plant.Genealogy.Get(lots[1]).IsDepleted);

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(lots[0], 1m);

        // Assert
        Assert.Equal([lots[1], lots[2]], quarantined);
        Assert.Equal(LotStatus.Quarantined, plant.Genealogy.Get(lots[1]).Status);
    }

    [Fact]
    public void The_ancestors_of_the_material_are_not_touched() {
        // Arrange
        Incident incident = new Incident();

        // Act
        incident.Plant.Genealogy.Contain(incident.HeavyBlend, 0.5m);

        // Assert
        Assert.Equal(LotStatus.Released, incident.StatusOf(incident.Bad));
        Assert.Equal(LotStatus.Released, incident.StatusOf(incident.Good1));
    }

    [Fact]
    public void A_material_with_no_descendant_is_simply_rejected() {
        // Arrange
        Incident incident = new Incident();

        // Act
        IReadOnlyList<LotNumber> quarantined = incident.Plant.Genealogy.Contain(incident.Unrelated, 0.5m);

        // Assert
        Assert.Empty(quarantined);
        Assert.Equal(LotStatus.Rejected, incident.StatusOf(incident.Unrelated));
    }

    [Fact]
    public void Containing_the_same_material_again_reports_the_same_lots() {
        // Arrange
        Incident incident = new Incident();
        IReadOnlyList<LotNumber> first = incident.Plant.Genealogy.Contain(incident.Bad, 0.2m);

        // Act
        IReadOnlyList<LotNumber> second = incident.Plant.Genealogy.Contain(incident.Bad, 0.2m);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(LotStatus.Rejected, incident.StatusOf(incident.Bad));
    }

    [Fact]
    public void A_lot_released_again_after_containment_can_be_consumed_again() {
        // Arrange
        Incident incident = new Incident();
        incident.Plant.Genealogy.Contain(incident.Bad, 0.2m);
        Lot finished = incident.Plant.Genealogy.Get(incident.Finished);

        // Act: quality clears the investigation
        finished.Release();

        // Assert
        Assert.Equal(LotStatus.Released, finished.Status);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.1")]
    [InlineData("1.0000000000000000000000000001")]
    [InlineData("2")]
    public void A_threshold_outside_zero_exclusive_to_one_inclusive_is_refused_and_nothing_is_rejected(string threshold) {
        // Arrange
        Incident incident = new Incident();
        Snapshot before   = Snapshot.Of(incident.Plant.Genealogy);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => incident.Plant.Genealogy.Contain(incident.Bad, decimal.Parse(threshold, System.Globalization.CultureInfo.InvariantCulture)));
        before.AssertUnchanged();
    }

    [Fact]
    public void Containing_a_material_that_is_unknown_is_refused_and_nothing_changes() {
        // Arrange
        Incident incident = new Incident();
        Snapshot before   = Snapshot.Of(incident.Plant.Genealogy);
        LotNumber anyUnknown = Any.LotNumber().Generate();

        // Act / Assert
        Assert.Throws<KeyNotFoundException>(() => incident.Plant.Genealogy.Contain(anyUnknown, 0.5m));
        Assert.Throws<ArgumentNullException>(() => incident.Plant.Genealogy.Contain(null!, 0.5m));
        before.AssertUnchanged();
    }

}
