namespace InternationalCatalogue.Domain;

/// <summary>Thrown when the catalogue refuses a record that duplicates one it holds.</summary>
public sealed class DuplicateRecordException : InvalidOperationException
{
    public DuplicateRecordException(DuplicateReason reason, RecordId existing, string message)
        : base(message)
    {
        ArgumentNullException.ThrowIfNull(existing);
        if (reason == DuplicateReason.None || !Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "A refusal names a duplicate reason.");
        }

        Reason = reason;
        Existing = existing;
    }

    public DuplicateReason Reason { get; }

    /// <summary>The record already in the catalogue (for a retired id, the record it was merged into).</summary>
    public RecordId Existing { get; }
}
