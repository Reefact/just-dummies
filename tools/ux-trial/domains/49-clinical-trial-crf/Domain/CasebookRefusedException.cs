namespace ClinicalTrial.Domain;

/// <summary>A casebook operation refused by the trial's data-management rules.</summary>
public sealed class CasebookRefusedException : InvalidOperationException
{
    public CasebookRefusedException(CasebookRefusal reason, string message)
        : base(message)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown refusal reason.");
        }

        Reason = reason;
    }

    public CasebookRefusal Reason { get; }
}
