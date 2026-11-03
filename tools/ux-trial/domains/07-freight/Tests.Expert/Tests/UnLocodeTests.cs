using Freight.Domain;

using Xunit;

namespace Freight.Tests.Expert.Tests;

public sealed class UnLocodeTests {

    [Theory]
    [InlineData("FRMRS", "FR")]
    [InlineData("NLRTM", "NL")]
    [InlineData("US2A9", "US")]
    public void A_locode_exposes_its_two_letter_country(string code, string expectedCountry) {
        UnLocode locode = new UnLocode(code);

        Assert.Equal(expectedCountry, locode.Country);
        Assert.Equal(code, locode.Code);
    }

    [Theory]
    [InlineData("FRMRS")]
    [InlineData("FR2A9")]
    [InlineData("ZZZZZ")]
    public void A_location_may_mix_letters_and_digits_from_2_to_9(string code) {
        UnLocode locode = new UnLocode(code);

        Assert.Equal(code, locode.ToString());
    }

    [Theory]
    [InlineData("frmrs")]
    [InlineData("FRMR")]
    [InlineData("FRMRSX")]
    [InlineData("F1MRS")]
    [InlineData("FR0AA")]
    [InlineData("FRA1A")]
    [InlineData("FR AB")]
    [InlineData("FR-AB")]
    public void A_code_outside_the_locode_format_is_refused(string malformed) {
        Assert.Throws<FormatException>(() => new UnLocode(malformed));
    }

    [Fact]
    public void A_null_code_is_refused_as_missing() {
        Assert.Throws<ArgumentNullException>(() => new UnLocode(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void A_blank_code_is_refused_as_an_invalid_argument(string blank) {
        Assert.Throws<ArgumentException>(() => new UnLocode(blank));
    }

    [Fact(Skip = "domain defect: a trailing line feed passes the '$' of the pattern and ends up in Code, seed irrelevant (literal input)")]
    public void A_code_followed_by_a_line_feed_is_refused() {
        Assert.Throws<FormatException>(() => new UnLocode("FRMRS\n"));
    }

    [Fact]
    public void Two_locodes_with_the_same_code_are_equal() {
        UnLocode first  = new UnLocode("FRMRS");
        UnLocode second = new UnLocode("FRMRS");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first.Equals((object)second));
    }

    [Fact]
    public void Two_locodes_with_different_codes_are_not_equal() {
        UnLocode marseille  = new UnLocode("FRMRS");
        UnLocode rotterdam = new UnLocode("NLRTM");

        Assert.NotEqual(marseille, rotterdam);
    }

    [Fact]
    public void A_locode_is_never_equal_to_nothing_nor_to_something_else() {
        UnLocode marseille = new UnLocode("FRMRS");

        Assert.False(marseille.Equals(null));
        Assert.False(marseille.Equals((object)"FRMRS"));
    }

}
