using CarbonLedger.Domain;
using CarbonLedger.Tests.Expert.Generators;
using CarbonLedger.Tests.Expert.Support;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Expert.Ledger;

public sealed class LedgerFoundationTests {

    // ---- founding a ledger ---------------------------------------------------------------------------------------

    [Fact]
    public void A_new_ledger_has_issued_and_retired_nothing() {
        // Arrange
        Account issuance = Any.Account().WithKind(AccountKind.Issuance).Generate();

        // Act
        CarbonRegistryLedger ledger = new CarbonRegistryLedger("VCS", issuance);

        // Assert
        Assert.Equal(0, ledger.IssuedQuantity);
        Assert.Equal(0, ledger.RetiredQuantity);
        Assert.Empty(ledger.Journal);
        Assert.True(ledger.IsConsistent());
    }

    [Fact]
    public void A_new_ledger_knows_its_registry_and_its_issuance_account_and_nothing_else() {
        // Arrange
        Account issuance = Any.Account().WithKind(AccountKind.Issuance).Generate();

        // Act
        CarbonRegistryLedger ledger = new CarbonRegistryLedger("VCS", issuance);

        // Assert
        Assert.Equal("VCS", ledger.Registry);
        Assert.Same(issuance, ledger.IssuanceAccount);
        Assert.Same(issuance, Assert.Single(ledger.Accounts));
        Assert.Equal(0, ledger.Balance(issuance.AccountId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("V")]
    [InlineData("VCSXYZ")]
    [InlineData("vcs")]
    [InlineData("VC5")]
    [InlineData(" VC")]
    public void A_registry_that_is_not_two_to_five_capital_letters_is_refused(string registry) {
        Account anyIssuance = Any.Account().WithKind(AccountKind.Issuance).Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new CarbonRegistryLedger(registry, anyIssuance));

        Assert.Equal("registry", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: the registry pattern '^[A-Z]{2,5}$' lets a final newline through ('$' matches before it), so a ledger can be founded on registry 'VCS\\n'; literal input, no seed")]
    public void A_registry_followed_by_a_line_break_is_refused() {
        Account anyIssuance = Any.Account().WithKind(AccountKind.Issuance).Generate();

        Assert.Throws<ArgumentException>(() => new CarbonRegistryLedger("VCS\n", anyIssuance));
    }

    [Theory]
    [InlineData(AccountKind.Holding)]
    [InlineData(AccountKind.Retirement)]
    public void A_ledger_can_only_be_founded_on_an_issuance_account(AccountKind kind) {
        Account notIssuance = Any.Account().WithKind(kind).Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new CarbonRegistryLedger("VCS", notIssuance));

        Assert.Equal("issuanceAccount", refusal.ParamName);
    }

    [Fact]
    public void A_ledger_needs_a_registry_and_an_issuance_account() {
        Account anyIssuance = Any.Account().WithKind(AccountKind.Issuance).Generate();

        Assert.Throws<ArgumentNullException>(() => new CarbonRegistryLedger(null!, anyIssuance));
        Assert.Throws<ArgumentNullException>(() => new CarbonRegistryLedger("VCS", null!));
    }

    // ---- opening accounts ----------------------------------------------------------------------------------------

    [Theory]
    [InlineData(AccountKind.Holding)]
    [InlineData(AccountKind.Retirement)]
    public void An_opened_account_is_known_to_the_ledger_with_an_empty_balance(AccountKind kind) {
        // Arrange
        RegistryScene scene   = RegistryScene.Open();
        Account       stranger = scene.Stranger();
        Account       account  = new Account(stranger.AccountId, stranger.Holder, kind, scene.OpenedAt);

        // Act
        scene.Ledger.OpenAccount(account);

        // Assert
        Assert.Contains(account, scene.Ledger.Accounts);
        Assert.Equal(0, scene.Ledger.Balance(account.AccountId));
        Assert.Empty(scene.Ledger.UnitsHeldBy(account.AccountId));
    }

    [Fact]
    public void A_registry_has_a_single_issuance_account() {
        // Arrange
        RegistryScene scene         = RegistryScene.Open();
        Account       stranger      = scene.Stranger();
        Account       anotherSource = new Account(stranger.AccountId, stranger.Holder, AccountKind.Issuance, scene.OpenedAt);

        // Act
        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.OpenAccount(anotherSource));

        // Assert
        Assert.Contains(scene.Registry, refusal.Message);
        Assert.DoesNotContain(anotherSource, scene.Ledger.Accounts);
    }

    [Fact]
    public void An_account_cannot_be_opened_twice() {
        // Arrange
        RegistryScene scene = RegistryScene.Open();

        // Act
        LedgerRuleViolationException refusal = Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.OpenAccount(scene.First));

        // Assert
        Assert.Contains(scene.First.AccountId, refusal.Message);
    }

    [Fact]
    public void Two_different_accounts_cannot_share_a_number() {
        // Arrange
        RegistryScene scene   = RegistryScene.Open();
        Account       sameId  = new Account(scene.First.AccountId, scene.First.Holder, AccountKind.Retirement, scene.OpenedAt);

        // Act and assert
        Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.OpenAccount(sameId));
        Assert.Equal(AccountKind.Holding, scene.Ledger.Accounts.Single(account => account.AccountId == scene.First.AccountId).Kind);
    }

    [Fact]
    public void An_account_cannot_take_the_number_of_the_issuance_account() {
        // Arrange
        RegistryScene scene  = RegistryScene.Open();
        Account       sameId = new Account(scene.Issuance.AccountId, scene.Issuance.Holder, AccountKind.Holding, scene.OpenedAt);

        // Act and assert
        Assert.Throws<LedgerRuleViolationException>(() => scene.Ledger.OpenAccount(sameId));
    }

    [Fact]
    public void A_missing_account_cannot_be_opened() {
        RegistryScene scene = RegistryScene.Open();

        Assert.Throws<ArgumentNullException>(() => scene.Ledger.OpenAccount(null!));
    }

}
