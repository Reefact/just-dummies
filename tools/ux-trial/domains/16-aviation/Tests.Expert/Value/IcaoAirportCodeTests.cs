using Aviation.Domain.Tests.Expert.Generators;

using JustDummies;

namespace Aviation.Domain.Tests.Expert.Value;

public sealed class IcaoAirportCodeTests {

    [Theory]
    [InlineData("LFPG")]
    [InlineData("KJFK")]
    [InlineData("AAAA")]
    [InlineData("ZZZZ")]
    public void A_code_of_four_upper_case_letters_is_accepted_and_keeps_its_value(string value) {
        // Act
        IcaoAirportCode code = new IcaoAirportCode(value);

        // Assert
        Assert.Equal(value, code.Value);
    }

    [Fact]
    public void A_missing_value_is_refused_as_missing() {
        Assert.Throws<ArgumentNullException>(() => new IcaoAirportCode(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("    ")]
    [InlineData("\t\n")]
    public void An_empty_or_blank_value_is_refused_before_its_length_is_looked_at(string value) {
        // "    " has four characters: it is the blank check, not the length check, that refuses it.
        Assert.Throws<ArgumentException>(() => new IcaoAirportCode(value));
    }

    [Theory]
    [InlineData("LFP")]
    [InlineData("L")]
    [InlineData("LFPGX")]
    [InlineData("LFPG ")]
    public void A_value_that_is_not_four_characters_long_is_refused_for_its_length(string value) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new IcaoAirportCode(value));

        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("lfpg")]
    [InlineData("Lfpg")]
    [InlineData("LF1G")]
    [InlineData("LF-G")]
    [InlineData("LF G")]
    [InlineData("LFPÉ")]
    public void A_four_character_value_that_is_not_all_upper_case_ascii_letters_is_refused_for_its_content(string value) {
        // Exactly ArgumentException: the length was right, so the out-of-range refusal must not be the one that answers.
        Assert.Throws<ArgumentException>(() => new IcaoAirportCode(value));
    }

    [Theory]
    [InlineData("E")]
    [InlineData("L")]
    public void An_airport_in_the_E_or_L_region_is_european(string firstLetter) {
        // Arrange
        IcaoAirportCode code = Any.IcaoAirportCode().WithValue(v => v.StartingWith(firstLetter)).Generate();

        // Assert
        Assert.True(code.IsEuropean);
    }

    [Theory]
    [InlineData("K")]
    [InlineData("D")]
    [InlineData("F")]
    [InlineData("M")]
    [InlineData("A")]
    [InlineData("Z")]
    public void An_airport_outside_the_E_and_L_regions_is_not_european(string firstLetter) {
        // Arrange
        IcaoAirportCode code = Any.IcaoAirportCode().WithValue(v => v.StartingWith(firstLetter)).Generate();

        // Assert
        Assert.False(code.IsEuropean);
    }

    [Fact]
    public void Two_codes_built_from_the_same_value_are_distinct_instances() {
        // Characterisation: unlike IataAirportCode, this type does not override Equals (reference equality).
        // Arrange
        string anyValue = Any.IcaoAirportCode().Generate().Value;

        // Act
        IcaoAirportCode first  = new IcaoAirportCode(anyValue);
        IcaoAirportCode second = new IcaoAirportCode(anyValue);

        // Assert
        Assert.NotEqual(first, second);
        Assert.Equal(first.Value, second.Value);
    }

    [Fact]
    public void A_code_prints_as_its_value() {
        Assert.Equal("LFPG", new IcaoAirportCode("LFPG").ToString());
    }

}
