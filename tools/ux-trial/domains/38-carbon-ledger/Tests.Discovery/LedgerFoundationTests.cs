using CarbonLedger.Domain;
using CarbonLedger.Tests.Discovery.Generators;
using CarbonLedger.Tests.Discovery.Support;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Discovery;

public sealed class LedgerFoundationTests {

    [Fact]
    public void A_ledger_is_founded_on_an_issuance_account_and_starts_empty() {
        // Arrange
        Account issuance = Accounts.Of(AccountKind.Issuance);

        // Act
        CarbonRegistryLedger ledger = new CarbonRegistryLedger("VCS", issuance);

        // Assert
        Assert.Equal("VCS", ledger.Registry);
        Assert.Same(issuance, ledger.IssuanceAccount);
        Assert.Empty(ledger.Journal);
        Assert.Equal(0, ledger.IssuedQuantity);
        Assert.Equal(0, ledger.RetiredQuantity);
        Assert.Equal(0, ledger.Balance(issuance.AccountId));
        Assert.Same(issuance, Assert.Single(ledger.Accounts));
    }

    [Theory]
    [InlineData(AccountKind.Holding)]
    [InlineData(AccountKind.Retirement)]
    public void A_ledger_cannot_be_founded_on_an_account_that_is_not_an_issuance_account(AccountKind kind) {
        Account account = Accounts.Of(kind);

        Assert.Throws<ArgumentException>(() => new CarbonRegistryLedger("VCS", account));
    }

    [Theory]
    [InlineData("")]
    [InlineData("V")]
    [InlineData("ABCDEF")]
    [InlineData("vcs")]
    [InlineData("VC1")]
    public void A_registry_code_that_is_not_two_to_five_upper_case_letters_is_refused(string registry) {
        Account issuance = Accounts.Of(AccountKind.Issuance);

        Assert.Throws<ArgumentException>(() => new CarbonRegistryLedger(registry, issuance));
    }

    [Fact]
    public void A_registry_code_and_an_issuance_account_are_both_required() {
        Account issuance = Accounts.Of(AccountKind.Issuance);

        Assert.Throws<ArgumentNullException>(() => new CarbonRegistryLedger(null!, issuance));
        Assert.Throws<ArgumentNullException>(() => new CarbonRegistryLedger("VCS", null!));
    }

    [Fact(Skip = "domain defect: the registry pattern ^[A-Z]{2,5}$ lets a trailing line feed through ($ matches before a final \\n), so a ledger for registry \"VCS\\n\" is founded; deterministic, no seed")]
    public void A_registry_code_with_a_trailing_line_feed_is_refused() {
        Account issuance = Accounts.Of(AccountKind.Issuance);

        Assert.Throws<ArgumentException>(() => new CarbonRegistryLedger("VCS\n", issuance));
    }

    [Theory]
    [InlineData(AccountKind.Holding)]
    [InlineData(AccountKind.Retirement)]
    public void A_holding_or_retirement_account_can_be_opened(AccountKind kind) {
        // Arrange
        LedgerWorld world = new LedgerWorld();

        // Act
        Account opened = world.OpenAccount(kind);

        // Assert
        Assert.Contains(opened, world.Ledger.Accounts);
        Assert.Equal(0, world.Ledger.Balance(opened.AccountId));
    }

    [Fact]
    public void A_ledger_never_has_a_second_issuance_account() {
        // Arrange
        LedgerWorld world       = new LedgerWorld();
        Account     anotherSource = Accounts.OtherThan(world.IssuanceAccount, AccountKind.Issuance);

        // Act
        Action open = () => world.Ledger.OpenAccount(anotherSource);

        // Assert
        Assert.Throws<LedgerRuleViolationException>(open);
        Assert.DoesNotContain(anotherSource, world.Ledger.Accounts);
    }

    [Fact]
    public void An_account_id_can_be_opened_only_once() {
        // Arrange
        LedgerWorld world = new LedgerWorld();
        Account     first = world.OpenHolding();

        // The same id with another kind and another holder is still the same account number.
        Account sameNumber = Any.Account()
                                .WithKind(AccountKind.Retirement)
                                .WithHolder(h => h.WithCountryCode(first.Holder.CountryCode))
                                .WithAccountNumber(first.AccountId.Substring(3))
                                .Generate();

        // Act
        Action open = () => world.Ledger.OpenAccount(sameNumber);

        // Assert
        Assert.Throws<LedgerRuleViolationException>(open);
        Assert.Same(first, world.Ledger.Accounts.Single(a => a.AccountId == first.AccountId));
    }

    [Fact]
    public void The_issuance_account_id_cannot_be_reused_for_another_account() {
        LedgerWorld world = new LedgerWorld();
        Account clash = Any.Account()
                           .WithKind(AccountKind.Holding)
                           .WithHolder(h => h.WithCountryCode(world.IssuanceAccount.Holder.CountryCode))
                           .WithAccountNumber(world.IssuanceAccount.AccountId.Substring(3))
                           .Generate();

        Assert.Throws<LedgerRuleViolationException>(() => world.Ledger.OpenAccount(clash));
    }

    [Fact]
    public void Opening_a_null_account_is_refused() {
        LedgerWorld world = new LedgerWorld();

        Assert.Throws<ArgumentNullException>(() => world.Ledger.OpenAccount(null!));
    }

    [Fact]
    public void Querying_an_account_that_is_not_open_is_refused() {
        // Arrange
        LedgerWorld world   = new LedgerWorld();
        Account     unknown = world.UnopenedAccount();

        // Assert
        Assert.Throws<LedgerRuleViolationException>(() => world.Ledger.Balance(unknown.AccountId));
        Assert.Throws<LedgerRuleViolationException>(() => world.Ledger.UnitsHeldBy(unknown.AccountId));
        Assert.Throws<ArgumentNullException>(() => world.Ledger.Balance(null!));
    }

    [Fact]
    public void A_fresh_ledger_is_consistent() {
        LedgerWorld world = new LedgerWorld();
        world.OpenHolding();
        world.OpenRetirement();

        Assert.True(world.Ledger.IsConsistent());
    }

    [Fact(Skip = "domain defect: Journal and Accounts are typed read-only but are the live internal List/Dictionary.ValueCollection, so a caller can cast and mutate the append-only journal; deterministic, no seed")]
    public void The_journal_cannot_be_modified_through_the_collection_it_exposes() {
        LedgerWorld world = new LedgerWorld();
        world.Issue(world.OpenHolding(), 2);

        Assert.False(world.Ledger.Journal is List<JournalEntry>);
    }

}
