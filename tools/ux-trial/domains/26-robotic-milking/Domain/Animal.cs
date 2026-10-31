namespace RoboticMilking.Domain;

/// <summary>A lactating cow as the milking robot knows her.</summary>
public sealed class Animal
{
    public const int MaxLactationNumber = 20;
    public const int MaxDaysInMilk = 999;

    private readonly HashSet<UdderQuarter> _blindQuarters;

    /// <param name="id">The ear-tag identifier.</param>
    /// <param name="lactationNumber">The current lactation, 1 for a first-calf heifer.</param>
    /// <param name="daysInMilk">Days since the last calving.</param>
    /// <param name="blindQuarters">Quarters dried off or damaged, never prepared nor attached.</param>
    /// <param name="lastMilkedAt">When the previous session ended, or <c>null</c> for a cow never milked by the robot.</param>
    public Animal(
        AnimalId id,
        int lactationNumber,
        int daysInMilk,
        IEnumerable<UdderQuarter> blindQuarters,
        DateTimeOffset? lastMilkedAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(blindQuarters);
        ArgumentOutOfRangeException.ThrowIfLessThan(lactationNumber, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(lactationNumber, MaxLactationNumber);
        ArgumentOutOfRangeException.ThrowIfNegative(daysInMilk);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(daysInMilk, MaxDaysInMilk);

        HashSet<UdderQuarter> blind = new();
        foreach (UdderQuarter quarter in blindQuarters)
        {
            if (!Enum.IsDefined(quarter))
            {
                throw new ArgumentOutOfRangeException(nameof(blindQuarters), quarter, "Unknown udder quarter.");
            }

            blind.Add(quarter);
        }

        if (blind.Count == Enum.GetValues<UdderQuarter>().Length)
        {
            throw new ArgumentException("A cow with four blind quarters cannot be milked.", nameof(blindQuarters));
        }

        Id = id;
        LactationNumber = lactationNumber;
        DaysInMilk = daysInMilk;
        _blindQuarters = blind;
        LastMilkedAt = lastMilkedAt;
    }

    public AnimalId Id { get; }

    public int LactationNumber { get; }

    public int DaysInMilk { get; }

    public IReadOnlySet<UdderQuarter> BlindQuarters => _blindQuarters;

    public DateTimeOffset? LastMilkedAt { get; }

    public IReadOnlyList<UdderQuarter> MilkableQuarters =>
        Enum.GetValues<UdderQuarter>().Where(q => !_blindQuarters.Contains(q)).ToList();

    public bool IsMilkable(UdderQuarter quarter) => !_blindQuarters.Contains(quarter);
}
