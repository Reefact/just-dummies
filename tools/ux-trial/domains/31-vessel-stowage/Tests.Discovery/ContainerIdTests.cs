using VesselStowage.Domain;

namespace VesselStowage.Tests;

public sealed class ContainerIdTests {

    [Fact]
    public void The_documented_example_is_a_valid_container_number() {
        // Act
        ContainerId id = new ContainerId("CSQU3054383");

        // Assert
        Assert.Equal("CSQU3054383", id.Value);
    }

    [Fact]
    public void The_check_digit_of_the_documented_example_is_3() {
        Assert.Equal(3, ContainerId.ComputeCheckDigit("CSQU305438"));
    }

    [Fact]
    public void A_remainder_of_10_gives_a_check_digit_of_0() {
        // ABCU000007 sums to a remainder of 10 modulo 11
        Assert.Equal(0, ContainerId.ComputeCheckDigit("ABCU000007"));
        Assert.Equal("ABCU0000070", new ContainerId("ABCU0000070").Value);
    }

    [Theory]
    [InlineData('A', 10)]
    [InlineData('B', 12)]
    [InlineData('K', 21)]
    [InlineData('L', 23)]
    [InlineData('U', 32)]
    [InlineData('V', 34)]
    [InlineData('Z', 38)]
    public void Letter_values_skip_the_multiples_of_11(char letter, int expectedValue) {
        // A string "<letter>000000000" has weight 2^0 on the letter and 0 elsewhere: sum = value, check digit = value % 11 % 10
        Assert.Equal(expectedValue % 11 % 10, ContainerId.ComputeCheckDigit(letter + "000000000"));
    }

    [Fact]
    public void A_generated_container_number_is_accepted_by_the_constructor() {
        // Arrange
        ContainerId anyId = Any.ContainerId().Generate();

        // Act
        ContainerId rebuilt = new ContainerId(anyId.Value);

        // Assert: rebuilding from the text is possible and gives an equal id
        Assert.Equal(anyId, rebuilt);
    }

    [Fact]
    public void A_null_number_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new ContainerId(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("CSQU305438")]
    [InlineData("CSQU30543833")]
    public void A_number_of_the_wrong_length_is_refused(string text) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ContainerId(text));

        Assert.Contains("11 characters", refusal.Message);
    }

    [Theory]
    [InlineData("csqU3054383")]
    [InlineData("C1QU3054383")]
    [InlineData("CS QU305438")]
    public void An_owner_code_that_is_not_three_upper_case_letters_is_refused(string text) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ContainerId(text));

        Assert.Contains("owner code", refusal.Message);
    }

    [Theory]
    [InlineData("CSQA3054383")]
    [InlineData("CSQu3054383")]
    [InlineData("CSQ13054383")]
    public void An_equipment_category_other_than_U_J_or_Z_is_refused(string text) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ContainerId(text));

        Assert.Contains("category", refusal.Message);
    }

    [Theory]
    [InlineData("CSQU30543A3")]
    [InlineData("CSQUX054383")]
    [InlineData("CSQU305438X")]
    public void A_serial_number_or_check_digit_that_is_not_a_digit_is_refused(string text) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ContainerId(text));

        Assert.Contains("digits", refusal.Message);
    }

    [Theory]
    [InlineData('J')]
    [InlineData('Z')]
    [InlineData('U')]
    public void Each_of_the_three_categories_is_accepted(char category) {
        // Arrange: the category letter is part of the body the check digit is computed from
        string body = "ABC" + category + "123456";
        string text = body + ContainerId.ComputeCheckDigit(body);

        // Act
        ContainerId id = new ContainerId(text);

        // Assert
        Assert.Equal(text, id.Value);
    }

    [Fact]
    public void A_wrong_check_digit_is_refused_and_named_in_the_message() {
        // Arrange: a valid number whose last digit is moved by one
        string anyValid = Any.ContainerId().Generate().Value;
        int    digit    = anyValid[10] - '0';
        string tampered = anyValid.Substring(0, 10) + (char)('0' + (digit + 1) % 10);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ContainerId(tampered));

        // Assert
        Assert.Contains("Check digit", refusal.Message);
        Assert.Contains(tampered, refusal.Message);
    }

    [Fact]
    public void Two_ids_built_from_the_same_text_are_equal() {
        // Arrange
        string anyText = Any.ContainerId().Generate().Value;

        // Act
        ContainerId first  = new ContainerId(anyText);
        ContainerId second = new ContainerId(anyText);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_ids_of_different_numbers_are_different() {
        Assert.NotEqual(new ContainerId("CSQU3054383"), new ContainerId("ABCU0000070"));
    }

    [Fact]
    public void An_id_is_not_equal_to_null_or_to_another_kind_of_object() {
        ContainerId id = new ContainerId("CSQU3054383");

        Assert.False(id.Equals(null));
        Assert.False(id.Equals((object?)"CSQU3054383"));
    }

    [Fact]
    public void An_id_prints_as_its_number() {
        Assert.Equal("CSQU3054383", new ContainerId("CSQU3054383").ToString());
    }

}
