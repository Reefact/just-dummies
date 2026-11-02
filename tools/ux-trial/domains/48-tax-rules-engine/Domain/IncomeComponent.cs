namespace TaxRules.Domain;

/// <summary>
/// One item of a taxpayer's income for the year. Each kind computes its own taxable amount
/// from what was received: deductions, losses and partial inclusions differ by kind.
/// </summary>
public abstract class IncomeComponent
{
    public const int MaxSourceLength = 100;

    protected IncomeComponent(string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        if (source.Length > MaxSourceLength)
        {
            throw new ArgumentException($"A source holds at most {MaxSourceLength} characters.", nameof(source));
        }

        Source = source;
    }

    /// <summary>The payer or activity the income comes from.</summary>
    public string Source { get; }

    /// <summary>Exactly one flag of <see cref="IncomeKind"/>.</summary>
    public abstract IncomeKind Kind { get; }

    public abstract Money TaxableAmount { get; }
}
