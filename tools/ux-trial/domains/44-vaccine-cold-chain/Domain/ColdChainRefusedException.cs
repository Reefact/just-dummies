namespace ColdChain.Domain;

/// <summary>An operation the consignment refuses in its current state, with the reason.</summary>
public sealed class ColdChainRefusedException : InvalidOperationException
{
    public ColdChainRefusedException(ColdChainRefusal refusal, string message)
        : base(message)
    {
        if (!Enum.IsDefined(refusal))
        {
            throw new ArgumentOutOfRangeException(nameof(refusal), refusal, "Unknown refusal.");
        }

        Refusal = refusal;
    }

    public ColdChainRefusal Refusal { get; }
}
