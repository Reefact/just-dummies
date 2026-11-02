namespace TaxRules.Domain;

/// <summary>A tax schedule refused an operation; <see cref="Reason"/> says why.</summary>
public sealed class TaxScheduleException : InvalidOperationException
{
    public TaxScheduleException(ScheduleRefusal reason, string message)
        : base(message)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown refusal reason.");
        }

        Reason = reason;
    }

    public ScheduleRefusal Reason { get; }
}
