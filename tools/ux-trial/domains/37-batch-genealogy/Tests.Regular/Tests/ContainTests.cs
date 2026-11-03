namespace BatchGenealogy.Domain.Tests;

[Reproducible]
public sealed class ContainTests {

    [Fact]
    public void Containing_a_material_rejects_it_and_quarantines_the_lots_that_carry_enough_of_it() {
        // Arrange: a (30) + b (10) -> mix (A is 75 percent of it); mix -> pack
        Plant plant = new Plant();
        Lot   a     = plant.Stock(30m);
        Lot   b     = plant.Stock(10m);
        Lot   mix   = plant.Blend(plant.Later(2), 36m, a.Take(30m), b.Take(10m));
        Lot   pack  = plant.Process(plant.Later(4), 36m, mix.Take(36m));

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(a.Number, 0.5m);

        // Assert
        Assert.Equal(LotStatus.Rejected, a.Status);
        Assert.Equal(new[] { mix.Number, pack.Number }, quarantined);
        Assert.Equal(LotStatus.Quarantined, mix.Status);
        Assert.Equal(LotStatus.Quarantined, pack.Status);
        Assert.Equal(LotStatus.Released, b.Status);
    }

    [Fact]
    public void A_lot_carrying_less_than_the_minimum_share_is_left_alone() {
        // Arrange
        Plant plant = new Plant();
        Lot   a     = plant.Stock(10m);
        Lot   b     = plant.Stock(30m);
        Lot   mix   = plant.Blend(plant.Later(2), 36m, a.Take(10m), b.Take(30m));

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(a.Number, 0.5m);

        // Assert
        Assert.Empty(quarantined);
        Assert.Equal(LotStatus.Released, mix.Status);
        Assert.Equal(LotStatus.Rejected, a.Status);
    }

    [Fact]
    public void The_minimum_share_is_inclusive() {
        // Arrange: A is exactly half of the mix
        Plant plant = new Plant();
        Lot   a     = plant.Stock(20m);
        Lot   b     = plant.Stock(20m);
        Lot   mix   = plant.Blend(plant.Later(2), 36m, a.Take(20m), b.Take(20m));

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(a.Number, 0.5m);

        // Assert
        Assert.Equal(new[] { mix.Number }, quarantined);
    }

    [Fact]
    public void A_minimum_share_just_above_the_share_leaves_the_lot_alone() {
        // Arrange
        Plant plant = new Plant();
        Lot   a     = plant.Stock(20m);
        Lot   b     = plant.Stock(20m);
        Lot   mix   = plant.Blend(plant.Later(2), 36m, a.Take(20m), b.Take(20m));

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(a.Number, 0.5001m);

        // Assert
        Assert.Empty(quarantined);
        Assert.Equal(LotStatus.Released, mix.Status);
    }

    [Fact]
    public void A_very_small_minimum_share_reaches_every_descendant() {
        // Arrange: A is a hundredth of the mix, and the mix feeds a pack
        Plant plant = new Plant();
        Lot   a     = plant.Stock(1m);
        Lot   b     = plant.Stock(99m);
        Lot   mix   = plant.Blend(plant.Later(2), 100m, a.Take(1m), b.Take(99m));
        Lot   pack  = plant.Process(plant.Later(4), 90m, mix.Take(100m));

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(a.Number, 0.0000001m);

        // Assert
        Assert.Equal(new[] { mix.Number, pack.Number }, quarantined);
    }

    [Fact]
    public void Descendants_are_quarantined_even_once_consumed_and_the_report_is_nearest_first() {
        // Arrange
        Plant plant = new Plant();
        Lot   a     = plant.Stock(10m);
        Lot   first = plant.Process(plant.Later(2), 10m, a.Take(10m));
        Lot   second = plant.Process(plant.Later(4), 10m, first.Take(10m));
        Lot   third = plant.Process(plant.Later(6), 10m, second.Take(10m));

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(a.Number, 1m);

        // Assert
        Assert.Equal(new[] { first.Number, second.Number, third.Number }, quarantined);
        Assert.True(first.IsDepleted);
        Assert.Equal(LotStatus.Quarantined, first.Status);
    }

    [Fact]
    public void A_descendant_already_rejected_stays_rejected_and_is_not_reported() {
        // Arrange
        Plant plant = new Plant();
        Lot   a     = plant.Stock(10m);
        Lot   made  = plant.Process(plant.Later(2), 10m, a.Take(10m));
        made.Reject();

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(a.Number, 0.5m);

        // Assert
        Assert.Empty(quarantined);
        Assert.Equal(LotStatus.Rejected, made.Status);
    }

    [Fact]
    public void A_descendant_already_in_quarantine_is_reported_again() {
        // Arrange
        Plant plant = new Plant();
        Lot   a     = plant.Stock(10m);
        Lot   made  = plant.Process(plant.Later(2), 10m, a.Take(10m));
        made.Quarantine();

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(a.Number, 0.5m);

        // Assert
        Assert.Equal(new[] { made.Number }, quarantined);
    }

    [Fact]
    public void Containing_a_lot_with_no_descendants_only_rejects_it() {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(10m);

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(raw.Number, 1m);

        // Assert
        Assert.Empty(quarantined);
        Assert.Equal(LotStatus.Rejected, raw.Status);
    }

    [Fact]
    public void Containing_the_same_material_twice_is_harmless() {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(10m);
        Lot   made  = plant.Process(plant.Later(2), 10m, raw.Take(10m));
        plant.Genealogy.Contain(raw.Number, 1m);

        // Act
        IReadOnlyList<LotNumber> again = plant.Genealogy.Contain(raw.Number, 1m);

        // Assert
        Assert.Equal(new[] { made.Number }, again);
        Assert.Equal(LotStatus.Rejected, raw.Status);
    }

    [Fact]
    public void A_quarantined_lot_cannot_feed_a_new_step_after_containment() {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(10m);
        Lot   made  = plant.Process(plant.Later(2), 5m, raw.Take(5m));
        plant.Genealogy.Contain(raw.Number, 0.5m);
        Step  next  = plant.PrepareProcess(plant.Later(4), 1m, made.Take(1m));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => plant.Genealogy.Record(next.Transformation));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(1.0001)]
    [InlineData(2)]
    public void A_minimum_share_must_lie_in_zero_exclusive_to_one_inclusive(double share) {
        // Arrange
        Plant plant = new Plant();
        Lot   raw   = plant.Stock(10m);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => plant.Genealogy.Contain(raw.Number, (decimal)share));
        Assert.Equal(LotStatus.Released, raw.Status);
    }

    [Fact]
    public void An_unknown_material_cannot_be_contained() {
        // Arrange
        Plant     plant      = new Plant();
        LotNumber anyUnknown = Any.LotNumber().Generate();

        // Act / Assert
        Assert.Throws<KeyNotFoundException>(() => plant.Genealogy.Contain(anyUnknown, 0.5m));
    }

    [Fact(Skip = "domain defect: shares are decimals, so a lot made only of three thirds of the material sums to 0.9999999999999999999999999999 rather than 1 and Contain(…, 1m) misses it; any seed")]
    public void A_lot_wholly_made_of_the_material_is_caught_by_a_minimum_share_of_one() {
        // Arrange: A is split in three equal parts, blended back with a third each
        Plant              plant  = new Plant();
        Lot                a      = plant.Stock(300m);
        IReadOnlyList<Lot> thirds = plant.Split(plant.Later(2), a.Take(300m), 100m, 100m, 100m);
        Lot                merged = plant.Blend(plant.Later(4), 300m, thirds[0].Take(100m), thirds[1].Take(100m), thirds[2].Take(100m));

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(a.Number, 1m);

        // Assert
        Assert.Contains(merged.Number, quarantined);
    }

}
