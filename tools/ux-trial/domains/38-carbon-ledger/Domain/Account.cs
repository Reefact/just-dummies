using System.Text.RegularExpressions;

namespace CarbonLedger.Domain;

/// <summary>A registry account, identified by a number such as <c>FR-000123</c>.</summary>
public sealed partial class Account
{
    public Account(string accountId, Holder holder, AccountKind kind, DateTimeOffset openedOn)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(holder);
        if (!AccountIdPattern().IsMatch(accountId))
        {
            throw new ArgumentException("An account id is two upper-case letters, a dash and six digits.", nameof(accountId));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown account kind.");
        }

        if (!accountId.StartsWith(holder.CountryCode, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Account '{accountId}' must be prefixed with its holder's country code '{holder.CountryCode}'.",
                nameof(accountId));
        }

        AccountId = accountId;
        Holder = holder;
        Kind = kind;
        OpenedOn = openedOn;
    }

    public string AccountId { get; }

    public Holder Holder { get; }

    public AccountKind Kind { get; }

    public DateTimeOffset OpenedOn { get; }

    public override string ToString() => $"{AccountId} [{Kind}]";

    [GeneratedRegex(@"^[A-Z]{2}-\d{6}$")]
    private static partial Regex AccountIdPattern();
}
