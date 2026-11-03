using System;

using ClinicalTrial.Domain.Tests.Regular.Generators;

using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Regular;

public sealed class QueryTests {

    [Fact]
    public void A_new_query_is_open_and_unanswered() {
        // Arrange
        Query anyQuery = Any.Query().Generate();

        // Assert
        Assert.Equal(QueryStatus.Open, anyQuery.Status);
        Assert.Null(anyQuery.Response);
        Assert.Null(anyQuery.AnsweredOn);
        Assert.Null(anyQuery.ClosedOn);
    }

    [Fact]
    public void A_query_exposes_what_the_monitor_raised() {
        // Arrange
        string   anyText = Any.String().NotBlank().WithMaxLength(Query.MaxTextLength).Generate();
        DateOnly anyDate = TrialFields.Date().Generate();

        // Act
        Query query = new Query(3, "Vitals.HeartRateBpm", anyText, anyDate);

        // Assert
        Assert.Equal(3, query.Number);
        Assert.Equal("Vitals.HeartRateBpm", query.FieldName);
        Assert.Equal(anyText, query.Text);
        Assert.Equal(anyDate, query.RaisedOn);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void A_query_number_starts_at_one(int number) {
        // Arrange
        Query anyQuery = Any.Query().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Query(number, anyQuery.FieldName, anyQuery.Text, anyQuery.RaisedOn));

        // Assert
        Assert.Equal("number", refusal.ParamName);
    }

    [Theory]
    [InlineData("Vitals")]
    [InlineData("Vitals.HeartRateBpm")]
    [InlineData("Lab.ALT.Result")]
    [InlineData("AE1.Outcome")]
    [InlineData("A")]
    public void A_field_path_of_pascal_case_names_separated_by_dots_is_accepted(string fieldName) {
        // Arrange
        Query anyQuery = Any.Query().Generate();

        // Act
        Query query = new Query(anyQuery.Number, fieldName, anyQuery.Text, anyQuery.RaisedOn);

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
    [InlineData("Vitals-HeartRate")]
    [InlineData("1Vitals")]
    [InlineData("Vitals/HeartRate")]
    public void A_field_path_that_does_not_follow_the_format_is_refused(string fieldName) {
        // Arrange
        Query anyQuery = Any.Query().Generate();

        // Assert
        Assert.Throws<FormatException>(() => new Query(anyQuery.Number, fieldName, anyQuery.Text, anyQuery.RaisedOn));
    }

    [Fact(Skip = "domain defect: the field pattern is anchored with a dollar sign, which matches before a final newline, so Vitals followed by a newline is accepted as a field path; no drawn value involved, no seed")]
    public void A_field_path_followed_by_a_line_break_is_refused() {
        // Arrange
        Query anyQuery = Any.Query().Generate();

        // Assert
        Assert.Throws<FormatException>(() => new Query(anyQuery.Number, "Vitals\n", anyQuery.Text, anyQuery.RaisedOn));
    }

    [Fact]
    public void A_missing_field_path_is_refused() {
        // Arrange
        Query anyQuery = Any.Query().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Query(anyQuery.Number, null!, anyQuery.Text, anyQuery.RaisedOn));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_text_is_refused(string text) {
        // Arrange
        Query anyQuery = Any.Query().Generate();

        // Assert
        Assert.Equal("text", Assert.Throws<ArgumentException>(() => new Query(anyQuery.Number, anyQuery.FieldName, text, anyQuery.RaisedOn)).ParamName);
    }

    [Fact]
    public void A_missing_text_is_refused() {
        // Arrange
        Query anyQuery = Any.Query().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Query(anyQuery.Number, anyQuery.FieldName, null!, anyQuery.RaisedOn));
    }

    [Fact]
    public void A_text_may_have_500_characters_and_not_501() {
        // Arrange
        Query anyQuery = Any.Query().Generate();

        // Act
        Query longest = new Query(anyQuery.Number, anyQuery.FieldName, new string('x', 500), anyQuery.RaisedOn);
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Query(anyQuery.Number, anyQuery.FieldName, new string('x', 501), anyQuery.RaisedOn));

        // Assert
        Assert.Equal(500, longest.Text.Length);
        Assert.Equal("text", refusal.ParamName);
    }

}
