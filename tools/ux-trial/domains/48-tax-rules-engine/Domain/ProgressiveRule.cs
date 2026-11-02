namespace TaxRules.Domain;

/// <summary>
/// A progressive scale: contiguous brackets from zero, each starting where the previous one
/// ends, the last open-ended, rates never decreasing. The tax is the sum over the brackets,
/// rounded once at the end.
/// </summary>
public sealed class ProgressiveRule : TaxRule
{
    public const int MaxBrackets = 20;

    private readonly List<TaxBracket> brackets;

    public ProgressiveRule(RuleCode code, string label, IncomeKind taxBase, IEnumerable<TaxBracket> brackets, ICondition? condition = null)
        : base(code, label, condition)
    {
        TaxBase = Guard.IncomeKinds(taxBase, nameof(taxBase));
        ArgumentNullException.ThrowIfNull(brackets);
        List<TaxBracket> list = brackets.ToList();
        if (list.Count == 0 || list.Count > MaxBrackets)
        {
            throw new ArgumentException($"A progressive scale has 1 to {MaxBrackets} brackets; got {list.Count}.", nameof(brackets));
        }

        if (list.Contains(null!))
        {
            throw new ArgumentException("A scale cannot contain a null bracket.", nameof(brackets));
        }

        if (!list[0].Lower.IsZero)
        {
            throw new ArgumentException($"The first bracket starts at zero, not {list[0].Lower}.", nameof(brackets));
        }

        for (int i = 1; i < list.Count; i++)
        {
            TaxBracket previous = list[i - 1];
            TaxBracket current = list[i];
            if (previous.Upper is null)
            {
                throw new ArgumentException($"Only the last bracket is open-ended; bracket {i} of {list.Count} is.", nameof(brackets));
            }

            if (!current.Lower.Equals(previous.Upper))
            {
                throw new ArgumentException(
                    $"Brackets are contiguous: bracket {i + 1} starts at {current.Lower}, the previous one ends at {previous.Upper}.",
                    nameof(brackets));
            }

            if (current.Rate.CompareTo(previous.Rate) < 0)
            {
                throw new ArgumentException($"A progressive scale never lowers its rate: {previous.Rate} then {current.Rate}.", nameof(brackets));
            }
        }

        if (!list[^1].IsOpenEnded)
        {
            throw new ArgumentException($"The last bracket is open-ended; it stops at {list[^1].Upper}.", nameof(brackets));
        }

        this.brackets = list;
    }

    public IncomeKind TaxBase { get; }

    public IReadOnlyList<TaxBracket> Brackets => brackets;

    /// <summary>The rate of the bracket the income falls in.</summary>
    public Rate MarginalRateAt(Money income)
    {
        ArgumentNullException.ThrowIfNull(income);
        return brackets.First(b => b.Contains(income)).Rate;
    }

    protected override BreakdownLine Apply(Taxpayer taxpayer, int taxYear)
    {
        Money income = taxpayer.TaxableIncome(TaxBase);
        decimal tax = brackets.Sum(b => b.TaxOn(income));
        return new BreakdownLine(Code, Label, Effect, LineOutcome.Applied, income, Money.RoundedFrom(tax));
    }
}
