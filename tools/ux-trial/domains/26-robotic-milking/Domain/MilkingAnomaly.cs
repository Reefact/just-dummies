namespace RoboticMilking.Domain;

/// <summary>An anomaly reported during a session. Some concern one quarter, others the whole animal or machine.</summary>
public sealed class MilkingAnomaly
{
    /// <param name="kind">What happened.</param>
    /// <param name="quarter">The quarter concerned: required for quarter-level kinds, forbidden for session-level ones.</param>
    /// <param name="elapsed">Time since the session started; not negative.</param>
    public MilkingAnomaly(AnomalyKind kind, UdderQuarter? quarter, TimeSpan elapsed)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown anomaly kind.");
        }

        if (quarter is { } q && !Enum.IsDefined(q))
        {
            throw new ArgumentOutOfRangeException(nameof(quarter), quarter, "Unknown udder quarter.");
        }

        if (IsQuarterLevel(kind) && quarter is null)
        {
            throw new ArgumentException($"A {kind} anomaly names the quarter it concerns.", nameof(quarter));
        }

        if (!IsQuarterLevel(kind) && quarter is not null)
        {
            throw new ArgumentException($"A {kind} anomaly concerns the whole session, not quarter {quarter}.", nameof(quarter));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(elapsed, TimeSpan.Zero);

        Kind = kind;
        Quarter = quarter;
        Elapsed = elapsed;
    }

    public AnomalyKind Kind { get; }

    public UdderQuarter? Quarter { get; }

    public TimeSpan Elapsed { get; }

    /// <summary>A critical anomaly forces the session into the safe state at once.</summary>
    public bool IsCritical => Kind is AnomalyKind.AnimalAgitation or AnomalyKind.VacuumLoss or AnomalyKind.EmergencyStop;

    /// <summary>A diverting anomaly sends the quarter's milk to the discard line instead of the tank.</summary>
    public bool DivertsMilk => Kind is AnomalyKind.HighConductivity or AnomalyKind.BloodInMilk;

    public static bool IsQuarterLevel(AnomalyKind kind) =>
        kind is AnomalyKind.CupKickOff or AnomalyKind.HighConductivity or AnomalyKind.BloodInMilk;
}
