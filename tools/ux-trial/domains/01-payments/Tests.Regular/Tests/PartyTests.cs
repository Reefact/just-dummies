using JustDummies;

using Xunit;

namespace Payments.Domain.Tests;

public sealed class PartyTests {

    [Fact]
    public void A_party_keeps_its_account_and_its_agent() {
        // Arrange
        string anyName    = Any.String().Alpha().WithLengthBetween(1, 70).Generate();
        Iban   anyAccount = Any.Iban().InCountry("DE").Generate();
        Bic    anyAgent   = Any.Bic().InCountry("DE").Generate();

        // Act
        Party party = new Party(anyName, anyAccount, anyAgent);

        // Assert
        Assert.Equal(anyName, party.Name);
        Assert.Same(anyAccount, party.Account);
        Assert.Same(anyAgent, party.Agent);
    }

    [Fact]
    public void The_agent_is_optional() {
        // Arrange
        Iban anyAccount = Any.Iban().Generate();

        // Act
        Party party = new Party("Alice", anyAccount, null);

        // Assert
        Assert.Null(party.Agent);
    }

    [Fact]
    public void The_name_is_trimmed() {
        // Arrange
        string anyName = Any.String().Alpha().WithLengthBetween(1, 70).Generate();

        // Act
        Party party = new Party("  \t" + anyName + " \n ", Any.Iban().Generate(), null);

        // Assert
        Assert.Equal(anyName, party.Name);
    }

    [Fact]
    public void A_name_of_70_characters_is_accepted_even_when_padded() {
        // Arrange
        string anyMaximalName = Any.String().Alpha().WithLength(70).Generate();

        // Act
        Party party = new Party("   " + anyMaximalName + "   ", Any.Iban().Generate(), null);

        // Assert
        Assert.Equal(70, party.Name.Length);
    }

    [Fact]
    public void A_name_of_71_characters_is_refused() {
        // Arrange
        string anyExcessiveName = Any.String().Alpha().WithLength(71).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Party(anyExcessiveName, Any.Iban().Generate(), null));

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" \t\n ")]
    public void A_name_that_is_empty_once_trimmed_is_refused(string name) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Party(name, Any.Iban().Generate(), null));

        // Assert
        Assert.Equal("name", refusal.ParamName);
        Assert.IsNotType<ArgumentNullException>(refusal);
    }

    [Fact]
    public void A_null_name_is_refused_as_null() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Party(null!, Any.Iban().Generate(), null));

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void A_null_account_is_refused_as_null() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Party("Alice", null!, null));

        // Assert
        Assert.Equal("account", refusal.ParamName);
    }

    [Fact]
    public void The_name_is_checked_before_the_account() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Party(null!, null!, null));

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void An_agent_of_another_country_than_the_account_is_refused() {
        // Arrange
        Iban anyGermanAccount = Any.Iban().InCountry("DE").Generate();
        Bic  anyFrenchAgent   = Any.Bic().InCountry("FR").Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Party("Alice", anyGermanAccount, anyFrenchAgent));

        // Assert
        Assert.Equal("agent", refusal.ParamName);
    }

    [Fact]
    public void An_agent_of_the_account_country_is_accepted_whatever_the_branch() {
        // Arrange
        Iban anyAccount = Any.Iban().InCountry("BE").Generate();
        Bic  anyAgent   = Any.Bic().InCountry("BE").Generate();

        // Act
        Party party = new Party("Alice", anyAccount, anyAgent);

        // Assert
        Assert.Equal(party.Account.CountryCode, party.Agent!.CountryCode);
    }

    [Fact]
    public void A_drawn_party_is_always_valid() {
        // Arrange & Act
        Party anyParty = Any.Party().Generate();

        // Assert
        Assert.True(anyParty.Agent is null || anyParty.Agent.CountryCode == anyParty.Account.CountryCode);
        Assert.InRange(anyParty.Name.Length, 1, 70);
    }

}
