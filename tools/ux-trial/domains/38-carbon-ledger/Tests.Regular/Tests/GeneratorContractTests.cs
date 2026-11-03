using CarbonLedger.Domain;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Regular.Tests;

/// <summary>The scaffolded and hand-completed generators draw only what the domain constructors accept.</summary>
public sealed class GeneratorContractTests {

    private const int Draws = 300;

    [Fact]
    public void Generated_serials_holders_accounts_and_claims_are_always_accepted() {
        for (int i = 0; i < Draws; i++) {
            Assert.NotNull(new AnyCreditSerial().Generate());
            Assert.NotNull(new AnyHolder().Generate());
            Assert.NotNull(new AnyAccount().Generate());
            Assert.NotNull(new AnyRetirementClaim().Generate());
            Assert.NotNull(new AnyHoldingPeriod().Generate());
        }
    }

    [Fact]
    public void Generated_journal_entries_are_always_accepted() {
        for (int i = 0; i < Draws; i++) {
            Assert.NotNull(new AnyJournalEntry().Generate());
        }
    }

    [Fact]
    public void Generated_ledgers_are_founded_on_their_issuance_account() {
        for (int i = 0; i < Draws; i++) {
            CarbonRegistryLedger ledger = new AnyCarbonRegistryLedger().Generate();

            Assert.Equal(AccountKind.Issuance, ledger.IssuanceAccount.Kind);
        }
    }

}
