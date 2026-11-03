using CarbonLedger.Domain;

using JustDummies;

namespace CarbonLedger.Tests.Regular.Support;

/// <summary>
///     A registry ledger with its issuance account, two holding accounts and a retirement account, all opened at
///     the same arbitrary instant. Everything here is arrangement: the tests state what they care about on top.
/// </summary>
internal sealed class LedgerScenario {

    public const string RegistryCode = "VCS";

    private static readonly DateTimeOffset EarliestOpening = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset LatestOpening   = new DateTimeOffset(2040, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly List<string> _usedNumbers = new List<string>();

    public LedgerScenario() {
        Opened     = Any.DateTimeOffset().Between(EarliestOpening, LatestOpening).Generate();
        Issuance   = OpenAccountOfKind(AccountKind.Issuance);
        Alice      = OpenAccountOfKind(AccountKind.Holding);
        Bob        = OpenAccountOfKind(AccountKind.Holding);
        Retirement = OpenAccountOfKind(AccountKind.Retirement);

        Ledger = new CarbonRegistryLedger(RegistryCode, Issuance);
        Ledger.OpenAccount(Alice);
        Ledger.OpenAccount(Bob);
        Ledger.OpenAccount(Retirement);
    }

    public DateTimeOffset Opened { get; }

    public Account Issuance { get; }

    public Account Alice { get; }

    public Account Bob { get; }

    public Account Retirement { get; }

    public CarbonRegistryLedger Ledger { get; }

    /// <summary>The instant <paramref name="days" /> days after every account was opened.</summary>
    public DateTimeOffset Day(int days) {
        return Opened.AddDays(days);
    }

    /// <summary>A free account of the given kind, opened with the others, not yet registered in the ledger.</summary>
    public Account OpenAccountOfKind(AccountKind kind) {
        Account account = new AnyAccount()
                         .WithKind(kind)
                         .WithOpenedOn(Opened)
                         .WithAccountId(digits => digits.Except(_usedNumbers.ToArray()))
                         .Generate();
        _usedNumbers.Add(account.AccountId.Substring(3));

        return account;
    }

    /// <summary>Distinct serials of the ledger's registry, whatever reporting year they are later retired for.</summary>
    public IReadOnlyCollection<CreditSerial> NewSerials(int count) {
        return Any.ListOf(new AnyCreditSerial().WithRegistry(RegistryCode).WithVintage(v => v.LessThanOrEqualTo(RetirementClaim.MinReportingYear)))
                  .WithCount(count)
                  .Distinct()
                  .Generate();
    }

    public RetirementClaim NewClaim() {
        return new AnyRetirementClaim().Generate();
    }

}
