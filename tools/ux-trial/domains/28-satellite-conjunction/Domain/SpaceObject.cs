namespace SatelliteConjunction.Domain;

public enum ObjectKind
{
    Payload,
    RocketBody,
    Debris,
}

/// <summary>A catalogued object in orbit. Only an active payload can manoeuvre.</summary>
public sealed class SpaceObject
{
    public const int MaximumCatalogNumber = 999_999_999;
    public const int MaximumNameLength = 24;
    public const double MaximumHardBodyRadiusMeters = 100;

    public SpaceObject(int catalogNumber, string name, ObjectKind kind, double hardBodyRadiusMeters, bool isActive)
    {
        if (catalogNumber < 1 || catalogNumber > MaximumCatalogNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(catalogNumber), catalogNumber, $"A catalogue number (NORAD id) lies between 1 and {MaximumCatalogNumber}.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > MaximumNameLength)
        {
            throw new ArgumentException($"An object name holds at most {MaximumNameLength} characters.", nameof(name));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown object kind.");
        }

        if (!double.IsFinite(hardBodyRadiusMeters) || hardBodyRadiusMeters <= 0 || hardBodyRadiusMeters > MaximumHardBodyRadiusMeters)
        {
            throw new ArgumentOutOfRangeException(nameof(hardBodyRadiusMeters), hardBodyRadiusMeters, $"A hard-body radius is positive and at most {MaximumHardBodyRadiusMeters} m.");
        }

        if (isActive && kind != ObjectKind.Payload)
        {
            throw new ArgumentException("Only a payload can be active; rocket bodies and debris are inert.", nameof(isActive));
        }

        CatalogNumber = catalogNumber;
        Name = name;
        Kind = kind;
        HardBodyRadiusMeters = hardBodyRadiusMeters;
        IsActive = isActive;
    }

    public int CatalogNumber { get; }

    public string Name { get; }

    public ObjectKind Kind { get; }

    public double HardBodyRadiusMeters { get; }

    public bool IsActive { get; }

    public override string ToString() => $"{CatalogNumber} {Name}";
}
