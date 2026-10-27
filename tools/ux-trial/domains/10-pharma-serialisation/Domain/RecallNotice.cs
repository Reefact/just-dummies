namespace Serialisation.Domain;

/// <summary>A competent authority's notice recalling one batch.</summary>
public sealed class RecallNotice {

    public RecallNotice(BatchNumber batch, DateOnly issuedOn, DateOnly effectiveFrom, RecallClass recallClass, string reason) {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentOutOfRangeException.ThrowIfLessThan(effectiveFrom, issuedOn);
        if (!Enum.IsDefined(recallClass)) {
            throw new ArgumentOutOfRangeException(nameof(recallClass));
        }
        ArgumentException.ThrowIfNullOrEmpty(reason);
        if (reason.Length > 500) {
            throw new ArgumentException("A recall reason holds at most 500 characters.", nameof(reason));
        }

        Batch         = batch;
        IssuedOn      = issuedOn;
        EffectiveFrom = effectiveFrom;
        Class         = recallClass;
        Reason        = reason;
    }

    public BatchNumber Batch         { get; }
    public DateOnly    IssuedOn      { get; }
    public DateOnly    EffectiveFrom { get; }
    public RecallClass Class         { get; }
    public string      Reason        { get; }

    /// <summary>Class I recalls reach patients; the others stop at pharmacy level.</summary>
    public bool ReachesPatients => Class == RecallClass.ClassI;

}
