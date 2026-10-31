namespace CaptiveBreeding.Domain;

/// <summary>Raised when a proposed pairing breaks the programme's breeding rules.</summary>
public sealed class PairingRefusedException : Exception
{
    public PairingRefusedException(StudbookId sireId, StudbookId damId, string reason)
        : base($"Pairing {sireId} x {damId} refused: {reason}")
    {
        SireId = sireId;
        DamId = damId;
        Reason = reason;
    }

    public StudbookId SireId { get; }

    public StudbookId DamId { get; }

    public string Reason { get; }
}
