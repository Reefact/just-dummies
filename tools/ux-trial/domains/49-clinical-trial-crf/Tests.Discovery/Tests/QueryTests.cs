using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests;

public sealed class QueryTests {

    [Fact]
    public void A_new_query_is_open_and_unanswered() {
        // Arrange
        DateOnly raisedOn = new DateOnly(2024, 4, 15);

        // Act
        Query query = Any.Query().WithNumber(3).WithFieldName("Vitals.HeartRateBpm").WithRaisedOn(raisedOn).Generate();

        // Assert
        Assert.Equal(3, query.Number);
        Assert.Equal("Vitals.HeartRateBpm", query.FieldName);
        Assert.Equal(raisedOn, query.RaisedOn);
        Assert.Equal(QueryStatus.Open, query.Status);
        Assert.Null(query.Response);
        Assert.Null(query.AnsweredOn);
        Assert.Null(query.ClosedOn);
    }

    [Fact]
    public void A_query_number_starts_at_1() {
        // Arrange
        string   anyField = "Vitals";
        string   anyText  = Any.String().NotBlank().WithMaxLength(Query.MaxTextLength).Generate();
        DateOnly anyDate  = TrialCalendar.Date().Generate();

        // Act + Assert
        Assert.Equal(1, new Query(1, anyField, anyText, anyDate).Number);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Query(0, anyField, anyText, anyDate));
    }

    [Theory]
    [InlineData("Vitals")]
    [InlineData("Vitals.HeartRateBpm")]
    [InlineData("Lab.ALT.Result")]
    [InlineData("A1.B2.C3")]
    public void A_field_path_is_PascalCase_names_separated_by_dots(string field) {
        Assert.Equal(field, Any.Query().WithFieldName(field).Generate().FieldName);
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
    public void A_field_that_is_not_a_PascalCase_path_is_refused(string field) {
        // Arrange
        string   anyText = Any.String().NotBlank().WithMaxLength(Query.MaxTextLength).Generate();
        DateOnly anyDate = TrialCalendar.Date().Generate();

        // Act + Assert
        Assert.Throws<FormatException>(() => new Query(1, field, anyText, anyDate));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_query_text_is_not_blank(string text) {
        // Arrange
        DateOnly anyDate = TrialCalendar.Date().Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new Query(1, "Vitals", text, anyDate));
    }

    [Fact]
    public void A_query_text_has_at_most_500_characters() {
        // Arrange
        DateOnly anyDate = TrialCalendar.Date().Generate();

        // Act + Assert
        Assert.Equal(500, new Query(1, "Vitals", new string('q', 500), anyDate).Text.Length);
        Assert.Throws<ArgumentException>(() => new Query(1, "Vitals", new string('q', 501), anyDate));
    }

}
