namespace CarbonLedger.Domain;

/// <summary>Raised when an operation would break a registry rule (double holding, double use, unknown account...).</summary>
public sealed class LedgerRuleViolationException : InvalidOperationException
{
    public LedgerRuleViolationException(string message)
        : base(message)
    {
    }
}
