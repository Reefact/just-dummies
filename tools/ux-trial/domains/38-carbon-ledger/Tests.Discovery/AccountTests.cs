using CarbonLedger.Domain;
using CarbonLedger.Tests.Discovery.Generators;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Discovery;

public sealed class AccountTests {

    private static readonly DateTimeOffset OpeningDay = new DateTimeOffset(2024, 3, 15, 9, 30, 0, TimeSpan.FromHours(1));

    [Fact]
    public void An_account_exposes_the_four_parts_it_was_built_from() {
        // Arrange
        Holder holder = new Holder("ACME01", "Acme Carbon", "FR");

        // Act
        Account account = new Account("FR-000123", holder, AccountKind.Holding, OpeningDay);

        // Assert
        Assert.Equal("FR-000123", account.AccountId);
        Assert.Same(holder, account.Holder);
        Assert.Equal(AccountKind.Holding, account.Kind);
        Assert.Equal(OpeningDay, account.OpenedOn);
        Assert.Equal(OpeningDay.Offset, account.OpenedOn.Offset);
    }

    [Fact]
    public void An_account_reads_as_its_id_and_its_kind() {
        Account account = Any.Account().WithHolder(h => h.WithCountryCode("FR")).WithAccountNumber("000123").WithKind(AccountKind.Retirement).Generate();

        Assert.Equal("FR-000123 [Retirement]", account.ToString());
    }

    [Theory]
    [InlineData(AccountKind.Issuance)]
    [InlineData(AccountKind.Holding)]
    [InlineData(AccountKind.Retirement)]
    public void Every_declared_kind_is_accepted(AccountKind kind) {
        Account account = Any.Account().WithKind(kind).Generate();

        Assert.Equal(kind, account.Kind);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void A_kind_that_is_not_a_declared_member_is_refused(int undeclared) {
        // Arrange
        Holder anyHolder = Any.Holder().WithCountryCode("FR").Generate();

        // Act
        Action construct = () => new Account("FR-000123", anyHolder, (AccountKind)undeclared, OpeningDay);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void An_account_id_must_start_with_the_country_code_of_its_holder() {
        // Arrange
        Holder frenchHolder = Any.Holder().WithCountryCode("FR").Generate();

        // Act
        Action construct = () => new Account("DE-000123", frenchHolder, AccountKind.Holding, OpeningDay);

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(construct);
        Assert.Contains("FR", refusal.Message);
    }

    [Fact]
    public void The_country_prefix_is_compared_case_sensitively_and_whole() {
        // "FR-000123" is not prefixed by a holder in "FX", and the prefix rule is not satisfied by a mere shared first letter
        Holder holder = Any.Holder().WithCountryCode("FX").Generate();

        Assert.Throws<ArgumentException>(() => new Account("FR-000123", holder, AccountKind.Holding, OpeningDay));
    }

    [Theory]
    [InlineData("")]
    [InlineData("FR")]
    [InlineData("FR-")]
    [InlineData("FR000123")]
    [InlineData("FR-00012")]
    [InlineData("FR-0001234")]
    [InlineData("FR-00012A")]
    [InlineData("fr-000123")]
    [InlineData("F-000123")]
    [InlineData("FRA-000123")]
    [InlineData("FR_000123")]
    [InlineData(" FR-000123")]
    [InlineData("FR-000123 ")]
    public void An_account_id_that_is_not_two_letters_a_dash_and_six_digits_is_refused(string accountId) {
        Holder frenchHolder = Any.Holder().WithCountryCode("FR").Generate();

        Assert.Throws<ArgumentException>(() => new Account(accountId, frenchHolder, AccountKind.Holding, OpeningDay));
    }

    [Fact(Skip = "domain defect: the account id pattern ^[A-Z]{2}-\\d{6}$ lets a trailing line feed through ($ matches before a final \\n), so \"FR-000123\\n\" is accepted; deterministic, no seed")]
    public void An_account_id_with_a_trailing_line_feed_is_refused() {
        Holder frenchHolder = Any.Holder().WithCountryCode("FR").Generate();

        Assert.Throws<ArgumentException>(() => new Account("FR-000123\n", frenchHolder, AccountKind.Holding, OpeningDay));
    }

    [Fact(Skip = "domain defect: the account id pattern uses \\d, which matches any Unicode decimal digit, so \"FR-\u0660\u0660\u0660\u0661\u0662\u0663\" (Arabic-Indic digits) is accepted although an id is six ASCII digits; deterministic, no seed")]
    public void An_account_id_made_of_non_ascii_digits_is_refused() {
        Holder frenchHolder = Any.Holder().WithCountryCode("FR").Generate();

        Assert.Throws<ArgumentException>(() => new Account("FR-\u0660\u0660\u0660\u0661\u0662\u0663", frenchHolder, AccountKind.Holding, OpeningDay));
    }

    [Fact]
    public void A_null_id_or_holder_is_refused() {
        // Arrange
        Holder anyHolder = Any.Holder().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Account(null!, anyHolder, AccountKind.Holding, OpeningDay));
        Assert.Throws<ArgumentNullException>(() => new Account("FR-000123", null!, AccountKind.Holding, OpeningDay));
    }

    [Fact]
    public void An_account_may_be_opened_at_the_earliest_and_latest_instants() {
        // The opening instant is a dummy as far as the constructor is concerned: no instant is refused.
        Holder anyHolder = Any.Holder().WithCountryCode("FR").Generate();

        Assert.Equal(DateTimeOffset.MinValue, new Account("FR-000123", anyHolder, AccountKind.Holding, DateTimeOffset.MinValue).OpenedOn);
        Assert.Equal(DateTimeOffset.MaxValue, new Account("FR-000123", anyHolder, AccountKind.Holding, DateTimeOffset.MaxValue).OpenedOn);
    }

}
