namespace TokenLedger.Domain;

/// <summary>A transaction the ledger refused, with the business reason.</summary>
public sealed class LedgerRefusedException : InvalidOperationException
{
    public LedgerRefusedException(RefusalReason reason, string message)
        : base(message)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "A refusal names a defined reason.");
        }

        Reason = reason;
    }

    public RefusalReason Reason { get; }
}
