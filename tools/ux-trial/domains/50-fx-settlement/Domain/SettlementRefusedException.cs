namespace FxSettlement.Domain;

/// <summary>A refusal of the settlement batch, carrying its reason.</summary>
public sealed class SettlementRefusedException : InvalidOperationException
{
    public SettlementRefusedException(SettlementRefusalReason reason, string message)
        : base(message)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown refusal reason.");
        }

        Reason = reason;
    }

    public SettlementRefusalReason Reason { get; }
}
