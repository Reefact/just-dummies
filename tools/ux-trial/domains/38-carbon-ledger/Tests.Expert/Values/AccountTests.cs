using CarbonLedger.Domain;
using CarbonLedger.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Expert.Values;

public sealed class AccountTests {

    [Fact]
    public void An_account_keeps_what_it_was_opened_with() {
        // Arrange
        Holder         holder   = Any.Holder().WithCountryCode("FR").Generate();
        AccountKind    anyKind  = Any.Enum<AccountKind>().Generate();
        DateTimeOffset anyOpened = Any.DateTimeOffset().Generate();

        // Act
        Account account = new Account("FR-000123", holder, anyKind, anyOpened);

        // Assert
        Assert.Equal("FR-000123", account.AccountId);
        Assert.Same(holder, account.Holder);
    }

    [Fact]
    public void An_account_keeps_the_kind_and_the_opening_instant_it_was_given() {
        // Arrange
        Holder holder = Any.Holder().WithCountryCode("FR").Generate();
        DateTimeOffset opened = new DateTimeOffset(2024, 3, 5, 10, 30, 0, TimeSpan.FromHours(1));

        // Act
        Account account = new Account("FR-000123", holder, AccountKind.Retirement, opened);

        // Assert
        Assert.Equal(AccountKind.Retirement, account.Kind);
        Assert.Equal(opened, account.OpenedOn);
        Assert.Equal(opened.Offset, account.OpenedOn.Offset);
    }

    [Theory]
    [InlineData(AccountKind.Issuance)]
    [InlineData(AccountKind.Holding)]
    [InlineData(AccountKind.Retirement)]
    public void An_account_prints_its_id_and_its_kind(AccountKind kind) {
        Account account = Any.Account().WithAccountNumber("000123").WithHolder(holder => holder.WithCountryCode("FR")).WithKind(kind).Generate();

        Assert.Equal($"FR-000123 [{kind}]", account.ToString());
    }

    [Theory]
    [InlineData("FR-00123")]
    [InlineData("FR-0001234")]
    [InlineData("FR000123")]
    [InlineData("FR_000123")]
    [InlineData("fr-000123")]
    [InlineData("FRA-000123")]
    [InlineData("F-0000123")]
    [InlineData("FR-00012A")]
    [InlineData("FR-000123 ")]
    [InlineData("")]
    public void An_account_number_that_is_not_two_capitals_a_dash_and_six_digits_is_refused(string accountId) {
        // Arrange
        Holder anyFrenchHolder = Any.Holder().WithCountryCode("FR").Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Account(accountId, anyFrenchHolder, Any.Enum<AccountKind>().Generate(), Any.DateTimeOffset().Generate()));

        // Assert
        Assert.Equal("accountId", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: the account pattern '^[A-Z]{2}-\\d{6}$' lets a final newline through ('$' matches before it), so 'FR-000123\\n' is an account id; literal input, no seed")]
    public void An_account_number_followed_by_a_line_break_is_refused() {
        Holder anyFrenchHolder = Any.Holder().WithCountryCode("FR").Generate();

        Assert.Throws<ArgumentException>(() => new Account("FR-000123\n", anyFrenchHolder, AccountKind.Holding, Any.DateTimeOffset().Generate()));
    }

    [Fact(Skip = "domain defect (low): '\\d' in .NET also matches non-ASCII decimal digits, so 'FR-\\u0661\\u0662\\u0663\\u0664\\u0665\\u0666' (Arabic-Indic) is accepted as an account id although MODEL.md says six digits; literal input, no seed")]
    public void An_account_number_written_with_non_ascii_digits_is_refused() {
        Holder anyFrenchHolder = Any.Holder().WithCountryCode("FR").Generate();

        Assert.Throws<ArgumentException>(() => new Account("FR-١٢٣٤٥٦", anyFrenchHolder, AccountKind.Holding, Any.DateTimeOffset().Generate()));
    }

    [Fact]
    public void An_account_number_must_start_with_the_country_of_its_holder() {
        // Arrange
        Holder germanHolder = Any.Holder().WithCountryCode("DE").Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Account("FR-000123", germanHolder, AccountKind.Holding, Any.DateTimeOffset().Generate()));

        // Assert
        Assert.Equal("accountId", refusal.ParamName);
        Assert.Contains("DE", refusal.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(int.MaxValue)]
    public void An_undeclared_kind_is_refused(int kind) {
        Holder anyHolder = Any.Holder().Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Account(anyHolder.CountryCode + "-000123", anyHolder, (AccountKind)kind, Any.DateTimeOffset().Generate()));

        Assert.Equal("kind", refusal.ParamName);
    }

    [Fact]
    public void A_missing_account_number_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Account(null!, Any.Holder().Generate(), AccountKind.Holding, Any.DateTimeOffset().Generate()));
    }

    [Fact]
    public void A_missing_holder_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Account("FR-000123", null!, AccountKind.Holding, Any.DateTimeOffset().Generate()));
    }

}
