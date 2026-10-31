using System.Text.RegularExpressions;

namespace CarbonLedger.Domain;

/// <summary>A legal person registered with the registry, who may own accounts.</summary>
public sealed partial class Holder
{
    public const int MaxLegalNameLength = 200;

    public Holder(string holderId, string legalName, string countryCode)
    {
        ArgumentNullException.ThrowIfNull(holderId);
        ArgumentNullException.ThrowIfNull(legalName);
        ArgumentNullException.ThrowIfNull(countryCode);
        if (!HolderIdPattern().IsMatch(holderId))
        {
            throw new ArgumentException("A holder id is 4 to 12 upper-case letters or digits.", nameof(holderId));
        }

        if (string.IsNullOrWhiteSpace(legalName))
        {
            throw new ArgumentException("A holder needs a legal name.", nameof(legalName));
        }

        if (legalName.Length > MaxLegalNameLength)
        {
            throw new ArgumentException($"A legal name is at most {MaxLegalNameLength} characters.", nameof(legalName));
        }

        if (!CountryPattern().IsMatch(countryCode))
        {
            throw new ArgumentException("A country code is an ISO 3166-1 alpha-2 code (two upper-case letters).", nameof(countryCode));
        }

        HolderId = holderId;
        LegalName = legalName.Trim();
        CountryCode = countryCode;
    }

    public string HolderId { get; }

    public string LegalName { get; }

    public string CountryCode { get; }

    public override string ToString() => $"{LegalName} ({HolderId}, {CountryCode})";

    [GeneratedRegex("^[A-Z0-9]{4,12}$")]
    private static partial Regex HolderIdPattern();

    [GeneratedRegex("^[A-Z]{2}$")]
    private static partial Regex CountryPattern();
}
