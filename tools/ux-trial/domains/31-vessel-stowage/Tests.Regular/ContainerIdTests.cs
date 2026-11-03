using JustDummies;

using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class ContainerIdTests {

    [Fact]
    public void A_known_ISO_6346_number_is_accepted() {
        // Act
        ContainerId id = new ContainerId("CSQU3054383");

        // Assert
        Assert.Equal("CSQU3054383", id.Value);
        Assert.Equal("CSQU3054383", id.ToString());
    }

    [Fact]
    public void The_check_digit_of_a_known_number_is_computed_from_its_first_ten_characters() {
        // Act
        int checkDigit = ContainerId.ComputeCheckDigit("CSQU305438");

        // Assert
        Assert.Equal(3, checkDigit);
    }

    [Theory]
    [InlineData('U')]
    [InlineData('J')]
    [InlineData('Z')]
    public void Each_of_the_three_equipment_categories_is_accepted(char category) {
        // Arrange
        string anyOwnerCode = Any.String().Alpha().InUpperCase().WithLength(3).Generate();
        string anySerial    = Any.StringMatching("[0-9]{6}").Generate();
        string firstTen     = anyOwnerCode + category + anySerial;

        // Act
        ContainerId id = new ContainerId(firstTen + ContainerId.ComputeCheckDigit(firstTen));

        // Assert
        Assert.Equal(category, id.Value[3]);
    }

    [Fact]
    public void A_wrong_check_digit_is_refused() {
        // Arrange
        ContainerId anyId = Any.ContainerId().Generate();
        int wrongDigit = (anyId.Value[10] - '0' + 1) % 10;
        string tampered = anyId.Value.Substring(0, 10) + wrongDigit;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>("value", () => new ContainerId(tampered));

        // Assert
        Assert.Contains("Check digit", refusal.Message);
    }

    [Theory]
    [InlineData("CSQU305438")]
    [InlineData("CSQU30543833")]
    [InlineData("")]
    public void A_number_that_is_not_eleven_characters_long_is_refused(string value) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>("value", () => new ContainerId(value));

        // Assert
        Assert.Contains("11 characters", refusal.Message);
    }

    [Theory]
    [InlineData("csqU3054383")]
    [InlineData("C5QU3054383")]
    [InlineData("CS-U3054383")]
    public void An_owner_code_that_is_not_three_upper_case_letters_is_refused(string value) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>("value", () => new ContainerId(value));

        // Assert
        Assert.Contains("owner code", refusal.Message);
    }

    [Theory]
    [InlineData("CSQA3054383")]
    [InlineData("CSQu3054383")]
    [InlineData("CSQ13054383")]
    public void An_equipment_category_other_than_U_J_or_Z_is_refused(string value) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>("value", () => new ContainerId(value));

        // Assert
        Assert.Contains("category", refusal.Message);
    }

    [Theory]
    [InlineData("CSQU30X4383")]
    [InlineData("CSQU305438A")]
    [InlineData("CSQU 054383")]
    public void A_serial_number_or_check_digit_that_is_not_a_digit_is_refused(string value) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>("value", () => new ContainerId(value));

        // Assert
        Assert.Contains("must be digits", refusal.Message);
    }

    [Fact]
    public void A_null_number_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("value", () => new ContainerId(null!));
    }

    [Fact]
    public void Two_ids_with_the_same_number_are_equal_and_share_a_hash_code() {
        // Arrange
        ContainerId anyId = Any.ContainerId().Generate();

        // Act
        ContainerId sameNumber = new ContainerId(anyId.Value);

        // Assert
        Assert.Equal(anyId, sameNumber);
        Assert.True(anyId.Equals((object)sameNumber));
        Assert.Equal(anyId.GetHashCode(), sameNumber.GetHashCode());
    }

    [Fact]
    public void Two_ids_with_different_numbers_are_not_equal() {
        // Arrange
        ContainerId anyId      = Any.ContainerId().Generate();
        ContainerId anyOtherId = Any.ContainerId().DifferentFrom(anyId).Generate();

        // Assert
        Assert.False(anyId.Equals(anyOtherId));
        Assert.False(anyId.Equals(null));
    }

}
