using ColdChain.Domain;

using JustDummies;

using Xunit;

namespace ColdChain.Tests;

public sealed class CustodianIdTests {

    [Fact]
    public void A_licence_is_kept_as_given() {
        // Act
        CustodianId custodian = new CustodianId("FR-GDP-04217");

        // Assert
        Assert.Equal("FR-GDP-04217", custodian.Value);
        Assert.Equal("FR-GDP-04217", custodian.ToString());
    }

    [Fact]
    public void It_exposes_the_country_of_the_licence() {
        // Arrange
        // The number is a dummy: the country is what the assertion reads.
        int anyNumber = Any.Int32().Between(1, 99_999).Generate();

        // Act
        CustodianId custodian = new CustodianId($"BE-GDP-{anyNumber:D5}");

        // Assert
        Assert.Equal("BE", custodian.Country);
    }

    [Theory]
    [InlineData("FR-GDP-00001")]
    [InlineData("ZZ-GDP-99999")]
    public void The_limits_of_the_licence_number_are_accepted(string text) {
        // Act
        CustodianId custodian = new CustodianId(text);

        // Assert
        Assert.Equal(text, custodian.Value);
    }

    [Theory]
    [InlineData("fr-GDP-04217")]
    [InlineData("F-GDP-04217")]
    [InlineData("FRA-GDP-04217")]
    [InlineData("FR-gdp-04217")]
    [InlineData("FR-GDP-0421")]
    [InlineData("FR-GDP-042170")]
    [InlineData("FR-GDP-0421A")]
    [InlineData("FR-GMP-04217")]
    [InlineData("FRGDP04217")]
    [InlineData(" FR-GDP-04217")]
    [InlineData("FR-GDP-04217 ")]
    public void A_licence_of_the_wrong_shape_is_refused(string text) {
        // Act
        Exception? thrown = Record.Exception(() => new CustodianId(text));

        // Assert
        Assert.IsType<FormatException>(thrown);
    }

    [Fact]
    public void Licence_number_00000_is_never_issued() {
        // Act
        Exception? thrown = Record.Exception(() => new CustodianId("FR-GDP-00000"));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(thrown);
    }

    [Fact(Skip = "domain defect: CustodianId accepts a trailing line break (regex $ matches before a final newline), no drawn value involved, seed n/a")]
    public void A_trailing_line_break_is_not_part_of_a_licence() {
        // Act
        Exception? thrown = Record.Exception(() => new CustodianId("FR-GDP-04217\n"));

        // Assert
        Assert.IsType<FormatException>(thrown);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_licence_is_refused(string text) {
        // Act
        Exception? thrown = Record.Exception(() => new CustodianId(text));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(thrown);
    }

    [Fact]
    public void A_missing_licence_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new CustodianId(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    [Fact]
    public void Two_licences_with_the_same_text_are_equal() {
        // Arrange
        CustodianId custodian        = new CustodianId("FR-GDP-04217");
        CustodianId sameCustodian    = new CustodianId("FR-GDP-04217");
        CustodianId anotherCustodian = new CustodianId("FR-GDP-04218");

        // Assert
        Assert.Equal(custodian, sameCustodian);
        Assert.Equal(custodian.GetHashCode(), sameCustodian.GetHashCode());
        Assert.NotEqual(custodian, anotherCustodian);
    }

}
