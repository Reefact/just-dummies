namespace NetworkAcl.Domain;

/// <summary>A change the policy refuses, with the reason.</summary>
public sealed class PolicyRefusedException : InvalidOperationException
{
    public PolicyRefusedException(PolicyRefusalReason reason, string message)
        : base(message)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown refusal reason.");
        }

        Reason = reason;
    }

    public PolicyRefusalReason Reason { get; }
}
