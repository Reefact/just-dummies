namespace RadiocarbonChronology.Tests.Discovery;

public sealed class PrecedenceRelationTests {

    [Fact]
    public void A_relation_names_the_older_and_the_younger_layer() {
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
    public void A_blank_older_layer_code_is_refused(string? olderLayerCode) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.PrecedenceRelation().WithOlderLayerCode(olderLayerCode!).Generate());

        // Assert
        Assert.Equal("olderLayerCode", refusal.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("\t")]
    public void A_blank_younger_layer_code_is_refused(string? youngerLayerCode) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.PrecedenceRelation().WithYoungerLayerCode(youngerLayerCode!).Generate());

        // Assert
        Assert.Equal("youngerLayerCode", refusal.ParamName);
    }

    [Fact]
    public void A_layer_cannot_precede_itself() {
        // Arrange
        string anyCode = Any.StratigraphicLayer().Generate().Code;

        // Assert
        Assert.Throws<ArgumentException>(() => new PrecedenceRelation(anyCode, anyCode));
    }

    [Fact]
    public void Layer_codes_are_compared_case_sensitively() {
        // Act
        PrecedenceRelation relation = new PrecedenceRelation("US1", "us1");

        // Assert
        Assert.Equal("US1", relation.OlderLayerCode);
    }

    [Fact]
    public void Two_relations_between_the_same_layers_in_the_same_order_are_equal() {
        // Arrange
        PrecedenceRelation relation         = new PrecedenceRelation("US2", "US1");
        PrecedenceRelation sameRelation     = new PrecedenceRelation("US2", "US1");
        PrecedenceRelation oppositeRelation = new PrecedenceRelation("US1", "US2");

        // Assert
        Assert.Equal(relation, sameRelation);
        Assert.Equal(relation.GetHashCode(), sameRelation.GetHashCode());
        Assert.NotEqual(relation, oppositeRelation);
        Assert.Equal("US2 < US1", relation.ToString());
    }

}
