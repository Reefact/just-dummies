namespace CaptiveBreeding.Domain;

/// <summary>A proposed mating: a sire, a dam and the kinship between them.</summary>
public sealed class BreedingPair : IEquatable<BreedingPair>
{
    public BreedingPair(StudbookId sireId, StudbookId damId, KinshipCoefficient kinship)
    {
        ArgumentNullException.ThrowIfNull(sireId);
        ArgumentNullException.ThrowIfNull(damId);
        ArgumentNullException.ThrowIfNull(kinship);
        if (sireId == damId)
        {
            throw new ArgumentException("An individual cannot be paired with itself.", nameof(damId));
        }

        if (kinship.Value > 0.5)
        {
            throw new ArgumentOutOfRangeException(nameof(kinship), kinship.Value, "Two distinct individuals cannot have a kinship above 0.5.");
        }

        SireId = sireId;
        DamId = damId;
        Kinship = kinship;
    }

    public StudbookId SireId { get; }

    public StudbookId DamId { get; }

    public KinshipCoefficient Kinship { get; }

    public bool Involves(StudbookId id) => SireId == id || DamId == id;

    public bool Equals(BreedingPair? other) => other is not null && SireId == other.SireId && DamId == other.DamId;

    public override bool Equals(object? obj) => Equals(obj as BreedingPair);

    public override int GetHashCode() => HashCode.Combine(SireId, DamId);

    public override string ToString() => $"{SireId} x {DamId} (f = {Kinship})";
}
