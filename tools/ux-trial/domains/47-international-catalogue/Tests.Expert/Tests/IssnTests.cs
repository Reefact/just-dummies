using Xunit;

namespace InternationalCatalogue.Domain.Tests;

public sealed class IssnTests {

    [Theory]
    [InlineData("0378-5955")]
    [InlineData("2049-3630")]
    [InlineData("0000-006X")]
    [InlineData("2434-561X")]
    [InlineData("0000-0000")]
    public void A_valid_issn_is_kept_as_written(string written) {
        // Act
        Issn issn = new Issn(written);

        // Assert
        Assert.Equal(written, issn.Value);
        Assert.Equal(written, issn.ToString());
    }

    [Fact]
    public void An_issn_is_required() {
        Assert.Throws<ArgumentNullException>(() => new Issn(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("03785955")]
    [InlineData("0378 5955")]
    [InlineData("0378-595")]
    [InlineData("0378-59555")]
    [InlineData("037-85955")]
    [InlineData("0378-595x")]
    [InlineData("0378-595Y")]
    [InlineData("0378-5955\n")]
    [InlineData(" 0378-5955")]
    [InlineData("０３７８-５９５５")]
    public void An_issn_is_written_four_digits_a_hyphen_and_four_characters(string malformed) {
        Assert.Throws<FormatException>(() => new Issn(malformed));
    }

    [Theory]
    [InlineData("0378-5954", '5')]
    [InlineData("0000-0060", 'X')]
    [InlineData("2049-3631", '0')]
    public void The_check_character_must_be_right(string wrong, char expected) {
        // Act
        FormatException refusal = Assert.Throws<FormatException>(() => new Issn(wrong));

        // Assert
        Assert.Contains($"should be {expected}", refusal.Message);
    }

    [Theory]
    [InlineData("0378595", '5')]
    [InlineData("2049363", '0')]
    [InlineData("0000006", 'X')]
    [InlineData("0000013", '2')]
    [InlineData("1234567", '9')]
    public void The_check_character_weighs_the_seven_digits_from_eight_down_to_two(string sevenDigits, char expected) {
        Assert.Equal(expected, Issn.ComputeCheckCharacter(sevenDigits));
    }

    [Fact]
    public void Two_issns_are_equal_when_written_the_same() {
        Assert.Equal(new Issn("0378-5955"), new Issn("0378-5955"));
        Assert.NotEqual(new Issn("0378-5955"), new Issn("2049-3630"));
    }

}
