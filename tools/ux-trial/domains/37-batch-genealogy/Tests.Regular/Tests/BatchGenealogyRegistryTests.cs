namespace BatchGenealogy.Domain.Tests;

[Reproducible]
public sealed class BatchGenealogyRegistryTests {

    [Fact]
    public void A_site_is_known_by_its_four_letter_code_and_starts_empty() {
        // Arrange
        string anySiteCode = Any.StringMatching("[A-Z]{4}").Generate();

        // Act
        BatchGenealogy genealogy = new BatchGenealogy(anySiteCode);

        // Assert
        Assert.Equal(anySiteCode, genealogy.SiteCode);
        Assert.Empty(genealogy.Lots);
        Assert.Empty(genealogy.Transformations);
        Assert.Empty(genealogy.Links);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("ABCDE")]
    [InlineData("abcd")]
    [InlineData("AB1D")]
    [InlineData("AB D")]
    [InlineData("")]
    [InlineData("ÉCOL")]
    public void A_site_code_other_than_four_upper_case_letters_is_refused(string siteCode) {
        Assert.Throws<FormatException>(() => new BatchGenealogy(siteCode));
    }

    [Fact]
    public void A_missing_site_code_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new BatchGenealogy(null!));
    }

    [Fact]
    public void A_registered_lot_can_be_found_by_its_number() {
        // Arrange
        BatchGenealogy genealogy = Any.BatchGenealogy().Generate();
        Lot            anyLot    = Any.Lot().Generate();

        // Act
        genealogy.Register(anyLot);

        // Assert
        Assert.Same(anyLot, genealogy.Get(anyLot.Number));
        Assert.Same(anyLot, genealogy.Get(new LotNumber(anyLot.Number.Value)));
        Assert.Single(genealogy.Lots);
    }

    [Fact]
    public void A_lot_number_can_only_be_registered_once() {
        // Arrange
        BatchGenealogy genealogy = Any.BatchGenealogy().Generate();
        Lot            anyLot    = Any.Lot().Generate();
        Lot            sameNumber = Any.Lot().WithNumber(anyLot.Number).Generate();
        genealogy.Register(anyLot);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => genealogy.Register(sameNumber));
        Assert.Same(anyLot, genealogy.Get(anyLot.Number));
    }

    [Fact]
    public void Registering_the_same_lot_object_twice_is_refused_too() {
        // Arrange
        BatchGenealogy genealogy = Any.BatchGenealogy().Generate();
        Lot            anyLot    = Any.Lot().Generate();
        genealogy.Register(anyLot);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => genealogy.Register(anyLot));
    }

    [Fact]
    public void A_missing_lot_cannot_be_registered() {
        // Arrange
        BatchGenealogy genealogy = Any.BatchGenealogy().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => genealogy.Register(null!));
    }

    [Fact]
    public void An_unknown_lot_cannot_be_fetched() {
        // Arrange
        BatchGenealogy genealogy = Any.BatchGenealogy().Generate();
        LotNumber      anyUnknown = Any.LotNumber().Generate();

        // Act / Assert
        Assert.Throws<KeyNotFoundException>(() => genealogy.Get(anyUnknown));
        Assert.Throws<ArgumentNullException>(() => genealogy.Get(null!));
    }

    [Fact]
    public void A_lot_and_its_sub_lot_are_two_different_registrations() {
        // Arrange
        BatchGenealogy genealogy = Any.BatchGenealogy().Generate();
        Lot            parent    = Any.Lot().WithNumber(n => n.WhollyLot()).Generate();
        Lot            child     = Any.Lot().WithNumber(parent.Number.SubLot(1)).Generate();

        // Act
        genealogy.Register(parent);
        genealogy.Register(child);

        // Assert
        Assert.Equal(2, genealogy.Lots.Count);
    }

}
