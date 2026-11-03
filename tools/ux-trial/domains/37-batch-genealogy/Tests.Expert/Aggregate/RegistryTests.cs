using JustDummies;

using Xunit;

using Genealogy = BatchGenealogy.Domain.BatchGenealogy;

namespace BatchGenealogy.Domain.Tests.Aggregate;

public sealed class RegistryTests {

    // ---- the site ----------------------------------------------------------------------------------

    [Theory]
    [InlineData("AAAA")]
    [InlineData("ZZZZ")]
    [InlineData("LYON")]
    public void A_site_code_of_four_upper_case_letters_is_kept(string siteCode) {
        Assert.Equal(siteCode, new Genealogy(siteCode).SiteCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("LYO")]
    [InlineData("LYONS")]
    [InlineData("lyon")]
    [InlineData("Lyon")]
    [InlineData("LY0N")]
    [InlineData("LY N")]
    [InlineData(" LYO")]
    [InlineData("LYO-")]
    [InlineData("LYÓN")]
    public void Any_other_site_code_is_refused(string siteCode) {
        Assert.Throws<FormatException>(() => new Genealogy(siteCode));
    }

    [Fact]
    public void A_missing_site_code_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Genealogy(null!));
    }

    [Fact]
    public void A_new_genealogy_knows_no_lot_no_transformation_and_no_link() {
        // Act
        Genealogy genealogy = Any.BatchGenealogy().Generate();

        // Assert
        Assert.Empty(genealogy.Lots);
        Assert.Empty(genealogy.Transformations);
        Assert.Empty(genealogy.Links);
    }

    // ---- lots --------------------------------------------------------------------------------------

    [Fact]
    public void A_registered_lot_can_be_found_by_its_number() {
        // Arrange
        Genealogy genealogy = Any.BatchGenealogy().Generate();
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
        Genealogy genealogy = Any.BatchGenealogy().Generate();
        Lot       anyLot    = Any.Lot().Generate();
        genealogy.Register(anyLot);

        // Act
        Lot found = genealogy.Get(new LotNumber(anyLot.Number.Value));

        // Assert
        Assert.Same(anyLot, found);
    }

    [Fact]
    public void Lots_with_different_numbers_coexist() {
        // Arrange
        Genealogy       genealogy = Any.BatchGenealogy().Generate();
        List<LotNumber> numbers   = Plant.DistinctLotNumbers(3);

        // Act
        foreach (LotNumber number in numbers) { genealogy.Register(Any.Lot().WithNumber(number).Generate()); }

        // Assert
        Assert.Equal(3, genealogy.Lots.Count);
        Assert.All(numbers, number => Assert.Equal(number, genealogy.Get(number).Number));
    }

    [Fact]
    public void A_sub_lot_and_its_base_lot_are_two_lots() {
        // Arrange
        Genealogy genealogy = Any.BatchGenealogy().Generate();
        LotNumber baseLot   = Any.LotNumber().Base().Generate();

        // Act
        genealogy.Register(Any.Lot().WithNumber(baseLot).Generate());
        genealogy.Register(Any.Lot().WithNumber(baseLot.SubLot(1)).Generate());

        // Assert
        Assert.Equal(2, genealogy.Lots.Count);
    }

    [Fact]
    public void A_lot_number_cannot_be_registered_twice_and_the_first_lot_stays() {
        // Arrange
        Genealogy genealogy = Any.BatchGenealogy().Generate();
        Lot       first     = Any.Lot().Generate();
        Lot       second    = Any.Lot().WithNumber(new LotNumber(first.Number.Value)).Generate();
        genealogy.Register(first);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => genealogy.Register(second));
        Assert.Same(first, genealogy.Get(first.Number));
        Assert.Single(genealogy.Lots);
    }

    [Fact]
    public void Registering_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.BatchGenealogy().Generate().Register(null!));
    }

    [Fact]
    public void Looking_up_a_lot_that_was_never_registered_says_so() {
        // Arrange
        Genealogy genealogy = Any.BatchGenealogy().Generate();
        Lot       anyRegistered = Any.Lot().Generate();
        genealogy.Register(anyRegistered);
        LotNumber anyUnknown = Any.LotNumber().DifferentFrom(anyRegistered.Number).Generate();

        // Act / Assert
        Assert.Throws<KeyNotFoundException>(() => genealogy.Get(anyUnknown));
    }

    [Fact]
    public void Looking_up_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.BatchGenealogy().Generate().Get(null!));
    }

}
