namespace BatchGenealogy.Tests.Expert.Tests;

public sealed class ContainmentTests {

    [Fact]
    public void Containing_a_non_conforming_material_rejects_it_and_quarantines_the_lots_it_reached() {
        // Arrange
        Plant     plant     = new();
        LotNumber anyApi    = plant.ReleasedRawMaterial(enoughFor: Kg(25m));
        LotNumber anyFiller = plant.ReleasedRawMaterial(enoughFor: Kg(75m));
        LotNumber blend     = plant.Blend(Kg(100m), Kg(25m).Of(anyApi), Kg(75m).Of(anyFiller));
        LotNumber tablets   = plant.Process(Kg(95m), Kg(100m).Of(blend));

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(anyApi, minimumShare: 0.01m);

        // Assert
        Assert.Equal([blend, tablets], quarantined);
        Assert.Equal(LotStatus.Rejected, plant[anyApi].Status);
        Assert.Equal(LotStatus.Quarantined, plant[tablets].Status);
        Assert.Equal(LotStatus.Released, plant[anyFiller].Status);
    }

    [Fact]
    public void A_lot_carrying_less_of_the_material_than_the_threshold_stays_released() {
        // Arrange
        Plant     plant      = new();
        LotNumber anyApi     = plant.ReleasedRawMaterial(enoughFor: Kg(20m));
        LotNumber anyFiller  = plant.ReleasedRawMaterial(enoughFor: Kg(190m));
        LotNumber rich       = plant.Blend(Kg(100m), Kg(10m).Of(anyApi), Kg(90m).Of(anyFiller));
        LotNumber trace      = plant.Blend(Kg(100m), Kg(1m).Of(anyApi), Kg(99m).Of(anyFiller));

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(anyApi, minimumShare: 0.05m);

        // Assert
        Assert.Equal([rich], quarantined);
        Assert.Equal(LotStatus.Released, plant[trace].Status);
    }

    [Fact]
    public void A_lot_carrying_exactly_the_threshold_is_quarantined() {
        // Arrange
        Plant     plant     = new();
        LotNumber anyApi    = plant.ReleasedRawMaterial(enoughFor: Kg(5m));
        LotNumber anyFiller = plant.ReleasedRawMaterial(enoughFor: Kg(95m));
        LotNumber blend     = plant.Blend(Kg(100m), Kg(5m).Of(anyApi), Kg(95m).Of(anyFiller));

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(anyApi, minimumShare: 0.05m);

        // Assert
        Assert.Equal([blend], quarantined);
    }

    [Fact]
    public void A_descendant_already_rejected_stays_rejected_and_is_not_reported_as_quarantined() {
        // Arrange
        Plant     plant     = new();
        LotNumber anyApi    = plant.ReleasedRawMaterial(enoughFor: Kg(50m));
        LotNumber anyFiller = plant.ReleasedRawMaterial(enoughFor: Kg(50m));
        LotNumber blend     = plant.Blend(Kg(100m), Kg(50m).Of(anyApi), Kg(50m).Of(anyFiller));
        plant[blend].Reject();

        // Act
        IReadOnlyList<LotNumber> quarantined = plant.Genealogy.Contain(anyApi, minimumShare: 0.01m);

        // Assert
        Assert.Empty(quarantined);
        Assert.Equal(LotStatus.Rejected, plant[blend].Status);
    }

}
