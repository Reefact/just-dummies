using ParimutuelBetting.Domain;

using Xunit;

namespace ParimutuelBetting.Tests.Regular;

public sealed class BetTypeRulesTests {

    [Theory]
    [InlineData(BetType.Win, 1)]
    [InlineData(BetType.Place, 1)]
    [InlineData(BetType.Quinella, 2)]
    [InlineData(BetType.Exacta, 2)]
    [InlineData(BetType.Trifecta, 3)]
    public void A_bet_type_names_as_many_horses_as_its_arity(BetType betType, int expectedArity) {
        // Act
        int arity = betType.Arity();

        // Assert
        Assert.Equal(expectedArity, arity);
    }

    [Theory]
    [InlineData(BetType.Win, true)]
    [InlineData(BetType.Place, true)]
    [InlineData(BetType.Quinella, false)]
    [InlineData(BetType.Exacta, true)]
    [InlineData(BetType.Trifecta, true)]
    public void Only_the_quinella_ignores_the_order_of_the_horses(BetType betType, bool expectedOrdered) {
        // Act
        bool ordered = betType.IsOrdered();

        // Assert
        Assert.Equal(expectedOrdered, ordered);
    }

    [Fact]
    public void An_undefined_bet_type_has_no_arity_and_no_order() {
        // Arrange
        BetType undefined = (BetType)99;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => undefined.Arity());
        Assert.Throws<ArgumentOutOfRangeException>(() => undefined.IsOrdered());
    }

}
