using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace InternationalCatalogue.Domain.Tests.Discovery;

[Reproducible]
public sealed class IssnTests {

    [Theory]
    [InlineData("0378-5955")]
    [InlineData("2434-561X")]
    [InlineData("0317-8471")]
    [InlineData("2049-3630")]
    public void A_well_formed_issn_is_accepted(string text) {
        Assert.Equal(text, new Issn(text).Value);
    }

    [Fact]
    public void An_issn_check_character_of_ten_is_written_with_an_upper_case_X() {
        // Act
        Issn issn = new Issn("2434-561X");

        // Assert
        Assert.Equal("2434-561X", issn.ToString());
    }

    [Theory]
    [InlineData("2434-561x")]
    [InlineData("03785955")]
    [InlineData("0378 5955")]
    [InlineData("0378–5955")]
    [InlineData("378-5955")]
    [InlineData("0378-59555")]
    [InlineData("0378-595")]
    [InlineData("0378-595A")]
    [InlineData("X378-5955")]
    [InlineData("")]
    [InlineData(" 0378-5955")]
    [InlineData("0378-5955 ")]
    [InlineData("0378-5955\n")]
    [InlineData("٠٣٧٨-٥٩٥٥")]
    public void An_issn_is_written_four_digits_a_hyphen_three_digits_and_a_check_character(string text) {
        Assert.Throws<FormatException>(() => new Issn(text));
    }

    [Fact]
    public void An_issn_with_a_wrong_check_character_is_refused() {
        // Arrange
        string anyBody = Any.StringMatching("[0-9]{7}").Generate();
        char correct = CheckCharacters.Issn(anyBody);
        char wrong = correct == '0' ? '1' : '0';

        // Act
        FormatException error = Assert.Throws<FormatException>(() => new Issn(anyBody[..4] + "-" + anyBody[4..] + wrong));

        // Assert
        Assert.Contains(correct.ToString(), error.Message);
    }

    [Theory]
    [InlineData("0378595", '5')]
    [InlineData("2434561", 'X')]
    [InlineData("0000000", '0')]
    public void The_check_character_follows_the_weights_eight_down_to_two(string sevenDigits, char expected) {
        Assert.Equal(expected, Issn.ComputeCheckCharacter(sevenDigits));
    }

    [Fact]
    public void A_null_issn_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Issn(null!));
    }

    [Fact]
    public void Two_issn_are_equal_when_their_text_is() {
        Assert.Equal(new Issn("0378-5955"), new Issn("0378-5955"));
        Assert.NotEqual(new Issn("0378-5955"), new Issn("0317-8471"));
    }

}
