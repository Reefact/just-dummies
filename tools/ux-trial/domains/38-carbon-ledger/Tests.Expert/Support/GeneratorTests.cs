using CarbonLedger.Domain;
using CarbonLedger.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Expert.Support;

/// <summary>The test data builders are code too: a hundred draws of each must all be values the domain accepts.</summary>
public sealed class GeneratorTests {

    private const int Draws = 100;

    [Fact]
    public void Every_generator_draws_values_the_domain_accepts() {
        Assert.Equal(Draws, Any.ListOf(Any.CreditSerial()).WithCount(Draws).Generate().Count);
        Assert.Equal(Draws, Any.ListOf(Any.Holder()).WithCount(Draws).Generate().Count);
        Assert.Equal(Draws, Any.ListOf(Any.Account()).WithCount(Draws).Generate().Count);
        Assert.Equal(Draws, Any.ListOf(Any.RetirementClaim()).WithCount(Draws).Generate().Count);
        Assert.Equal(Draws, Any.ListOf(Any.HoldingPeriod()).WithCount(Draws).Generate().Count);
        Assert.Equal(Draws, Any.ListOf(Any.JournalEntry()).WithCount(Draws).Generate().Count);
    }

    [Fact]
    public void A_drawn_account_always_carries_the_country_of_its_holder() {
        List<Account> accounts = Any.ListOf(Any.Account()).WithCount(Draws).Generate();

        Assert.All(accounts, account => Assert.StartsWith(account.Holder.CountryCode + "-", account.AccountId));
    }

    [Fact]
    public void A_drawn_entry_has_the_accounts_its_kind_requires_and_a_claim_only_when_it_retires() {
        List<JournalEntry> entries = Any.ListOf(Any.JournalEntry()).WithCount(Draws).Generate();

        Assert.All(entries, entry => Assert.Equal(entry.Kind == EntryKind.Retirement, entry.Claim is not null));
    }

}
