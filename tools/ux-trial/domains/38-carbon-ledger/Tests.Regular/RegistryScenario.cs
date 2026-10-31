using System;
using System.Collections.Generic;
using System.Linq;

using CarbonLedger.Domain;

using JustDummies;

namespace CarbonLedger.Tests.Regular;

/// <summary>
///     An open registry with its issuance account, plus the helpers the ledger tests share: accounts opened
///     before the first posting, units of this registry, and posting moments that move forward.
/// </summary>
internal sealed class RegistryScenario {

    /// <summary>Every account the scenario opens is opened before this instant; every entry is recorded after it.</summary>
    public static readonly DateTimeOffset FirstPosting = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private RegistryScenario(string registry, Account issuanceAccount) {
        Registry        = registry;
        IssuanceAccount = issuanceAccount;
        Ledger          = new CarbonRegistryLedger(registry, issuanceAccount);
    }

    public string Registry { get; }

    public Account IssuanceAccount { get; }

    public CarbonRegistryLedger Ledger { get; }

    public static RegistryScenario Open() {
        string  anyRegistry        = Any.StringMatching("[A-Z]{2,5}").Generate();
        Account anyIssuanceAccount = AnAccountOpenedBeforeFirstPosting().WithKind(AccountKind.Issuance).Generate();

        return new RegistryScenario(anyRegistry, anyIssuanceAccount);
    }

    public static AnyAccount AnAccountOpenedBeforeFirstPosting() {
        return Arbitrary.Account().WithOpenedOn(o => o.Before(FirstPosting));
    }

    public Account OpenAccount(AccountKind kind) {
        Account anyAccount = AnAccountOpenedBeforeFirstPosting().WithKind(kind).Generate();
        Ledger.OpenAccount(anyAccount);

        return anyAccount;
    }

    /// <summary>A unit of this registry.</summary>
    public AnyCreditSerial Unit() {
        return Arbitrary.CreditSerial().WithRegistry(Registry);
    }

    public List<CreditSerial> Units(int count) {
        return Any.ListOf(Unit()).WithCount(count).Distinct().Generate();
    }

    public List<CreditSerial> SomeUnits() {
        return Any.ListOf(Unit()).WithCountBetween(1, 5).Distinct().Generate();
    }

    /// <summary>A moment no earlier than the last entry (or the first posting), so the journal stays append-only.</summary>
    public DateTimeOffset NextMoment() {
        DateTimeOffset last = Ledger.Journal.Count == 0 ? FirstPosting : Ledger.Journal[^1].RecordedAt;

        return Any.DateTimeOffset().AfterOrEqualTo(last).Before(last.AddDays(30)).Generate();
    }

    /// <summary>A claim whose reporting year is not before the latest vintage among <paramref name="units" />.</summary>
    public static RetirementClaim ClaimFor(IEnumerable<CreditSerial> units) {
        int latestVintage = units.Max(u => u.Vintage);

        return Arbitrary.RetirementClaim().WithReportingYear(y => y.GreaterThanOrEqualTo(latestVintage)).Generate();
    }

    /// <summary>Issues <paramref name="units" /> into a fresh holding account, and returns that account.</summary>
    public Account HoldingAccountWith(IReadOnlyCollection<CreditSerial> units) {
        Account holding = OpenAccount(AccountKind.Holding);
        Ledger.Issue(units, holding.AccountId, NextMoment());

        return holding;
    }

}
