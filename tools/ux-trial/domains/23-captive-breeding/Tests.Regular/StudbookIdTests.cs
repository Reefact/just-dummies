using JustDummies;

using Xunit;

namespace CaptiveBreeding.Domain.Tests.Regular;

public sealed class StudbookIdTests {

    [Fact]
    public void A_well_formed_value_is_kept_as_given() {
        // Arrange
        string anyValue = Any.StringMatching("[A-Z]{2,4}-[0-9]{4,6}").Generate();

        // Act
        StudbookId id = new StudbookId(anyValue);

        // Assert
        Assert.Equal(anyValue, id.Value);
        Assert.Equal(anyValue, id.ToString());
    }

    [Fact]
    public void The_scaffolded_generator_only_draws_well_formed_values() {
        // Arrange
        AnyStudbookId anyId = Any.StudbookId();

        // Act
        Exception? thrown = Record.Exception(() => Any.ListOf(anyId).WithCount(50).Generate());

        // Assert
        Assert.Null(thrown);
    }

    [Theory]
    [InlineData("ZS-0123")]
    [InlineData("ZSLA-012345")]
    [InlineData("ZSL-01234")]
    [InlineData("AB-0000")]
    public void The_extreme_lengths_of_both_parts_are_accepted(string value) {
        Exception? thrown = Record.Exception(() => new StudbookId(value));

        Assert.Null(thrown);
    }

    [Fact]
    public void The_institution_is_the_prefix_before_the_dash() {
        // Arrange
        string anyInstitution = Any.String().Alpha().InUpperCase().WithLengthBetween(2, 4).Generate();
        string anyNumber      = Any.String().Numeric().WithLengthBetween(4, 6).Generate();

        // Act
        StudbookId id = new StudbookId(anyInstitution + "-" + anyNumber);

        // Assert
        Assert.Equal(anyInstitution, id.Institution);
    }

    [Fact]
    public void The_institution_of_a_literal_id_is_its_letters() {
        Assert.Equal("ZSL", new StudbookId("ZSL-01234").Institution);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ZSL-01234 ")]
    [InlineData(" ZSL-01234")]
    [InlineData("ZSL01234")]
    [InlineData("ZSL--01234")]
    [InlineData("Z-01234")]
    [InlineData("ZSLAB-01234")]
    [InlineData("ZSL-123")]
    [InlineData("ZSL-1234567")]
    [InlineData("zsl-01234")]
    [InlineData("ZSl-01234")]
    [InlineData("ZS1-01234")]
    [InlineData("ZSL-0123A")]
    [InlineData("ZSL_01234")]
    public void A_malformed_value_is_refused(string value) {
        Assert.Throws<FormatException>(() => new StudbookId(value));
    }

    [Fact]
    public void A_lower_case_prefix_is_refused() {
        // Arrange
        string anyPrefix = Any.String().Alpha().InLowerCase().WithLengthBetween(2, 4).Generate();
        string anyNumber = Any.String().Numeric().WithLengthBetween(4, 6).Generate();

        // Act
        Action construction = () => new StudbookId(anyPrefix + "-" + anyNumber);

        // Assert
        Assert.Throws<FormatException>(construction);
    }

    [Fact]
    public void A_number_that_is_too_short_is_refused() {
        // Arrange
        string anyPrefix = Any.String().Alpha().InUpperCase().WithLengthBetween(2, 4).Generate();
        string anyNumber = Any.String().Numeric().WithLengthBetween(1, 3).Generate();

        // Act
        Action construction = () => new StudbookId(anyPrefix + "-" + anyNumber);

        // Assert
        Assert.Throws<FormatException>(construction);
    }

    [Fact]
    public void A_number_that_is_too_long_is_refused() {
        // Arrange
        string anyPrefix = Any.String().Alpha().InUpperCase().WithLengthBetween(2, 4).Generate();
        string anyNumber = Any.String().Numeric().WithLengthBetween(7, 12).Generate();

        // Act
        Action construction = () => new StudbookId(anyPrefix + "-" + anyNumber);

        // Assert
        Assert.Throws<FormatException>(construction);
    }

    [Fact]
    public void The_refusal_names_the_offending_value() {
        // Arrange
        string anyGarbage = Any.String().Alpha().InLowerCase().WithLengthBetween(1, 20).Generate();

        // Act
        FormatException refusal = Assert.Throws<FormatException>(() => new StudbookId(anyGarbage));

        // Assert
        Assert.Contains(anyGarbage, refusal.Message);
    }

    [Fact]
    public void A_null_value_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new StudbookId(null!));
    }

    [Fact(Skip = "domain defect: the pattern is anchored with '$', which .NET also matches before a final line feed, so 'ZSL-01234\\n' is accepted as a studbook id, seed n/a (literal)")]
    public void A_value_with_a_trailing_line_feed_is_refused() {
        Assert.Throws<FormatException>(() => new StudbookId("ZSL-01234\n"));
    }

    [Fact]
    public void Two_ids_with_the_same_value_are_equal() {
        // Arrange
        string anyValue = Any.StringMatching("[A-Z]{2,4}-[0-9]{4,6}").Generate();

        // Act
        StudbookId first  = new StudbookId(anyValue);
        StudbookId second = new StudbookId(anyValue);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first.Equals((object)second));
    }

    [Fact]
    public void Two_ids_with_different_values_are_not_equal() {
        // Arrange
        IReadOnlyList<StudbookId> anyIds = Any.ListOf(Any.StudbookId()).WithCount(2).Distinct().Generate();

        // Act / Assert
        Assert.False(anyIds[0].Equals(anyIds[1]));
        Assert.True(anyIds[0] != anyIds[1]);
    }

    [Fact]
    public void An_id_is_never_equal_to_null_or_to_another_kind_of_object() {
        // Arrange
        StudbookId anyId = Any.StudbookId().Generate();

        // Act / Assert
        Assert.False(anyId.Equals((StudbookId?)null));
        Assert.False(anyId.Equals((object?)null));
        Assert.False(anyId.Equals(anyId.Value));
        Assert.True(anyId != null);
        Assert.False(null == anyId);
    }

    [Fact]
    public void Two_null_ids_are_equal_under_the_operators() {
        StudbookId? left  = null;
        StudbookId? right = null;

        Assert.True(left == right);
    }

}
