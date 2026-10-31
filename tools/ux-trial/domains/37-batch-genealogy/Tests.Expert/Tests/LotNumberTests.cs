namespace BatchGenealogy.Tests.Expert.Tests;

public sealed class LotNumberTests {

    [Fact]
    public void A_sub_lot_number_is_its_lot_number_with_a_two_digit_index() {
        // Arrange
        LotNumber lot = new("API-240117");

        // Act
        LotNumber third = lot.SubLot(3);

        // Assert
        Assert.Equal("API-240117.03", third.Value);
    }

    [Fact]
    public void A_sibling_of_a_sub_lot_belongs_to_the_same_base_lot() {
        // Arrange
        LotNumber fifth = new("API-240117.05");

        // Act
        LotNumber second = fifth.SubLot(2);

        // Assert
        Assert.Equal("API-240117.02", second.Value);
    }

}
