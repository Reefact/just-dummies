namespace PassiveComponents.Domain;

/// <summary>A substitution the rule refuses; <see cref="Reason"/> names the first condition that failed.</summary>
public sealed class SubstitutionRefusedException : InvalidOperationException
{
    public SubstitutionRefusedException(SubstitutionRefusal reason, string message)
        : base(message)
    {
        if (reason == SubstitutionRefusal.None || !Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "A refusal names a defined reason other than None.");
        }

        Reason = reason;
    }

    public SubstitutionRefusal Reason { get; }
}
