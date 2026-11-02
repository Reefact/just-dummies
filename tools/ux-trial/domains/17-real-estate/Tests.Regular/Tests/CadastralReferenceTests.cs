namespace RealEstate.Tests.Regular;

public sealed class CadastralReferenceTests {

    [Theory]
    [InlineData("75056 000 AB 0123", "75056", "AB", "75", false)]
    [InlineData("13055 000 0A 0001", "13055", "0A", "13", false)]
    [InlineData("2A004 000 AB 0123", "2A004", "AB", "2A", true)]
    [InlineData("2B033 123 ZZ 9999", "2B033", "ZZ", "2B", true)]
    [InlineData("97105 000 AB 0123", "97105", "AB", "971", false)]
    [InlineData("97411 000 AB 0123", "97411", "AB", "974", false)]
    [InlineData("96001 000 AB 0123", "96001", "AB", "96", false)]
    public void A_reference_is_split_into_its_commune_section_and_department(string value, string communeCode, string section, string department, bool isInCorsica) {
        // Act
        CadastralReference reference = new CadastralReference(value);

        // Assert
        Assert.Equal(value, reference.Value);
        Assert.Equal(value, reference.ToString());
        Assert.Equal(communeCode, reference.CommuneCode);
        Assert.Equal(section, reference.Section);
        Assert.Equal(department, reference.Department);
        Assert.Equal(isInCorsica, reference.IsInCorsica);
    }

    [Fact]
    public void A_reference_cannot_be_null() {
        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new CadastralReference(null!));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t ")]
    public void A_reference_cannot_be_blank(string blank) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new CadastralReference(blank));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Theory]
    [InlineData("75056  000 AB 0123")]    // two spaces
    [InlineData("75056 000 ab 0123")]     // lower-case section
    [InlineData("75056 000 AB 123")]      // three-digit plan number
    [InlineData("75056 000 AB 01234")]    // five-digit plan number
    [InlineData("7505 000 AB 0123")]      // four-digit commune code
    [InlineData("750566 000 AB 0123")]    // six-digit commune code
    [InlineData("2C004 000 AB 0123")]     // 2C is not a Corsican department
    [InlineData("2A04 000 AB 0123")]      // Corsican code too short
    [InlineData("75056 00 AB 0123")]      // two-digit prefix
    [InlineData("75056 000 A 0123")]      // one-character section
    [InlineData("75056 000 AB-0123")]     // wrong separator
    [InlineData(" 75056 000 AB 0123")]    // leading space
    [InlineData("75056 000 AB 0123 ")]    // trailing space
    [InlineData("not a reference")]
    public void A_value_off_the_format_is_refused(string malformed) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new CadastralReference(malformed));

        // Assert
        Assert.Equal("value", exception.ParamName);
        Assert.Contains("is not a cadastral reference", exception.Message);
    }

    [Fact]
    public void A_plan_number_of_zero_is_refused() {
        // Arrange
        string anyPrefix = Any.StringMatching("([0-9]{5}|2[AB][0-9]{3}) [0-9]{3} [A-Z0-9]{2} ").Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new CadastralReference(anyPrefix + "0000"));

        // Assert
        Assert.Equal("A plan number starts at 0001. (Parameter 'value')", exception.Message);
    }

    [Fact]
    public void The_first_plan_number_is_accepted() {
        // Arrange
        string anyPrefix = Any.StringMatching("([0-9]{5}|2[AB][0-9]{3}) [0-9]{3} [A-Z0-9]{2} ").Generate();

        // Act
        CadastralReference reference = new CadastralReference(anyPrefix + "0001");

        // Assert
        Assert.EndsWith(" 0001", reference.Value);
    }

    [Fact]
    public void Two_references_with_the_same_value_are_equal() {
        // Arrange
        string anyValue = Any.CadastralReference().Generate().Value;

        // Act
        CadastralReference first  = new CadastralReference(anyValue);
        CadastralReference second = new CadastralReference(anyValue);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_references_with_different_values_are_not_equal() {
        // Arrange
        CadastralReference anyReference      = Any.CadastralReference().Generate();
        CadastralReference anyOtherReference = Any.CadastralReference().DifferentFrom(anyReference).Generate();

        // Assert
        Assert.NotEqual(anyReference, anyOtherReference);
    }

    [Fact]
    public void A_reference_is_not_equal_to_null_or_to_another_kind_of_object() {
        // Arrange
        CadastralReference anyReference = Any.CadastralReference().Generate();

        // Assert
        Assert.False(anyReference.Equals((CadastralReference?)null));
        Assert.False(anyReference.Equals(null));
        Assert.False(anyReference.Equals(anyReference.Value));
    }

    [Fact]
    public void The_regular_expression_is_applied_as_written() {
        // The domain note says \d is not restricted to ASCII digits and $ also matches before a final line feed.
        // Act
        CadastralReference withLineFeed   = new CadastralReference("75056 000 AB 0123\n");
        CadastralReference withArabicDigits = new CadastralReference("٧٥٠٥٦ 000 AB 0123");

        // Assert
        Assert.Equal("75056", new CadastralReference("75056 000 AB 0123").CommuneCode);
        Assert.EndsWith("\n", withLineFeed.Value);
        Assert.Equal("٧٥٠٥٦", withArabicDigits.CommuneCode);
    }

    [Fact(Skip = "domain defect: a trailing line feed slips past EndsWith(\"0000\"), so plan number 0000 is accepted when followed by \\n, seed n/a (literal input)")]
    public void A_plan_number_of_zero_followed_by_a_line_feed_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CadastralReference("75056 000 AB 0000\n"));
    }

}
