using JustDummies;
using VesselStowage.Domain.Tests.Generators;
using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class ContainerIdTests {

    [Fact]
    public void The_number_from_the_ISO_6346_standard_is_accepted() {
        // Act
        ContainerId id = new ContainerId("CSQU3054383");

        // Assert
        Assert.Equal("CSQU3054383", id.Value);
        Assert.Equal("CSQU3054383", id.ToString());
    }

    [Theory]
    [InlineData("CSQU305438", 3)]
    [InlineData("MSKU123456", 5)]
    [InlineData("KLWU000000", 1)] // K (21) and L (23): the letter values skip 22
    [InlineData("VVVU000000", 0)] // V is 34: the letter values skip 33
    [InlineData("ABCZ000001", 0)]
    [InlineData("ZZZJ999999", 5)]
    public void The_check_digit_follows_the_ISO_6346_algorithm(string firstTen, int expectedCheckDigit) {
        // Act
        int checkDigit = ContainerId.ComputeCheckDigit(firstTen);

        // Assert
        Assert.Equal(expectedCheckDigit, checkDigit);
    }

    [Theory]
    [InlineData("CSQU3054380")]
    [InlineData("CSQU3054381")]
    [InlineData("CSQU3054382")]
    [InlineData("CSQU3054384")]
    [InlineData("CSQU3054389")]
    public void A_wrong_check_digit_is_refused(string number) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ContainerId(number));

        // Assert
        Assert.Contains("Check digit", refusal.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("CSQU305438")]
    [InlineData("CSQU30543833")]
    [InlineData("CSQU 3054383")]
    public void A_number_of_the_wrong_length_is_refused(string number) {
        Assert.Throws<ArgumentException>(() => new ContainerId(number));
    }

    [Theory]
    [InlineData("csqU3054383")] // lower-case owner code
    [InlineData("C5QU3054383")] // a digit in the owner code
    [InlineData("CSQX3054383")] // a category other than U, J or Z
    [InlineData("CSQu3054383")] // lower-case category
    [InlineData("CSQU30A4383")] // a letter in the serial number
    [InlineData("CSQU305438X")] // a letter as check digit
    public void A_malformed_number_is_refused(string number) {
        Assert.Throws<ArgumentException>(() => new ContainerId(number));
    }

    [Theory]
    [InlineData("ABCU1234560", 'U')]
    [InlineData("ABCJ1234563", 'J')]
    [InlineData("ABCZ1234564", 'Z')]
    public void The_three_equipment_categories_are_accepted(string number, char category) {
        // Act
        ContainerId id = new ContainerId(number);

        // Assert
        Assert.Equal(category, id.Value[3]);
    }

    [Fact]
    public void A_missing_number_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new ContainerId(null!));
    }

    [Fact]
    public void Two_ids_built_from_the_same_number_are_equal() {
        // Arrange
        string anyNumber = Any.ContainerId().Generate().Value;

        // Act
        ContainerId first  = new ContainerId(anyNumber);
        ContainerId second = new ContainerId(anyNumber);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_ids_with_different_numbers_are_not_equal() {
        // Arrange
        ContainerId anyId      = Any.ContainerId().Generate();
        ContainerId anyOtherId = Any.ContainerId().DifferentFrom(anyId).Generate();

        // Assert
        Assert.NotEqual(anyId, anyOtherId);
    }

    [Fact]
    public void An_id_is_not_equal_to_nothing_or_to_a_plain_string() {
        // Arrange
        ContainerId anyId = Any.ContainerId().Generate();

        // Assert
        Assert.False(anyId.Equals(null));
        Assert.False(anyId.Equals((object?)anyId.Value));
    }

}
