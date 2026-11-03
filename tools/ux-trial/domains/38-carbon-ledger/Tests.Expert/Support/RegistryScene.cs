using CarbonLedger.Domain;
using CarbonLedger.Tests.Expert.Generators;

using JustDummies;

namespace CarbonLedger.Tests.Expert.Support;

/// <summary>
///     A registry open for business: a ledger, its issuance account, three holding accounts and a retirement
///     account, all opened at the same arbitrary instant. Nothing in it is meant to matter to a test: a test
///     states only what it needs on top (how many units, who holds them, when).
/// </summary>
internal sealed class RegistryScene {

    private static readonly DateTimeOffset EarliestOpening = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset LatestOpening   = new DateTimeOffset(2050, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private RegistryScene(CarbonRegistryLedger ledger, DateTimeOffset openedAt, Account first, Account second, Account third, Account retirement) {
        Ledger     = ledger;
        OpenedAt   = openedAt;
        First      = first;
        Second     = second;
        Third      = third;
        Retirement = retirement;
    }

    public CarbonRegistryLedger Ledger { get; }

    public DateTimeOffset OpenedAt { get; }

    public string Registry => Ledger.Registry;

    public Account Issuance => Ledger.IssuanceAccount;

    public Account First { get; }

    public Account Second { get; }

    public Account Third { get; }

    public Account Retirement { get; }

    public static RegistryScene Open() {
        string         registry = AnyCreditSerial.ValidRegistry().Generate();
        DateTimeOffset openedAt = Any.DateTimeOffset().Between(EarliestOpening, LatestOpening).Generate();

        // Five accounts with five different numbers, whatever their holders: the ledger refuses two with the same one.
        List<Account> drawn = Any.ListOf(Any.Account()).WithCount(5).DistinctBy(account => account.AccountId).Generate();

        Account issuance   = Reopen(drawn[0], AccountKind.Issuance, openedAt);
        Account first      = Reopen(drawn[1], AccountKind.Holding, openedAt);
        Account second     = Reopen(drawn[2], AccountKind.Holding, openedAt);
        Account third      = Reopen(drawn[3], AccountKind.Holding, openedAt);
        Account retirement = Reopen(drawn[4], AccountKind.Retirement, openedAt);

        CarbonRegistryLedger ledger = new CarbonRegistryLedger(registry, issuance);
        ledger.OpenAccount(first);
        ledger.OpenAccount(second);
        ledger.OpenAccount(third);
        ledger.OpenAccount(retirement);

        return new RegistryScene(ledger, openedAt, first, second, third, retirement);
    }

    /// <summary>The scene's opening instant, <paramref name="days" /> days later (or earlier, when negative).</summary>
    public DateTimeOffset At(int days) {
        return OpenedAt.AddDays(days);
    }

    /// <summary>Units of this registry that nobody has issued yet.</summary>
    public IReadOnlyList<CreditSerial> NewSerials(int count) {
        return Any.ListOf(Any.CreditSerial().WithRegistry(Registry)).WithCount(count).Distinct().Generate();
    }

    /// <summary>A claim that is valid for retiring <paramref name="serials" />: its reporting year is no earlier than their latest vintage.</summary>
    public RetirementClaim ClaimFor(IEnumerable<CreditSerial> serials) {
        int latestVintage = serials.Max(serial => serial.Vintage);

        return Any.RetirementClaim().WithReportingYear(year => year.GreaterThanOrEqualTo(latestVintage)).Generate();
    }

    public IReadOnlyList<CreditSerial> IssueTo(Account holder, int count, int day = 0) {
        IReadOnlyList<CreditSerial> serials = NewSerials(count);

        Ledger.Issue(serials, holder.AccountId, At(day));

        return serials;
    }

    public JournalEntry RetireFrom(Account holder, IReadOnlyCollection<CreditSerial> serials, int day = 0) {
        return Ledger.Retire(serials, holder.AccountId, Retirement.AccountId, ClaimFor(serials), At(day));
    }

    /// <summary>An account that is not open in this ledger: its holder's country is none of the open accounts' countries.</summary>
    public Account Stranger() {
        string[] openCountries = new[] { Issuance, First, Second, Third, Retirement }.Select(account => account.Holder.CountryCode).ToArray();

        return Any.Account().WithKind(AccountKind.Holding).WithHolder(holder => holder.WithCountryCode(country => country.Except(openCountries))).Generate();
    }

    /// <summary>Opens one more holding account, which comes into existence <paramref name="days" /> days after the scene's opening instant.</summary>
    public Account OpenHoldingAccountOn(int days) {
        Account drawn = Any.Account().WithKind(AccountKind.Holding).WithOpenedOn(At(days)).WithHolder(holder => holder.WithCountryCode(country => country.Except(OpenCountries()))).Generate();

        Ledger.OpenAccount(drawn);

        return drawn;
    }

    private string[] OpenCountries() {
        return Ledger.Accounts.Select(account => account.Holder.CountryCode).ToArray();
    }

    private static Account Reopen(Account drawn, AccountKind kind, DateTimeOffset openedOn) {
        return new Account(drawn.AccountId, drawn.Holder, kind, openedOn);
    }

}
