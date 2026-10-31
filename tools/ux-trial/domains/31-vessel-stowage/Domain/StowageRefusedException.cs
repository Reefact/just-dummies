namespace VesselStowage.Domain;

public sealed class StowageRefusedException : InvalidOperationException
{
    public StowageRefusedException(StowageViolation violation, string message)
        : base(message)
    {
        if (violation == StowageViolation.None)
        {
            throw new ArgumentOutOfRangeException(nameof(violation), violation, "A refusal names a violation.");
        }

        Violation = violation;
    }

    public StowageViolation Violation { get; }
}
