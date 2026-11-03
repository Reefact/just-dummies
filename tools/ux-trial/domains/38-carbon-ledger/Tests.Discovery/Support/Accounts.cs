using CarbonLedger.Domain;
using CarbonLedger.Tests.Discovery.Generators;

using JustDummies;

namespace CarbonLedger.Tests.Discovery.Support;

internal static class Accounts {

    public static Account Of(AccountKind kind) {
        return Any.Account().WithKind(kind).Generate();
    }

    /// <summary>An account of <paramref name="kind" /> whose id differs from <paramref name="other" />'s.</summary>
    public static Account OtherThan(Account other, AccountKind kind) {
        Account account;
        do {
            account = Of(kind);
        } while (account.AccountId == other.AccountId);

        return account;
    }

}
