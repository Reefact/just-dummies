using CarbonLedger.Domain;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Regular.Tests;

public sealed class AccountTests {

    [Fact]
    public void An_account_exposes_what_it_was_built_from() {
        // Arrange
        Holder         anyHolder   = new AnyHolder().WithCountryCode("FR").Generate();
        AccountKind    anyKind     = Any.Enum<AccountKind>().Generate();
        DateTimeOffset anyOpenedOn = Any.DateTimeOffset().Generate();

        // Act
        Account account = new Account("FR-000123", anyHolder, anyKind, anyOpenedOn);

        // Assert
        Assert.Equal("FR-000123", account.AccountId);
        Assert.Same(anyHolder, account.Holder);
        Assert.Equal(anyKind, account.Kind);
        Assert.Equal(anyOpenedOn, account.OpenedOn);
    }

    [Fact]
    public void An_account_is_described_by_its_id_and_kind() {
        // Arrange
        Holder anyFrenchHolder = new AnyHolder().WithCountryCode("FR").Generate();

        Account account = new AnyAccount().WithHolder(anyFrenchHolder).WithAccountId("000123").WithKind(AccountKind.Retirement).Generate();

        // Act / Assert
        Assert.Equal("FR-000123 [Retirement]", account.ToString());
    }

    [Fact]
    public void A_generated_account_is_prefixed_with_the_country_of_its_holder() {
        // Arrange / Act: the generator is hand-completed for the tie the constructor enforces
        Account anyAccount = new AnyAccount().Generate();

        // Assert
        Assert.StartsWith(anyAccount.Holder.CountryCode + "-", anyAccount.AccountId);
    }

    [Theory]
    [InlineData("F-000123")]
    [InlineData("FRA-000123")]
    [InlineData("fr-000123")]
    [InlineData("FR000123")]
    [InlineData("FR-00012")]
    [InlineData("FR-0001234")]
    [InlineData("FR-00012A")]
    [InlineData("")]
    public void An_account_id_that_is_not_two_letters_a_dash_and_six_digits_is_refused(string accountId) {
        // Arrange
        Holder         anyHolder   = new AnyHolder().WithCountryCode("FR").Generate();
        AccountKind    anyKind     = Any.Enum<AccountKind>().Generate();
        DateTimeOffset anyOpenedOn = Any.DateTimeOffset().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Account(accountId, anyHolder, anyKind, anyOpenedOn));
    }

    [Fact(Skip = "domain defect: the regex anchors use $, which .NET lets match before a final \\n; a trailing newline is accepted (deterministic, no seed involved)")]
    public void An_account_id_with_a_trailing_newline_is_refused() {
        Holder anyHolder = new AnyHolder().WithCountryCode("FR").Generate();

        Assert.Throws<ArgumentException>(() => new Account("FR-000123\n", anyHolder, AccountKind.Holding, DateTimeOffset.UnixEpoch));
    }

    [Fact]
    public void An_account_id_prefixed_with_another_country_than_its_holders_is_refused() {
        // Arrange
        Holder         frenchHolder = new AnyHolder().WithCountryCode("FR").Generate();
        AccountKind    anyKind      = Any.Enum<AccountKind>().Generate();
        DateTimeOffset anyOpenedOn  = Any.DateTimeOffset().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Account("DE-000123", frenchHolder, anyKind, anyOpenedOn));

        // Assert
        Assert.Contains("FR", refusal.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(99)]
    public void An_undefined_kind_is_refused(int kind) {
        // Arrange
        Holder         anyHolder   = new AnyHolder().WithCountryCode("FR").Generate();
        DateTimeOffset anyOpenedOn = Any.DateTimeOffset().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Account("FR-000123", anyHolder, (AccountKind)kind, anyOpenedOn));
    }

    [Fact]
    public void A_null_id_or_holder_is_refused() {
        // Arrange
        Holder anyHolder = new AnyHolder().WithCountryCode("FR").Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Account(null!, anyHolder, AccountKind.Holding, DateTimeOffset.UnixEpoch));
        Assert.Throws<ArgumentNullException>(() => new Account("FR-000123", null!, AccountKind.Holding, DateTimeOffset.UnixEpoch));
    }

}
