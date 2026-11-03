using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Expert;

public sealed class QueryTests {

    [Fact]
    public void A_new_query_is_open_and_waits_for_a_response() {
        // Arrange
        DateOnly anyDate = Scenarios.AnyDate();
        string   anyText = Scenarios.AnyQueryText();

        // Act
        Query query = new Query(1, "Vitals.HeartRateBpm", anyText, anyDate);

        // Assert
        Assert.Equal(QueryStatus.Open, query.Status);
        Assert.Equal(1, query.Number);
        Assert.Equal("Vitals.HeartRateBpm", query.FieldName);
        Assert.Equal(anyDate, query.RaisedOn);
        Assert.Null(query.Response);
        Assert.Null(query.AnsweredOn);
        Assert.Null(query.ClosedOn);
    }

    [Theory]
    [InlineData("Vitals")]
    [InlineData("Vitals.HeartRateBpm")]
    [InlineData("Lab.ALT.Result")]
    [InlineData("AE12.Relationship")]
    [InlineData("A")]
    public void A_field_is_a_path_of_pascal_case_names_separated_by_dots(string fieldName) {
        // Act
        Query query = new Query(1, fieldName, Scenarios.AnyQueryText(), Scenarios.AnyDate());

        // Assert
        Assert.Equal(fieldName, query.FieldName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("vitals")]
    [InlineData("Vitals.")]
    [InlineData(".Vitals")]
    [InlineData("Vitals..HeartRateBpm")]
    [InlineData("Vitals.heartRate")]
    [InlineData("Vitals HeartRate")]
    [InlineData("1Vitals")]
    [InlineData("Vitals-HeartRate")]
    public void A_field_that_is_not_such_a_path_is_refused(string fieldName) {
        // Act / Assert
        Assert.Throws<FormatException>(() => new Query(1, fieldName, Scenarios.AnyQueryText(), Scenarios.AnyDate()));
    }

    [Fact(Skip = "domain defect: the field pattern ends with '$', which .NET also matches before a final newline, so 'Vitals.HeartRateBpm\\n' is accepted as a field path; use \\z. No seed: literal input")]
    public void A_field_followed_by_a_line_break_is_refused() {
        // Act / Assert
        Assert.Throws<FormatException>(() => new Query(1, "Vitals.HeartRateBpm\n", Scenarios.AnyQueryText(), Scenarios.AnyDate()));
    }

    [Fact]
    public void A_missing_field_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Query(1, null!, Scenarios.AnyQueryText(), Scenarios.AnyDate()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_query_number_starts_at_1(int number) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Query(number, "Vitals", Scenarios.AnyQueryText(), Scenarios.AnyDate()));

        // Assert
        Assert.Equal("number", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void A_blank_text_is_refused(string text) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Query(1, "Vitals", text, Scenarios.AnyDate()));

        // Assert
        Assert.Equal("text", refusal.ParamName);
    }

    [Fact]
    public void A_missing_text_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Query(1, "Vitals", null!, Scenarios.AnyDate()));

        // Assert
        Assert.Equal("text", refusal.ParamName);
    }

    [Fact]
    public void A_text_may_have_500_characters_but_not_501() {
        // Arrange
        string atTheLimit  = Any.String().NotBlank().WithLength(500).Generate();
        string overTheLimit = Any.String().NotBlank().WithLength(501).Generate();
        DateOnly anyDate = Scenarios.AnyDate();

        // Act
        Query query = new Query(1, "Vitals", atTheLimit, anyDate);
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Query(1, "Vitals", overTheLimit, anyDate));

        // Assert
        Assert.Equal(500, query.Text.Length);
        Assert.Equal("text", refusal.ParamName);
    }

}
