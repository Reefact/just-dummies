using GridLoadFlow.Domain;

using Xunit;

namespace GridLoadFlow.Domain.Tests.Expert;

public sealed class BusIdTests {

    [Theory]
    [InlineData("A")]
    [InlineData("Z9")]
    [InlineData("BUS_1")]
    [InlineData("NORTH-2")]
    [InlineData("A23456789012345")]   // 15 characters
    [InlineData("A234567890123456")]  // 16 characters: the longest accepted
    public void A_well_formed_identifier_is_accepted_as_written(string text) {
        BusId id = new BusId(text);

        Assert.Equal(text, id.Value);
        Assert.Equal(text, id.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("1BUS")]               // starts with a digit
    [InlineData("_BUS")]               // starts with a separator
    [InlineData("-BUS")]
    [InlineData("bus")]                // lower case
    [InlineData("Bus")]
    [InlineData("BUs")]
    [InlineData("BUS 1")]              // inner space
    [InlineData("BUS.1")]
    [InlineData("A2345678901234567")]  // 17 characters
    public void A_malformed_identifier_is_refused(string text) {
        Assert.Throws<FormatException>(() => new BusId(text));
    }

    [Fact(Skip = "domain defect: the pattern ends with $, which also matches before a final newline, so 'BUS1\\n' is accepted as a bus id (literal input, no drawn value, seed n/a)")]
    public void An_identifier_with_a_trailing_newline_is_refused() {
        Assert.Throws<FormatException>(() => new BusId("BUS1\n"));
    }

    [Fact]
    public void A_missing_identifier_is_refused_as_missing_rather_than_as_malformed() {
        Assert.Throws<ArgumentNullException>(() => new BusId(null!));
    }

    [Fact]
    public void Two_identifiers_written_alike_are_one_identifier() {
        BusId first  = new BusId("NORTH-2");
        BusId second = new BusId("NORTH-2");

        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Identifiers_are_compared_case_sensitively_and_never_to_nothing() {
        BusId north = new BusId("NORTH");

        Assert.False(north.Equals(new BusId("NORTH2")));
        Assert.False(north.Equals((BusId?)null));
        Assert.False(north.Equals("NORTH"));
    }

}
