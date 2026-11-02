namespace ShiftPlanning.Domain;

/// <summary>Thrown when the roster refuses an operation; <see cref="Reason"/> says which rule refused it.</summary>
public sealed class RosterRefusedException : InvalidOperationException
{
    public RosterRefusedException(RefusalReason reason, string message)
        : base(message)
    {
        if (reason == RefusalReason.None || !Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "A refusal names a defined reason.");
        }

        Reason = reason;
    }

    public RefusalReason Reason { get; }
}
