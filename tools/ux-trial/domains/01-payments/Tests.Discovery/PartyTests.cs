using JustDummies;

using Payments.Domain;

using Xunit;

namespace Payments.Tests;

public sealed class PartyTests {

    [Fact]
    public void A_party_keeps_its_name_its_account_and_its_agent() {
        // Arrange
        Iban anyAccount = Any.Iban().InCountry("FR").Generate();
        Bic  anyAgent   = Any.Bic().InCountry("FR").Generate();

        // Act
        Party party = new Party("Alice Martin", anyAccount, anyAgent);

        // Assert
        Assert.Equal("Alice Martin", party.Name);
        Assert.Same(anyAccount, party.Account);
        Assert.Same(anyAgent, party.Agent);
    }

    [Fact]
    public void The_agent_is_optional() {
        // Arrange
        Iban anyAccount = Any.Iban().Generate();

        // Act
        Party party = new Party("Alice Martin", anyAccount, null);

        // Assert
        Assert.Null(party.Agent);
    }

    [Fact]
    public void The_name_is_trimmed() {
        // Arrange
        Iban anyAccount = Any.Iban().Generate();

        // Act
        Party party = new Party("  Alice Martin \t", anyAccount, null);

        // Assert
        Assert.Equal("Alice Martin", party.Name);
    }

    [Fact]
    public void A_name_of_70_characters_is_accepted() {
        // Arrange
        string anyName70 = Any.String().Alpha().WithLength(70).Generate();
        Iban   anyAccount = Any.Iban().Generate();

        // Act & Assert
        Assert.Equal(70, new Party(anyName70, anyAccount, null).Name.Length);
    }

    [Fact]
    public void A_name_of_71_characters_is_refused() {
        // Arrange
        string anyName71  = Any.String().Alpha().WithLength(71).Generate();
        Iban   anyAccount = Any.Iban().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Party(anyName71, anyAccount, null));
    }

    [Fact]
    public void The_length_limit_applies_once_trimmed() {
        // Arrange
        string anyName70  = Any.String().Alpha().WithLength(70).Generate();
        Iban   anyAccount = Any.Iban().Generate();

        // Act
        Party party = new Party("   " + anyName70 + "   ", anyAccount, null);

        // Assert
        Assert.Equal(70, party.Name.Length);
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData(" \t\n ")]
    public void A_name_empty_once_trimmed_is_refused(string blank) {
        // Arrange
        Iban anyAccount = Any.Iban().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Party(blank, anyAccount, null));
    }

    [Fact]
    public void A_null_name_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Party(null!, Any.Iban().Generate(), null));
    }

    [Fact]
    public void A_null_account_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Party(Any.String().Alpha().WithLengthBetween(1, 70).Generate(), null!, null));
    }

    [Fact]
    public void An_agent_in_another_country_than_the_account_is_refused() {
        // Arrange
        Iban anyFrenchAccount = Any.Iban().InCountry("FR").Generate();
        Bic  anyGermanAgent   = Any.Bic().InCountry("DE").Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Party("Alice Martin", anyFrenchAccount, anyGermanAgent));

        // Assert
        Assert.Equal("agent", refusal.ParamName);
    }

    [Fact]
    public void A_generated_party_has_an_agent_in_its_account_country_or_none() {
        // Arrange
        Party anyParty = Any.Party().Generate();

        // Assert
        Assert.True(anyParty.Agent is null || anyParty.Agent.CountryCode == anyParty.Account.CountryCode);
    }

}
