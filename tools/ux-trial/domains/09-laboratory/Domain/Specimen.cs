namespace Laboratory.Domain;

/// <summary>A collected sample. It must reach the laboratory after collection and within its stability window.</summary>
public sealed class Specimen {

    public static readonly TimeSpan StabilityWindow = TimeSpan.FromHours(72);

    public Specimen(SampleId id, SpecimenType type, DateTimeOffset collectedAt, DateTimeOffset receivedAt) {
        ArgumentNullException.ThrowIfNull(id);
        if (!Enum.IsDefined(type)) {
            throw new ArgumentOutOfRangeException(nameof(type));
        }
        if (receivedAt < collectedAt) {
            throw new ArgumentException("A specimen cannot be received before it is collected.", nameof(receivedAt));
        }
        if (receivedAt - collectedAt > StabilityWindow) {
            throw new ArgumentException("The specimen reached the laboratory outside its stability window.", nameof(receivedAt));
        }

        Id          = id;
        Type        = type;
        CollectedAt = collectedAt;
        ReceivedAt  = receivedAt;
    }

    public SampleId       Id          { get; }
    public SpecimenType   Type        { get; }
    public DateTimeOffset CollectedAt { get; }
    public DateTimeOffset ReceivedAt  { get; }

}
