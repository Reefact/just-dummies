using System.Globalization;
using System.Text.RegularExpressions;

namespace CarbonLedger.Domain;

/// <summary>
/// The identity of one carbon credit (one tonne of CO2e), e.g. <c>VCS-1234-2019-000000000042</c>.
/// </summary>
public sealed partial class CreditSerial : IEquatable<CreditSerial>
{
    public const int MinVintage = 1990;
    public const int MaxVintage = 2100;
    public const int MaxProjectId = 999_999;
    public const long MaxUnitNumber = 999_999_999_999;

    public CreditSerial(string registry, int projectId, int vintage, long unitNumber)
    {
        ArgumentNullException.ThrowIfNull(registry);
        if (!RegistryPattern().IsMatch(registry))
        {
            throw new ArgumentException("A registry code is 2 to 5 upper-case ASCII letters.", nameof(registry));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(projectId, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(projectId, MaxProjectId);
        ArgumentOutOfRangeException.ThrowIfLessThan(vintage, MinVintage);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(vintage, MaxVintage);
        ArgumentOutOfRangeException.ThrowIfLessThan(unitNumber, 1L);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(unitNumber, MaxUnitNumber);

        Registry = registry;
        ProjectId = projectId;
        Vintage = vintage;
        UnitNumber = unitNumber;
    }

    public string Registry { get; }

    public int ProjectId { get; }

    public int Vintage { get; }

    public long UnitNumber { get; }

    /// <summary>Parses the canonical form produced by <see cref="ToString"/>.</summary>
    public static CreditSerial Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Match match = SerialPattern().Match(text);
        if (!match.Success)
        {
            throw new FormatException($"'{text}' is not a credit serial of the form REG-PPPP-YYYY-NNNNNNNNNNNN.");
        }

        return new CreditSerial(
            match.Groups["reg"].Value,
            int.Parse(match.Groups["project"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["vintage"].Value, CultureInfo.InvariantCulture),
            long.Parse(match.Groups["unit"].Value, CultureInfo.InvariantCulture));
    }

    public bool Equals(CreditSerial? other) =>
        other is not null
        && Registry == other.Registry
        && ProjectId == other.ProjectId
        && Vintage == other.Vintage
        && UnitNumber == other.UnitNumber;

    public override bool Equals(object? obj) => Equals(obj as CreditSerial);

    public override int GetHashCode() => HashCode.Combine(Registry, ProjectId, Vintage, UnitNumber);

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Registry}-{ProjectId:D4}-{Vintage:D4}-{UnitNumber:D12}");

    [GeneratedRegex("^[A-Z]{2,5}$")]
    private static partial Regex RegistryPattern();

    [GeneratedRegex(@"^(?<reg>[A-Z]{2,5})-(?<project>\d{4,6})-(?<vintage>\d{4})-(?<unit>\d{12})$")]
    private static partial Regex SerialPattern();
}
