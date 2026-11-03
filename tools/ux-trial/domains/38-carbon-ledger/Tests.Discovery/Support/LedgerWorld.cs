using CarbonLedger.Domain;
using CarbonLedger.Tests.Discovery.Generators;

using JustDummies;

namespace CarbonLedger.Tests.Discovery.Support;

/// <summary>
///     A registry ledger with a drawn registry code and a drawn clock, plus the few verbs the tests keep needing:
///     open an account, mint serials of this registry, issue, and build a claim that covers some serials.
///     Everything it draws is a dummy; a test that cares about an instant or a serial passes its own.
/// </summary>
internal sealed class LedgerWorld {

    private static readonly DateTimeOffset AccountsOpenedAfter  = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AccountsOpenedBefore = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private DateTimeOffset _now;

    public LedgerWorld()
        : this(Any.StringMatching("[A-Z]{2,5}").Generate()) { }

    public LedgerWorld(string registry) {
        Registry = registry;
        _now     = Any.DateTimeOffset()
                      .Between(new DateTimeOffset(2031, 1, 1, 0, 0, 0, TimeSpan.Zero),
                               new DateTimeOffset(2040, 1, 1, 0, 0, 0, TimeSpan.Zero))
                      .Generate();
        IssuanceAccount = DrawAccount(AccountKind.Issuance);
        Ledger          = new CarbonRegistryLedger(Registry, IssuanceAccount);
    }

    public string Registry { get; }

    public Account IssuanceAccount { get; }

    public CarbonRegistryLedger Ledger { get; }

    /// <summary>The last instant handed out by <see cref="NextInstant" />.</summary>
    public DateTimeOffset Now => _now;

    /// <summary>A later instant than any handed out so far.</summary>
    public DateTimeOffset NextInstant() {
        _now += Any.TimeSpan().Between(TimeSpan.FromSeconds(1), TimeSpan.FromDays(30)).Generate();

        return _now;
    }

    public Account OpenAccount(AccountKind kind) {
        Account account = UnopenedAccount(kind);

        Ledger.OpenAccount(account);

        return account;
    }

    /// <summary>A valid account whose id this ledger does not know yet.</summary>
    public Account UnopenedAccount(AccountKind kind = AccountKind.Holding) {
        Account account;
        do {
            account = DrawAccount(kind);
        } while (Ledger.Accounts.Any(known => known.AccountId == account.AccountId));

        return account;
    }

    public Account OpenHolding()    => OpenAccount(AccountKind.Holding);
    public Account OpenRetirement() => OpenAccount(AccountKind.Retirement);

    /// <summary>Distinct serials of this ledger's registry, none of them issued yet.</summary>
    public List<CreditSerial> NewSerials(int count) {
        return Any.ListOf(Any.CreditSerial().WithRegistry(Registry)).WithCount(count).Distinct().Generate();
    }

    public List<CreditSerial> Issue(Account to, int count) {
        List<CreditSerial> serials = NewSerials(count);

        Ledger.Issue(serials, to.AccountId, NextInstant());

        return serials;
    }

    public JournalEntry Transfer(IReadOnlyCollection<CreditSerial> serials, Account from, Account to) {
        return Ledger.Transfer(serials, from.AccountId, to.AccountId, NextInstant());
    }

    public JournalEntry Retire(IReadOnlyCollection<CreditSerial> serials, Account from, Account to) {
        return Ledger.Retire(serials, from.AccountId, to.AccountId, ClaimCovering(serials), NextInstant());
    }

    /// <summary>
    ///     A claim a retirement of <paramref name="serials" /> can carry: its reporting year is not before their latest
    ///     vintage, which is a rule of the journal and not of the test.
    /// </summary>
    public static RetirementClaim ClaimCovering(IEnumerable<CreditSerial> serials) {
        int latestVintage = serials.Max(serial => serial.Vintage);

        return Any.RetirementClaim().WithReportingYear(year => year.GreaterThanOrEqualTo(latestVintage)).Generate();
    }

    private static Account DrawAccount(AccountKind kind) {
        return Any.Account()
                  .WithKind(kind)
                  .WithOpenedOn(openedOn => openedOn.Between(AccountsOpenedAfter, AccountsOpenedBefore))
                  .Generate();
    }

}
