using JustDummies;

namespace CarbonLedger.Domain;

/// <summary>Recipes shared by several generators and tests: one address per domain rule.</summary>
public static class CarbonRecipes {

    /// <summary>A registry code is 2 to 5 upper-case ASCII letters (the ledger and every serial share the rule).</summary>
    public static AnyString RegistryCode() {
        return Any.String().Alpha().InUpperCase().WithLengthBetween(2, 5);
    }

    /// <summary>Two accounts are the same account when they share an id; <see cref="Account" /> has reference equality.</summary>
    public static IEqualityComparer<Account> SameAccountId { get; } = new AccountIdComparer();

    private sealed class AccountIdComparer : IEqualityComparer<Account> {

        public bool Equals(Account? x, Account? y) {
            return x?.AccountId == y?.AccountId;
        }

        public int GetHashCode(Account obj) {
            return obj.AccountId.GetHashCode(StringComparison.Ordinal);
        }

    }

}

public sealed partial class AnyRetirementClaim {

    /// <summary>
    ///     A claim that can offset the given units: its reporting year is not before the latest vintage retired.
    ///     That rule lives between the claim and the units, so no per-parameter base can carry it.
    /// </summary>
    public AnyRetirementClaim ValidFor(IEnumerable<CreditSerial> serials) {
        int latestVintage = serials.Max(serial => serial.Vintage);

        return WithReportingYear(year => year.GreaterThanOrEqualTo(latestVintage));
    }

}
