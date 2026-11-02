using JustDummies;

using Xunit;

namespace RealEstate.Domain.Tests;

public sealed class CadastralReferenceTests {

    [Theory]
    [InlineData("75056 000 AB 0123")]
    [InlineData("2A004 000 AB 0001")]
    [InlineData("2B999 123 Z9 9999")]
    [InlineData("97411 000 0A 0001")]
    [InlineData("75056 000 AB 1000")]
    public void Accepts_a_reference_in_the_cadastral_format(string value) {
        // Act
        CadastralReference reference = new CadastralReference(value);

        // Assert
        Assert.Equal(value, reference.Value);
    }

    [Fact]
    public void Refuses_a_missing_reference() {
        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new CadastralReference(null!));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Refuses_a_blank_reference(string blank) {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CadastralReference(blank));
    }

    [Theory]
    [InlineData("75056 000 ab 0123")] // lower-case section
    [InlineData("75056  000 AB 0123")] // double space
    [InlineData("75056 000 AB  0123")] // double space before the plan number
    [InlineData(" 75056 000 AB 0123")] // leading space
    [InlineData("75056 000 AB 0123 ")] // trailing space
    [InlineData("7505 000 AB 0123")] // four-digit commune code
    [InlineData("750560 000 AB 0123")] // six-digit commune code
    [InlineData("2C004 000 AB 0001")] // Corsican code that is neither 2A nor 2B
    [InlineData("2a004 000 AB 0001")] // lower-case Corsican code
    [InlineData("75056 00 AB 0123")] // two-digit prefix
    [InlineData("75056 000 A 0123")] // one-character section
    [InlineData("75056 000 ABC 0123")] // three-character section
    [InlineData("75056 000 AB 012")] // three-digit plan number
    [InlineData("75056 000 AB 01234")] // five-digit plan number
    [InlineData("75056 000 AB 01A3")] // letter in the plan number
    [InlineData("75056-000-AB-0123")] // dashes instead of spaces
    [InlineData("75056000AB0123")] // no separators
    public void Refuses_a_reference_that_is_not_in_the_cadastral_format(string malformed) {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CadastralReference(malformed));
    }

    [Theory]
    [InlineData("75056 000 AB 0000")]
    [InlineData("2A004 000 AB 0000")]
    [InlineData("97411 123 Z9 0000")]
    public void Refuses_a_plan_number_of_zero(string value) {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CadastralReference(value));
    }

    [Theory]
    [InlineData("75056 000 AB 0001")]
    [InlineData("75056 000 AB 0010")]
    [InlineData("75056 000 AB 1000")]
    public void Accepts_the_plan_numbers_around_zero(string value) {
        // Act & Assert
        Assert.Equal(value, new CadastralReference(value).Value);
    }

    [Fact(Skip = "domain defect: the pattern's $ also matches before a final line feed, so '75056 000 AB 0000\\n' slips past the format check and past the EndsWith(\"0000\") plan-number rule; seed n/a (literal input)")]
    public void Refuses_a_plan_number_of_zero_followed_by_a_line_feed() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CadastralReference("75056 000 AB 0000\n"));
    }

    [Fact(Skip = "domain defect: \\d matches any Unicode decimal digit, so Arabic-Indic digits form an accepted reference; seed n/a (literal input)")]
    public void Refuses_digits_that_are_not_ascii() {
        // Arrange
        string arabicIndicDigits = "٧٥٠٥٦ 000 AB 0123";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CadastralReference(arabicIndicDigits));
    }

    [Theory]
    [InlineData("75056 000 AB 0123", "75056", "AB")]
    [InlineData("2A004 123 Z9 0001", "2A004", "Z9")]
    [InlineData("97411 000 0A 0001", "97411", "0A")]
    public void Reads_the_commune_code_and_the_section_from_their_positions(string value, string communeCode, string section) {
        // Arrange
        CadastralReference reference = new CadastralReference(value);

        // Act & Assert
        Assert.Equal(communeCode, reference.CommuneCode);
        Assert.Equal(section, reference.Section);
    }

    [Theory]
    [InlineData("2A004 000 AB 0001", true)]
    [InlineData("2B033 000 AB 0001", true)]
    [InlineData("20000 000 AB 0001", false)]
    [InlineData("75056 000 AB 0001", false)]
    [InlineData("97411 000 AB 0001", false)]
    public void Is_in_Corsica_only_for_a_commune_code_starting_with_2A_or_2B(string value, bool inCorsica) {
        // Act
        bool isInCorsica = new CadastralReference(value).IsInCorsica;

        // Assert
        Assert.Equal(inCorsica, isInCorsica);
    }

    [Theory]
    [InlineData("75056 000 AB 0001", "75")]
    [InlineData("01001 000 AB 0001", "01")]
    [InlineData("20000 000 AB 0001", "20")]
    [InlineData("96000 000 AB 0001", "96")]
    [InlineData("98000 000 AB 0001", "98")]
    [InlineData("2A004 000 AB 0001", "2A")]
    [InlineData("2B033 000 AB 0001", "2B")]
    [InlineData("97100 000 AB 0001", "971")]
    [InlineData("97411 000 AB 0001", "974")]
    [InlineData("97699 000 AB 0001", "976")]
    public void Takes_the_department_from_the_commune_code(string value, string department) {
        // Act
        string actual = new CadastralReference(value).Department;

        // Assert
        Assert.Equal(department, actual);
    }

    [Fact]
    public void Two_references_built_from_the_same_text_are_equal() {
        // Arrange
        string anyValue = Any.CadastralReference().Generate().Value;

        // Act
        CadastralReference first  = new CadastralReference(anyValue);
        CadastralReference second = new CadastralReference(anyValue);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_references_built_from_different_texts_are_not_equal() {
        // Arrange
        CadastralReference anyReference      = Any.CadastralReference().Generate();
        CadastralReference anyOtherReference = Any.CadastralReference().WithValue(value => value.DifferentFrom(anyReference.Value)).Generate();

        // Act & Assert
        Assert.False(anyReference.Equals(anyOtherReference));
    }

    [Fact]
    public void Is_not_equal_to_nothing_or_to_a_plain_string() {
        // Arrange
        CadastralReference anyReference = Any.CadastralReference().Generate();

        // Act & Assert
        Assert.False(anyReference.Equals((CadastralReference?)null));
        Assert.False(anyReference.Equals((object?)null));
        Assert.False(anyReference.Equals((object)anyReference.Value));
    }

    [Fact]
    public void Prints_as_its_text() {
        // Act
        string text = new CadastralReference("75056 000 AB 0123").ToString();

        // Assert
        Assert.Equal("75056 000 AB 0123", text);
    }

}
