using System.Globalization;
using System.Text.RegularExpressions;

namespace PassiveComponents.Domain;

/// <summary>
/// The value code printed on a part, read for a given kind. Two notations:
/// <list type="bullet">
/// <item>numeric (EIA): significant digits then a multiplier digit — <c>104</c> = 10 × 10⁴; the multiplier
/// digit is 0–7 for 10⁰–10⁷, 8 for 10⁻² and 9 for 10⁻¹. Three digits for every kind, four digits
/// (three significant) for precision resistors only;</item>
/// <item>letter: a letter stands for the decimal point and the unit — <c>4R7</c>, <c>R47</c>, <c>2n2</c>, <c>4K7</c>.</item>
/// </list>
/// Numeric codes count in picofarads for capacitors, microhenries for inductors and ohms for resistors,
/// so <c>104</c> is 100 nF on a capacitor and 100 kΩ on a resistor.
/// </summary>
public sealed partial class MarkingCode : IEquatable<MarkingCode>
{
    public MarkingCode(string code, ComponentKind kind)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown component kind.");
        }

        double value;
        if (NumericNotation().IsMatch(code))
        {
            if (code.Length == 4 && kind != ComponentKind.Resistor)
            {
                throw new FormatException($"Four-digit codes mark precision resistors only, not a {kind.ToString().ToLowerInvariant()}: '{code}'.");
            }

            int multiplier = code[^1] - '0';
            int exponent = multiplier switch
            {
                8 => -2,
                9 => -1,
                _ => multiplier,
            };
            value = Magnitude.Scientific(code[..^1], exponent + BaseExponent(kind));
        }
        else
        {
            Match letter = LetterNotation().Match(code);
            if (!letter.Success)
            {
                throw new FormatException($"A marking is three or four digits, or digits around one unit letter; got '{code}'.");
            }

            string integerPart = letter.Groups["integer"].Value;
            string fractionPart = letter.Groups["fraction"].Value;
            int digitCount = integerPart.Length + fractionPart.Length;
            if (digitCount < 1 || digitCount > 4)
            {
                throw new FormatException($"A letter marking carries one to four digits; got '{code}'.");
            }

            char unit = letter.Groups["unit"].Value[0];
            int? unitExponent = LetterExponent(kind, unit);
            if (unitExponent is null)
            {
                throw new FormatException($"'{unit}' is not a unit letter for a {kind.ToString().ToLowerInvariant()}: '{code}'.");
            }

            string digits = (integerPart.Length == 0 ? "0" : integerPart) + (fractionPart.Length == 0 ? string.Empty : "." + fractionPart);
            value = Magnitude.Scientific(digits, unitExponent.Value);
        }

        if (value == 0)
        {
            throw new ArgumentException($"A marking denotes a positive value; '{code}' reads zero.", nameof(code));
        }

        (double min, double max) = RangeOf(kind);
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(nameof(code), code, $"'{code}' reads {value:G4}, outside the {kind.ToString().ToLowerInvariant()} range [{min:G1}, {max:G1}].");
        }

        Code = code;
        Kind = kind;
        Value = value;
    }

    public string Code { get; }

    public ComponentKind Kind { get; }

    /// <summary>The marked value in SI base units: farads, henries or ohms.</summary>
    public double Value { get; }

    /// <summary>
    /// The usual code for a value: three digits from 10 base units up (100 nF → <c>104</c>), the R notation below
    /// (4.7 pF → <c>4R7</c>, 0.47 Ω → <c>R47</c>); a resistor value needing three significant digits takes the
    /// four-digit or R notation (10.2 kΩ → <c>1022</c>, 49.9 Ω → <c>49R9</c>). Refused when neither notation
    /// represents the value within a relative 1e-9 (4.7 mF is 47 × 10⁸ pF, past the 10⁷ multiplier).
    /// </summary>
    public static MarkingCode For(double value, ComponentKind kind)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown component kind.");
        }

        (double min, double max) = RangeOf(kind);
        if (!double.IsFinite(value) || value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"A {kind.ToString().ToLowerInvariant()} value lies in [{min:G1}, {max:G1}].");
        }

        double inBaseUnits = value / Magnitude.PowerOfTen(BaseExponent(kind));
        string? code = Encode(inBaseUnits, 2, false) ?? (kind == ComponentKind.Resistor ? Encode(inBaseUnits, 3, true) : null);
        return code is null
            ? throw new ArgumentException($"No {kind.ToString().ToLowerInvariant()} marking represents {value:G6}.", nameof(value))
            : new MarkingCode(code, kind);
    }

    /// <summary>True when both codes mark the same value of the same kind (<c>222</c> and <c>2n2</c> on a capacitor).</summary>
    public bool DenotesSameValueAs(MarkingCode other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return other.Kind == Kind && Magnitude.AreClose(other.Value, Value);
    }

    public bool Equals(MarkingCode? other) => other is not null && other.Kind == Kind && string.Equals(other.Code, Code, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as MarkingCode);

    public override int GetHashCode() => HashCode.Combine(Kind, StringComparer.Ordinal.GetHashCode(Code));

    public override string ToString() => Code;

    internal static (double Min, double Max) RangeOf(ComponentKind kind) => kind switch
    {
        ComponentKind.Capacitor => (Capacitance.MinFarads, Capacitance.MaxFarads),
        ComponentKind.Inductor => (Inductance.MinHenries, Inductance.MaxHenries),
        ComponentKind.Resistor => (Resistance.MinOhms, Resistance.MaxOhms),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown component kind."),
    };

    private static int BaseExponent(ComponentKind kind) => kind switch
    {
        ComponentKind.Capacitor => -12,
        ComponentKind.Inductor => -6,
        _ => 0,
    };

    private static int? LetterExponent(ComponentKind kind, char unit) => (kind, unit) switch
    {
        (ComponentKind.Resistor, 'R') => 0,
        (ComponentKind.Resistor, 'K' or 'k') => 3,
        (ComponentKind.Resistor, 'M') => 6,
        (ComponentKind.Resistor, 'G') => 9,
        (ComponentKind.Capacitor, 'R' or 'p') => -12,
        (ComponentKind.Capacitor, 'n') => -9,
        (ComponentKind.Capacitor, 'u' or 'µ') => -6,
        (ComponentKind.Capacitor, 'm') => -3,
        (ComponentKind.Inductor, 'n') => -9,
        (ComponentKind.Inductor, 'R' or 'u' or 'µ') => -6,
        (ComponentKind.Inductor, 'm') => -3,
        _ => null,
    };

    /// <summary>Writes s × 10^p, s with the given number of significant digits; null when it does not fit.</summary>
    private static string? Encode(double inBaseUnits, int significantDigits, bool fourDigitNumeric)
    {
        int exponent = Magnitude.DecadeOf(inBaseUnits) - significantDigits + 1;
        long significand = (long)Math.Round(inBaseUnits / Magnitude.PowerOfTen(exponent));
        if (significand == (long)Math.Pow(10, significantDigits))
        {
            significand /= 10;
            exponent++;
        }

        if (!Magnitude.AreClose(Magnitude.Scientific(significand.ToString(CultureInfo.InvariantCulture), exponent), inBaseUnits))
        {
            return null;
        }

        string s = significand.ToString(CultureInfo.InvariantCulture);
        if (exponent >= 0)
        {
            return exponent <= 7 && (significantDigits == 2 || fourDigitNumeric) ? s + exponent.ToString(CultureInfo.InvariantCulture) : null;
        }

        int point = s.Length + exponent;
        return point > 0
            ? s[..point] + "R" + s[point..]
            : s.Length - point <= 4 ? "R" + new string('0', -point) + s : null;
    }

    [GeneratedRegex(@"^\d{3,4}$")]
    private static partial Regex NumericNotation();

    [GeneratedRegex(@"^(?<integer>\d*)(?<unit>[RKkMGpnuµm])(?<fraction>\d*)$")]
    private static partial Regex LetterNotation();
}
