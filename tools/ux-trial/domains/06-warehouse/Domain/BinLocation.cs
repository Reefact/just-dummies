using System.Globalization;
using System.Text.RegularExpressions;

namespace Warehouse.Domain;

/// <summary>A storage bin, written aisle-rack-level: A-12-03.</summary>
public sealed class BinLocation {

    private static readonly Regex Format = new(@"^([A-Z])-(\d{2})-(\d{2})$");

    public BinLocation(char aisle, int rack, int level) {
        if (aisle < 'A' || aisle > 'Z') {
            throw new ArgumentOutOfRangeException(nameof(aisle), aisle, "An aisle is an upper-case letter.");
        }
        ArgumentOutOfRangeException.ThrowIfLessThan(rack, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(rack, 99);

        Aisle = aisle;
        Rack  = rack;
        Level = level switch {
            >= 1 and <= 20 => level,
            _              => throw new ArgumentOutOfRangeException(nameof(level), level, "A rack has levels 1 to 20.")
        };
    }

    public char Aisle { get; }
    public int  Rack  { get; }
    public int  Level { get; }

    /// <summary>Levels above 4 need a reach truck rather than a pallet jack.</summary>
    public bool NeedsReachTruck => Level > 4;

    public static BinLocation Parse(string text) {
        Match match = Format.Match(text);
        if (!match.Success) {
            throw new FormatException($"'{text}' is not a bin location like A-12-03.");
        }

        return new BinLocation(match.Groups[1].Value[0],
                               int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                               int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture));
    }

    public override bool Equals(object? obj) => obj is BinLocation other && other.ToString() == ToString();

    public override int GetHashCode() => ToString().GetHashCode();

    public override string ToString() => $"{Aisle}-{Rack:00}-{Level:00}";

}
