namespace HealthcareClaims.Domain;

/// <summary>A claim the file refuses to accept or to approve, with the business reason.</summary>
public sealed class ClaimRefusedException : InvalidOperationException {

    public ClaimRefusedException(ClaimRefusalReason reason, string message)
        : base(message) {
        if (!Enum.IsDefined(reason)) {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "A refusal names a known reason.");
        }

        Reason = reason;
    }

    public ClaimRefusalReason Reason { get; }

}
