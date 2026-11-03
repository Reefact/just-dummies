using JustDummies;

using Xunit;

namespace Payments.Domain.Tests.Expert;

public sealed class PartyTests {

    [Fact]
    public void A_party_keeps_its_name_its_account_and_its_agent() {
        // Arrange
        Iban account = Any.Iban().InCountry("FR").Generate();
        Bic  agent   = Any.Bic().InCountry("FR").Generate();

        // Act
        Party party = new Party("Alice Martin", account, agent);

        // Assert
        Assert.Equal("Alice Martin", party.Name);
        Assert.Same(account, party.Account);
        Assert.Same(agent, party.Agent);
    }

    [Fact]
    public void The_agent_is_optional() {
        // Arrange
        string anyName    = AnyParty.ValidNames().Generate();
        Iban   anyAccount = Any.Iban().Generate();

        // Act
        Party party = new Party(anyName, anyAccount, null);

        // Assert
        Assert.Null(party.Agent);
    }

    [Theory]
    [InlineData("  Alice  ", "Alice")]
    [InlineData("\tAlice\n", "Alice")]
    [InlineData("Alice Martin ", "Alice Martin")]
    [InlineData(" A", "A")]
    public void The_name_is_trimmed(string given, string expectedName) {
        // Arrange
        Iban anyAccount = Any.Iban().Generate();

        // Act
        Party party = new Party(given, anyAccount, null);

        // Assert
        Assert.Equal(expectedName, party.Name);
    }

    [Fact]
    public void A_name_of_70_characters_is_accepted() {
        // Arrange
        string anyMaximalName = Any.String().Alpha().WithLength(70).Generate();
        Iban   anyAccount     = Any.Iban().Generate();

        // Act
        Party party = new Party(anyMaximalName, anyAccount, null);

        // Assert
        Assert.Equal(70, party.Name.Length);
    }

    [Fact]
    public void A_name_of_71_characters_is_refused() {
        // Arrange
        string anyTooLongName = Any.String().Alpha().WithLength(71).Generate();
        Iban   anyAccount     = Any.Iban().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Party(anyTooLongName, anyAccount, null));

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void The_length_limit_applies_to_the_trimmed_name() {
        // Arrange
        string anyMaximalName = Any.String().Alpha().WithLength(70).Generate();
        Iban   anyAccount     = Any.Iban().Generate();

        // Act
        Party party = new Party("  " + anyMaximalName + "\t ", anyAccount, null);

        // Assert
        Assert.Equal(anyMaximalName, party.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t\r\n ")]
    public void A_name_that_is_empty_once_trimmed_is_refused(string blank) {
        // Arrange
        Iban anyAccount = Any.Iban().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Party(blank, anyAccount, null));

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void A_null_name_is_refused_as_a_null() {
        // Arrange
        Iban anyAccount = Any.Iban().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Party(null!, anyAccount, null));

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Fact]
    public void A_null_account_is_refused_as_a_null() {
        // Arrange
        string anyName = AnyParty.ValidNames().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Party(anyName, null!, null));

        // Assert
        Assert.Equal("account", refusal.ParamName);
    }

    [Fact]
    public void An_agent_of_another_country_than_the_account_is_refused() {
        // Arrange
        string anyName    = AnyParty.ValidNames().Generate();
        Iban   frAccount  = Any.Iban().InCountry("FR").Generate();
        Bic    deAgent    = Any.Bic().InCountry("DE").Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Party(anyName, frAccount, deAgent));

        // Assert
        Assert.Equal("agent", refusal.ParamName);
    }

    [Fact]
    public void An_agent_of_the_accounts_country_is_accepted() {
        // Arrange
        string anyName   = AnyParty.ValidNames().Generate();
        Iban   frAccount = Any.Iban().InCountry("FR").Generate();
        Bic    frAgent   = Any.Bic().InCountry("FR").Generate();

        // Act
        Party party = new Party(anyName, frAccount, frAgent);

        // Assert
        Assert.Same(frAgent, party.Agent);
    }

    [Fact]
    public void A_name_too_long_is_reported_before_an_agent_of_another_country() {
        // Arrange
        string anyTooLongName = Any.String().Alpha().WithLength(71).Generate();
        Iban   frAccount      = Any.Iban().InCountry("FR").Generate();
        Bic    deAgent        = Any.Bic().InCountry("DE").Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Party(anyTooLongName, frAccount, deAgent));

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

}
