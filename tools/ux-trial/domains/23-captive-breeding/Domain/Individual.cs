namespace CaptiveBreeding.Domain;

/// <summary>
/// An animal recorded in the studbook. A founder is wild-caught and has no recorded parents;
/// any other individual has both a known sire and a known dam.
/// </summary>
public sealed class Individual
{
    public Individual(StudbookId id, string name, Sex sex, DateOnly birthDate, StudbookId? sireId, StudbookId? damId)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(name);
        if (string.IsNullOrWhiteSpace(name) || name.Length > 40)
        {
            throw new ArgumentException("A house name is 1 to 40 non-blank characters.", nameof(name));
        }

        if (!Enum.IsDefined(sex))
        {
            throw new ArgumentOutOfRangeException(nameof(sex), sex, "Unknown sex.");
        }

        if ((sireId is null) != (damId is null))
        {
            throw new ArgumentException("Parentage is either fully known (sire and dam) or the individual is a founder (neither).");
        }

        if (sireId is not null && sireId == damId)
        {
            throw new ArgumentException("Sire and dam must be different individuals.", nameof(damId));
        }

        if (id == sireId || id == damId)
        {
            throw new ArgumentException("An individual cannot be its own parent.", nameof(id));
        }

        Id = id;
        Name = name;
        Sex = sex;
        BirthDate = birthDate;
        SireId = sireId;
        DamId = damId;
        IsAlive = true;
    }

    public StudbookId Id { get; }

    public string Name { get; }

    public Sex Sex { get; }

    public DateOnly BirthDate { get; }

    public StudbookId? SireId { get; }

    public StudbookId? DamId { get; }

    public bool IsFounder => SireId is null;

    public bool IsAlive { get; private set; }

    public DateOnly? DeathDate { get; private set; }

    public void RecordDeath(DateOnly date)
    {
        if (!IsAlive)
        {
            throw new InvalidOperationException($"{Id} is already recorded as dead.");
        }

        if (date < BirthDate)
        {
            throw new ArgumentOutOfRangeException(nameof(date), date, "Death cannot precede birth.");
        }

        IsAlive = false;
        DeathDate = date;
    }

    public override string ToString() => $"{Id} ({Name}, {Sex})";
}
