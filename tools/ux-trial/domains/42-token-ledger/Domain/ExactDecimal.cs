using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace TokenLedger.Domain;

/// <summary>
/// An exact decimal number of arbitrary precision: <c>Mantissa × 10^-Scale</c>, with a
/// non-negative scale of at most <see cref="MaxScale"/>. Always normalised: no trailing zero
/// in the mantissa while the scale is positive, and zero is <c>0</c> at scale 0 — so two equal
/// numbers have the same mantissa and scale.
/// </summary>
public sealed partial class ExactDecimal : IEquatable<ExactDecimal>, IComparable<ExactDecimal>
{
    public const int MaxScale = 80;

    public ExactDecimal(BigInteger mantissa, int scale)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(scale);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(scale, MaxScale);

        while (scale > 0 && (mantissa % 10).IsZero)
        {
            mantissa /= 10;
            scale--;
        }

        Mantissa = mantissa;
        Scale = mantissa.IsZero ? 0 : scale;
    }

    public static ExactDecimal Zero { get; } = new(BigInteger.Zero, 0);

    public static ExactDecimal One { get; } = new(BigInteger.One, 0);

    public BigInteger Mantissa { get; }

    /// <summary>Number of digits after the decimal point, once normalised.</summary>
    public int Scale { get; }

    public int Sign => Mantissa.Sign;

    public bool IsZero => Mantissa.IsZero;

    public bool IsInteger => Scale == 0;

    /// <summary>Reads <c>-?digits(.digits)?</c>, invariant culture, no exponent, no grouping.</summary>
    public static ExactDecimal Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Match match = Syntax().Match(text);
        if (!match.Success)
        {
            throw new FormatException($"'{text}' is not a plain decimal number such as -1234.5678.");
        }

        string fraction = match.Groups["fraction"].Value.TrimEnd('0');
        if (fraction.Length > MaxScale)
        {
            throw new FormatException($"'{text}' has more than {MaxScale} significant fractional digits.");
        }

        BigInteger mantissa = BigInteger.Parse(match.Groups["integer"].Value + fraction, NumberStyles.None, CultureInfo.InvariantCulture);
        if (match.Groups["sign"].Success)
        {
            mantissa = -mantissa;
        }

        return new ExactDecimal(mantissa, fraction.Length);
    }

    public static ExactDecimal FromInteger(BigInteger value) => new(value, 0);

    public ExactDecimal Negate() => new(-Mantissa, Scale);

    public ExactDecimal Add(ExactDecimal other)
    {
        ArgumentNullException.ThrowIfNull(other);
        int scale = Math.Max(Scale, other.Scale);
        return new ExactDecimal(Align(scale) + other.Align(scale), scale);
    }

    public ExactDecimal Subtract(ExactDecimal other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Add(other.Negate());
    }

    /// <summary>The exact product; refused when even its normalised form needs more than <see cref="MaxScale"/> digits.</summary>
    public ExactDecimal Multiply(ExactDecimal other)
    {
        ArgumentNullException.ThrowIfNull(other);
        BigInteger mantissa = Mantissa * other.Mantissa;
        int scale = Scale + other.Scale;
        while (scale > MaxScale && (mantissa % 10).IsZero)
        {
            mantissa /= 10;
            scale--;
        }

        if (scale > MaxScale && !mantissa.IsZero)
        {
            throw new OverflowException($"{this} x {other} needs more than {MaxScale} fractional digits.");
        }

        return new ExactDecimal(mantissa, mantissa.IsZero ? 0 : scale);
    }

    /// <summary>The quotient truncated toward zero at <paramref name="scale"/> fractional digits.</summary>
    public ExactDecimal Divide(ExactDecimal divisor, int scale)
    {
        ArgumentNullException.ThrowIfNull(divisor);
        if (scale < 0 || scale > MaxScale)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), scale, $"A quotient keeps 0 to {MaxScale} fractional digits.");
        }

        if (divisor.IsZero)
        {
            throw new DivideByZeroException("Division of an exact decimal by zero.");
        }

        BigInteger numerator = Mantissa * BigInteger.Pow(10, divisor.Scale + scale);
        BigInteger denominator = divisor.Mantissa * BigInteger.Pow(10, Scale);
        return new ExactDecimal(BigInteger.Divide(numerator, denominator), scale);
    }

    /// <summary>Drops the digits beyond <paramref name="scale"/>, toward zero.</summary>
    public ExactDecimal Truncate(int scale)
    {
        if (scale < 0 || scale > MaxScale)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), scale, $"A scale lies in [0, {MaxScale}].");
        }

        if (scale >= Scale)
        {
            return this;
        }

        return new ExactDecimal(BigInteger.Divide(Mantissa, BigInteger.Pow(10, Scale - scale)), scale);
    }

    /// <summary>The value multiplied by 10^scale, when that is an integer.</summary>
    public BigInteger ToScaledInteger(int scale)
    {
        if (scale < Scale)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), scale, $"{this} has {Scale} fractional digits; it is not a whole number at scale {scale}.");
        }

        return Align(scale);
    }

    public int CompareTo(ExactDecimal? other)
    {
        if (other is null)
        {
            return 1;
        }

        int scale = Math.Max(Scale, other.Scale);
        return Align(scale).CompareTo(other.Align(scale));
    }

    public bool Equals(ExactDecimal? other) => other is not null && Mantissa == other.Mantissa && Scale == other.Scale;

    public override bool Equals(object? obj) => Equals(obj as ExactDecimal);

    public override int GetHashCode() => HashCode.Combine(Mantissa, Scale);

    public override string ToString()
    {
        string digits = BigInteger.Abs(Mantissa).ToString(CultureInfo.InvariantCulture);
        string sign = Mantissa.Sign < 0 ? "-" : string.Empty;
        if (Scale == 0)
        {
            return sign + digits;
        }

        digits = digits.PadLeft(Scale + 1, '0');
        return sign + digits[..^Scale] + "." + digits[^Scale..];
    }

    private BigInteger Align(int scale) => Mantissa * BigInteger.Pow(10, scale - Scale);

    [GeneratedRegex(@"^(?<sign>-)?(?<integer>[0-9]+)(\.(?<fraction>[0-9]+))?$", RegexOptions.CultureInvariant)]
    private static partial Regex Syntax();
}
