using System.Text.RegularExpressions;

namespace PassiveComponents.Domain;

/// <summary>
/// A component-library entry: one manufacturer part number and the specification it guarantees.
/// The nominal value is in SI base units (farads, henries, ohms) and the rating means rated voltage (V)
/// for a capacitor, rated power (W) for a resistor and rated current (A) for an inductor.
/// </summary>
public sealed partial class Part : IEquatable<Part>
{
    public const double MinRatedVolts = 1;
    public const double MaxRatedVolts = 10_000;
    public const double MinRatedWatts = 0.01;
    public const double MinRatedAmperes = 0.001;
    public const double MaxRatedAmperes = 100;
    public const decimal MaxUnitCost = 10_000m;

    public Part(
        string partNumber,
        ComponentKind kind,
        double nominalValue,
        Tolerance tolerance,
        double rating,
        TemperatureCoefficient temperatureCoefficient,
        Package package,
        decimal unitCost)
    {
        ArgumentNullException.ThrowIfNull(partNumber);
        if (!PartNumberFormat().IsMatch(partNumber))
        {
            throw new ArgumentException($"A part number is 3-40 characters of A-Z, 0-9 and - . / + #, starting with a letter or digit; got '{partNumber}'.", nameof(partNumber));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown component kind.");
        }

        (double minValue, double maxValue) = MarkingCode.RangeOf(kind);
        if (!double.IsFinite(nominalValue) || nominalValue < minValue || nominalValue > maxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(nominalValue), nominalValue, $"A {Describe(kind)} value lies in [{minValue:G1}, {maxValue:G1}].");
        }

        ArgumentNullException.ThrowIfNull(tolerance);
        ArgumentNullException.ThrowIfNull(temperatureCoefficient);
        if (!Enum.IsDefined(package))
        {
            throw new ArgumentOutOfRangeException(nameof(package), package, "Unknown package.");
        }

        if ((package == Package.Chip2512 && kind != ComponentKind.Resistor) || (package == Package.Radial && kind == ComponentKind.Resistor))
        {
            throw new ArgumentException($"A {Describe(kind)} does not come in a {package} package.", nameof(package));
        }

        (double minRating, double maxRating) = RatingRange(kind, package);
        if (double.IsNaN(rating) || rating < minRating || rating > maxRating)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), rating, $"A {Describe(kind)} in {package} is rated in [{minRating}, {maxRating}] {UnitOf(kind)}.");
        }

        if (unitCost < 0 || unitCost > MaxUnitCost)
        {
            throw new ArgumentOutOfRangeException(nameof(unitCost), unitCost, $"A unit cost lies in [0, {MaxUnitCost}].");
        }

        if (decimal.Round(unitCost, 4) != unitCost)
        {
            throw new ArgumentException("Distributors price to four decimal places at most.", nameof(unitCost));
        }

        PartNumber = partNumber;
        Kind = kind;
        NominalValue = nominalValue;
        Tolerance = tolerance;
        Rating = rating;
        TemperatureCoefficient = temperatureCoefficient;
        Package = package;
        UnitCost = unitCost;
    }

    public string PartNumber { get; }

    public ComponentKind Kind { get; }

    public double NominalValue { get; }

    public Tolerance Tolerance { get; }

    public double Rating { get; }

    /// <summary>V, W or A, depending on the kind.</summary>
    public string RatingUnit => UnitOf(Kind);

    public TemperatureCoefficient TemperatureCoefficient { get; }

    public Package Package { get; }

    public decimal UnitCost { get; }

    /// <summary>Lowest value the tolerance allows at 25 °C.</summary>
    public double LowerBound => Tolerance.LowerBound(NominalValue);

    /// <summary>Highest value the tolerance allows at 25 °C.</summary>
    public double UpperBound => Tolerance.UpperBound(NominalValue);

    /// <summary>The highest power a resistor in this package may be rated for; refused for <see cref="Package.Radial"/>, which holds no resistor.</summary>
    public static double MaxRatedWatts(Package package) => package switch
    {
        Package.Chip0201 => 0.05,
        Package.Chip0402 => 0.0625,
        Package.Chip0603 => 0.1,
        Package.Chip0805 => 0.125,
        Package.Chip1206 => 0.25,
        Package.Chip2512 => 1,
        Package.Axial => 5,
        _ => throw new ArgumentOutOfRangeException(nameof(package), package, "No resistor comes in this package."),
    };

    /// <summary>Worst-case value range at a temperature in [-55, 155] °C: tolerance plus temperature drift.</summary>
    public (double Minimum, double Maximum) RangeAt(double temperatureCelsius)
    {
        double spread = Tolerance.Fraction + TemperatureCoefficient.DriftAt(temperatureCelsius);
        return (NominalValue * (1 - spread), NominalValue * (1 + spread));
    }

    public Capacitance AsCapacitance() =>
        Kind == ComponentKind.Capacitor ? new Capacitance(NominalValue) : throw new InvalidOperationException($"{PartNumber} is a {Describe(Kind)}, not a capacitor.");

    public Inductance AsInductance() =>
        Kind == ComponentKind.Inductor ? new Inductance(NominalValue) : throw new InvalidOperationException($"{PartNumber} is a {Describe(Kind)}, not an inductor.");

    public Resistance AsResistance() =>
        Kind == ComponentKind.Resistor ? new Resistance(NominalValue) : throw new InvalidOperationException($"{PartNumber} is a {Describe(Kind)}, not a resistor.");

    /// <summary>True when the nominal value is on the series' grid, within a relative 1e-9.</summary>
    public bool HasPreferredValueOf(ESeries series) => PreferredValue.IsPreferred(NominalValue, series);

    /// <summary>
    /// Whether <paramref name="substitute"/> may stand in for this part, checked in order: not the same part number;
    /// same kind; same package (same footprint); its whole tolerance band inside this one's; a rating at least this
    /// one's; a temperature coefficient no worse. The comparisons of values are relative (1e-9), never absolute.
    /// </summary>
    public SubstitutionRefusal CheckSubstitute(Part substitute)
    {
        ArgumentNullException.ThrowIfNull(substitute);
        if (string.Equals(substitute.PartNumber, PartNumber, StringComparison.Ordinal))
        {
            return SubstitutionRefusal.SamePart;
        }

        if (substitute.Kind != Kind)
        {
            return SubstitutionRefusal.DifferentKind;
        }

        if (substitute.Package != Package)
        {
            return SubstitutionRefusal.DifferentPackage;
        }

        if (!Magnitude.IsAtLeast(substitute.LowerBound, LowerBound) || !Magnitude.IsAtMost(substitute.UpperBound, UpperBound))
        {
            return SubstitutionRefusal.ToleranceBandNotContained;
        }

        if (!Magnitude.IsAtLeast(substitute.Rating, Rating))
        {
            return SubstitutionRefusal.RatingTooLow;
        }

        return Magnitude.IsAtMost(substitute.TemperatureCoefficient.PpmPerKelvin, TemperatureCoefficient.PpmPerKelvin)
            ? SubstitutionRefusal.None
            : SubstitutionRefusal.TemperatureCoefficientWorse;
    }

    public bool Equals(Part? other) =>
        other is not null
        && string.Equals(other.PartNumber, PartNumber, StringComparison.Ordinal)
        && other.Kind == Kind
        && other.NominalValue.Equals(NominalValue)
        && other.Tolerance.Equals(Tolerance)
        && other.Rating.Equals(Rating)
        && other.TemperatureCoefficient.Equals(TemperatureCoefficient)
        && other.Package == Package
        && other.UnitCost == UnitCost;

    public override bool Equals(object? obj) => Equals(obj as Part);

    public override int GetHashCode() =>
        HashCode.Combine(StringComparer.Ordinal.GetHashCode(PartNumber), Kind, NominalValue, Tolerance, Rating, TemperatureCoefficient, Package, UnitCost);

    public override string ToString() =>
        $"{PartNumber} ({Magnitude.Format(NominalValue, Kind switch { ComponentKind.Capacitor => "F", ComponentKind.Inductor => "H", _ => "Ω" })} {Tolerance}, {Package})";

    internal static string Describe(ComponentKind kind) => kind.ToString().ToLowerInvariant();

    private static string UnitOf(ComponentKind kind) => kind switch
    {
        ComponentKind.Capacitor => "V",
        ComponentKind.Resistor => "W",
        _ => "A",
    };

    private static (double Min, double Max) RatingRange(ComponentKind kind, Package package) => kind switch
    {
        ComponentKind.Capacitor => (MinRatedVolts, MaxRatedVolts),
        ComponentKind.Inductor => (MinRatedAmperes, MaxRatedAmperes),
        _ => (MinRatedWatts, MaxRatedWatts(package)),
    };

    [GeneratedRegex(@"^[A-Z0-9][A-Z0-9.\-/+#]{2,39}$")]
    private static partial Regex PartNumberFormat();
}
