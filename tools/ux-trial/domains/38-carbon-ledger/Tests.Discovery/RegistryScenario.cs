using CarbonLedger.Domain;

using JustDummies;

namespace CarbonLedger.Tests.Discovery;

/// <summary>
///     A ledger with its issuance account, two holding accounts and a retirement account, all opened at the
///     same arbitrary instant. Built inside each test (never in a constructor) so [Reproducible] pins its draws.
/// </summary>
internal sealed class RegistryScenario {

    private RegistryScenario(string registry, DateTimeOffset openedAt, Account issuance, Account seller, Account buyer, Account retirement) {
        Registry   = registry;
        OpenedAt   = openedAt;
        Issuance   = issuance;
        Seller     = seller;
        Buyer      = buyer;
        Retirement = retirement;
        Ledger     = new CarbonRegistryLedger(registry, issuance);
        Ledger.OpenAccount(seller);
        Ledger.OpenAccount(buyer);
        Ledger.OpenAccount(retirement);
    }

    public string Registry { get; }

    public DateTimeOffset OpenedAt { get; }

    public Account Issuance { get; }

    public Account Seller { get; }

    public Account Buyer { get; }

    public Account Retirement { get; }

    public CarbonRegistryLedger Ledger { get; }

    public static RegistryScenario Open() {
        string         anyRegistry = Any.String().Alpha().InUpperCase().WithLengthBetween(2, 5).Generate();
        DateTimeOffset anyOpenedAt = Any.DateTimeOffset()
                                        .Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                                 new DateTimeOffset(2090, 1, 1, 0, 0, 0, TimeSpan.Zero))
                                        .Generate();

        // Account ids must differ inside one registry; the issuance account holds no units.
        List<Account> anyAccounts = Any.ListOf(Any.Account().WithKind(AccountKind.Holding).WithOpenedOn(anyOpenedAt))
                                       .WithCount(2)
                                       .Generate();
        Account anyIssuance   = Any.Account().WithKind(AccountKind.Issuance).WithOpenedOn(anyOpenedAt).Generate();
        Account anyRetirement = Any.Account().WithKind(AccountKind.Retirement).WithOpenedOn(anyOpenedAt).Generate();

        return new RegistryScenario(anyRegistry, anyOpenedAt, anyIssuance, anyAccounts[0], anyAccounts[1], anyRetirement);
    }

    /// <summary>An instant after every account was opened, <paramref name="hours" /> hours in.</summary>
    public DateTimeOffset At(int hours) => OpenedAt.AddHours(hours);

    /// <summary>Serials of this registry, all different.</summary>
    public AnyCreditSerial AnySerial() => Any.CreditSerial().WithRegistry(Registry);

    public List<CreditSerial> AnyDistinctSerials(int count) =>
        Any.ListOf(AnySerial()).WithCount(count).Distinct().Generate();

    /// <summary>A claim the given units may honestly offset: its reporting year is not before their latest vintage.</summary>
    public RetirementClaim AnyClaimFor(IEnumerable<CreditSerial> serials) {
        int latestVintage = serials.Max(s => s.Vintage);

        return Any.RetirementClaim()
                  .WithReportingYear(y => y.GreaterThanOrEqualTo(latestVintage))
                  .Generate();
    }

}
