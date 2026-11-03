using JustDummies;

using RadiocarbonChronology.Domain;

namespace RadiocarbonChronology.Tests.Expert;

public sealed class PrecedenceRelationTests {

    [Fact]
    public void A_relation_names_the_older_layer_and_the_younger_one() {
        // Act
        PrecedenceRelation relation = new PrecedenceRelation("US12", "US7");

        // Assert
        Assert.Equal("US12", relation.OlderLayerCode);
        Assert.Equal("US7", relation.YoungerLayerCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_older_layer_code_is_refused(string? blank) {
        // Arrange
        string anyYoungerCode = Any.StratigraphicLayer().Generate().Code;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PrecedenceRelation(blank!, anyYoungerCode));

        // Assert
        Assert.Equal("olderLayerCode", refusal.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_younger_layer_code_is_refused(string? blank) {
        // Arrange
        string anyOlderCode = Any.StratigraphicLayer().Generate().Code;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PrecedenceRelation(anyOlderCode, blank!));

        // Assert
        Assert.Equal("youngerLayerCode", refusal.ParamName);
    }

    [Fact]
    public void A_layer_cannot_precede_itself() {
        // Arrange
        string anyCode = Any.StratigraphicLayer().Generate().Code;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PrecedenceRelation(anyCode, anyCode));

        // Assert
        Assert.Equal("youngerLayerCode", refusal.ParamName);
    }

    [Fact]
    public void Layer_codes_are_compared_with_their_case() {
        // Act
        PrecedenceRelation relation = new PrecedenceRelation("US1", "us1");

        // Assert
        Assert.Equal("us1", relation.YoungerLayerCode);
    }

    [Fact]
    public void Two_relations_between_the_same_layers_in_the_same_direction_are_equal() {
        // Arrange
        PrecedenceRelation relation     = new PrecedenceRelation("US12", "US7");
        PrecedenceRelation sameRelation = new PrecedenceRelation("US12", "US7");

        // Assert
        Assert.Equal(relation, sameRelation);
        Assert.Equal(relation.GetHashCode(), sameRelation.GetHashCode());
    }

    [Fact]
    public void The_opposite_succession_is_a_different_relation() {
        // Arrange
        PrecedenceRelation relation = new PrecedenceRelation("US12", "US7");

        // Assert
        Assert.NotEqual(relation, new PrecedenceRelation("US7", "US12"));
    }

    [Fact]
    public void A_relation_reads_with_the_older_layer_first() {
        // Assert
        Assert.Equal("US12 < US7", new PrecedenceRelation("US12", "US7").ToString());
    }

}
