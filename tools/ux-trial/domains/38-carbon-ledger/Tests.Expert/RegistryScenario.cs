using CarbonLedger.Domain;

using JustDummies;

namespace CarbonLedger.Tests.Expert;

/// <summary>
///     A ledger in a valid, arbitrary state, with the bookkeeping a test does not care about done for it:
///     distinct account ids, accounts opened before anything is posted to them, and a clock that only moves forward.
///     Everything it draws is a dummy; what a test is about, it states itself.
/// </summary>
internal sealed class RegistryScenario {

    private readonly List<string> _usedAccountNumbers = [];
    private          DateTimeOffset _now;

    public RegistryScenario()
        : this(Any.DateTimeOffset()
                  .Between(new DateTimeOffset(2005, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2090, 1, 1, 0, 0, 0, TimeSpan.Zero))
                  .WithGranularity(TimeSpan.FromSeconds(1))
                  .Generate()) { }

    /// <summary>A registry whose accounts are all opened on or before <paramref name="startsAt" />.</summary>
    public RegistryScenario(DateTimeOffset startsAt) {
        _now     = startsAt;
        Registry = CarbonRecipes.RegistryCode().Generate();
        Ledger   = new CarbonRegistryLedger(Registry, DrawAccount(AccountKind.Issuance));
    }

    public string Registry { get; }

    public CarbonRegistryLedger Ledger { get; }

    public Account Issuance => Ledger.IssuanceAccount;

    /// <summary>A serial of this registry, not issued yet.</summary>
    public AnyCreditSerial Serial => Any.CreditSerial().WithRegistry(Registry);

    /// <summary>An instant after every entry posted so far and after every account opened.</summary>
    public DateTimeOffset Later() {
        _now += Any.TimeSpan().Between(TimeSpan.FromMinutes(1), TimeSpan.FromDays(30)).WithGranularity(TimeSpan.FromSeconds(1)).Generate();

        return _now;
    }

    public Account OpenHolding() {
        return Open(AccountKind.Holding);
    }

    public Account OpenRetirement() {
        return Open(AccountKind.Retirement);
    }

    /// <summary>Draws <paramref name="count" /> distinct new units of this registry.</summary>
    public HashSet<CreditSerial> NewUnits(int count) {
        return Any.SetOf(Serial).WithCount(count).Generate();
    }

    /// <summary>Issues <paramref name="count" /> new units into <paramref name="account" /> and returns them.</summary>
    public HashSet<CreditSerial> IssueTo(Account account, int count) {
        HashSet<CreditSerial> units = NewUnits(count);
        Ledger.Issue(units, account.AccountId, Later());

        return units;
    }

    /// <summary>A claim that can offset <paramref name="units" />.</summary>
    public RetirementClaim ClaimFor(IEnumerable<CreditSerial> units) {
        return Any.RetirementClaim().ValidFor(units).Generate();
    }

    private Account Open(AccountKind kind) {
        Account account = DrawAccount(kind);
        Ledger.OpenAccount(account);

        return account;
    }

    // AnyString.Except refuses an empty list ("At least one value is required"), and the first account of a
    // registry legitimately has nothing to exclude, so the exclusion is applied only once there is something in it.
    private AnyString ExcludingNumbersAlreadyUsed(AnyString number) {
        return _usedAccountNumbers.Count == 0 ? number : number.Except(_usedAccountNumbers.ToArray());
    }

    private Account DrawAccount(AccountKind kind) {
        Account account = Any.Account()
                             .WithKind(kind)
                             .WithOpenedOn(openedOn => openedOn.BeforeOrEqualTo(_now).After(_now.AddYears(-30)))
                             .WithAccountNumber(ExcludingNumbersAlreadyUsed)
                             .Generate();
        _usedAccountNumbers.Add(account.AccountId[3..]);

        return account;
    }

}
