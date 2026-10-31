using System.Globalization;

namespace BatchGenealogy.Domain;

/// <summary>
/// A non-negative amount of material in a mass or volume unit. Arithmetic and comparison convert
/// through the base unit (gram or millilitre) and refuse to mix mass with volume.
/// </summary>
public sealed class Quantity : IEquatable<Quantity>, IComparable<Quantity>
{
    /// <summary>The largest amount a single record may carry, in any unit.</summary>
    public const decimal MaximumAmount = 1_000_000_000m;

    public Quantity(decimal amount, QuantityUnit unit)
    {
        if (!Enum.IsDefined(unit))
        {
            throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown unit.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(amount, MaximumAmount);
        Amount = amount;
        Unit = unit;
    }

    public decimal Amount { get; }

    public QuantityUnit Unit { get; }

    public bool IsMass => IsMassUnit(Unit);

    public bool IsZero => Amount == 0m;

    /// <summary>The amount in grams for a mass, in millilitres for a volume.</summary>
    public decimal BaseAmount => Amount * Factor(Unit);

    public bool IsCommensurableWith(Quantity other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return IsMass == other.IsMass;
    }

    public Quantity ConvertTo(QuantityUnit unit)
    {
        if (IsMassUnit(unit) != IsMass)
        {
            throw new InvalidOperationException($"Cannot convert {this} to {unit}: mass and volume do not mix.");
        }

        return new Quantity(BaseAmount / Factor(unit), unit);
    }

    public Quantity Add(Quantity other)
    {
        EnsureCommensurable(other);
        return new Quantity((BaseAmount + other.BaseAmount) / Factor(Unit), Unit);
    }

    public Quantity Subtract(Quantity other)
    {
        EnsureCommensurable(other);
        if (other.BaseAmount > BaseAmount)
        {
            throw new InvalidOperationException($"Cannot subtract {other} from {this}: a quantity cannot become negative.");
        }

        return new Quantity((BaseAmount - other.BaseAmount) / Factor(Unit), Unit);
    }

    /// <summary>This quantity scaled by a fraction in [0, 1].</summary>
    public Quantity Scale(decimal fraction)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fraction);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fraction, 1m);
        return new Quantity(Amount * fraction, Unit);
    }

    public int CompareTo(Quantity? other)
    {
        if (other is null)
        {
            return 1;
        }

        EnsureCommensurable(other);
        return BaseAmount.CompareTo(other.BaseAmount);
    }

    public bool Equals(Quantity? other) => other is not null && IsMass == other.IsMass && BaseAmount == other.BaseAmount;

    public override bool Equals(object? obj) => Equals(obj as Quantity);

    public override int GetHashCode() => HashCode.Combine(IsMass, BaseAmount);

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Amount} {Symbol(Unit)}");

    private void EnsureCommensurable(Quantity other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (!IsCommensurableWith(other))
        {
            throw new InvalidOperationException($"Cannot combine {this} with {other}: mass and volume do not mix.");
        }
    }

    private static bool IsMassUnit(QuantityUnit unit) => unit is QuantityUnit.Milligram or QuantityUnit.Gram or QuantityUnit.Kilogram;

    private static decimal Factor(QuantityUnit unit) => unit switch
    {
        QuantityUnit.Milligram => 0.001m,
        QuantityUnit.Gram => 1m,
        QuantityUnit.Kilogram => 1000m,
        QuantityUnit.Millilitre => 1m,
        QuantityUnit.Litre => 1000m,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown unit."),
    };

    private static string Symbol(QuantityUnit unit) => unit switch
    {
        QuantityUnit.Milligram => "mg",
        QuantityUnit.Gram => "g",
        QuantityUnit.Kilogram => "kg",
        QuantityUnit.Millilitre => "mL",
        QuantityUnit.Litre => "L",
        _ => unit.ToString(),
    };
}
