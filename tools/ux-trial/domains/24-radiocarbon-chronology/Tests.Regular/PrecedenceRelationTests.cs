using JustDummies;
using RadiocarbonChronology.Domain;
using RadiocarbonChronology.Tests.Regular.Generators;
using Xunit;

namespace RadiocarbonChronology.Tests.Regular;

public sealed class PrecedenceRelationTests {

    [Fact]
    public void A_relation_names_the_older_and_the_younger_layer() {
        // Arrange / Act
        PrecedenceRelation relation = new PrecedenceRelation("US2", "US1");

        // Assert
        Assert.Equal("US2", relation.OlderLayerCode);
        Assert.Equal("US1", relation.YoungerLayerCode);
    }

    [Fact]
    public void A_generated_relation_never_ties_a_layer_to_itself() {
        // Arrange
        PrecedenceRelation anyRelation = Any.PrecedenceRelation().Generate();

        // Act
        bool same = anyRelation.OlderLayerCode == anyRelation.YoungerLayerCode;

        // Assert
        Assert.False(same);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void A_blank_older_layer_code_is_refused(string? older) {
        // Arrange
        string anyYounger = Any.StringMatching("[A-Z]{1,3}[0-9]{1,4}").Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new PrecedenceRelation(older!, anyYounger));

        // Assert
        Assert.Equal("olderLayerCode", error.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void A_blank_younger_layer_code_is_refused(string? younger) {
        // Arrange
        string anyOlder = Any.StringMatching("[A-Z]{1,3}[0-9]{1,4}").Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new PrecedenceRelation(anyOlder, younger!));

        // Assert
        Assert.Equal("youngerLayerCode", error.ParamName);
    }

    [Fact]
    public void A_layer_cannot_precede_itself() {
        // Arrange
        string anyCode = Any.StringMatching("[A-Z]{1,3}[0-9]{1,4}").Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new PrecedenceRelation(anyCode, anyCode));

        // Assert
        Assert.Contains(anyCode, error.Message);
    }

    [Fact]
    public void The_comparison_of_layer_codes_is_case_sensitive() {
        // Arrange / Act
        PrecedenceRelation relation = new PrecedenceRelation("us1", "US1");

        // Assert
        Assert.NotEqual(relation.OlderLayerCode, relation.YoungerLayerCode);
    }

    [Fact]
    public void Two_relations_between_the_same_layers_in_the_same_order_are_equal() {
        // Arrange
        PrecedenceRelation anyRelation = Any.PrecedenceRelation().Generate();

        // Act
        PrecedenceRelation copy = new PrecedenceRelation(anyRelation.OlderLayerCode, anyRelation.YoungerLayerCode);

        // Assert
        Assert.Equal(anyRelation, copy);
        Assert.Equal(anyRelation.GetHashCode(), copy.GetHashCode());
    }

    [Fact]
    public void Reversing_a_relation_gives_a_different_one() {
        // Arrange
        PrecedenceRelation anyRelation = Any.PrecedenceRelation().Generate();

        // Act
        PrecedenceRelation reversed = new PrecedenceRelation(anyRelation.YoungerLayerCode, anyRelation.OlderLayerCode);

        // Assert
        Assert.NotEqual(anyRelation, reversed);
    }

    [Fact]
    public void A_relation_is_written_older_before_younger() {
        // Arrange
        PrecedenceRelation relation = new PrecedenceRelation("US5", "US4");

        // Act
        string text = relation.ToString();

        // Assert
        Assert.Equal("US5 < US4", text);
    }

}
