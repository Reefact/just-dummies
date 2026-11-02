namespace TaxRules.Domain;

/// <summary>
/// The evaluation of one taxpayer under one schedule: a line per top-level rule, the gross tax,
/// the credits set against it (never more than the tax: credits are not refundable) and the net tax.
/// </summary>
public sealed class TaxBreakdown
{
    private readonly List<BreakdownLine> lines;

    public TaxBreakdown(TaxpayerId taxpayerId, JurisdictionCode jurisdiction, int year, Money taxableIncome, IEnumerable<BreakdownLine> lines)
    {
        ArgumentNullException.ThrowIfNull(taxpayerId);
        ArgumentNullException.ThrowIfNull(jurisdiction);
        ArgumentNullException.ThrowIfNull(taxableIncome);
        ArgumentNullException.ThrowIfNull(lines);
        List<BreakdownLine> list = lines.ToList();
        if (list.Contains(null!))
        {
            throw new ArgumentException("A breakdown cannot contain a null line.", nameof(lines));
        }

        TaxpayerId = taxpayerId;
        Jurisdiction = jurisdiction;
        Year = year;
        TaxableIncome = taxableIncome;
        this.lines = list;
        GrossTax = Total(list.Where(l => l.Effect == RuleEffect.Tax));
        CreditsAvailable = Total(list.Where(l => l.Effect == RuleEffect.Credit));
        CreditsApplied = Money.Min(GrossTax, CreditsAvailable);
        NetTax = GrossTax.Minus(CreditsApplied);
    }

    public TaxpayerId TaxpayerId { get; }

    public JurisdictionCode Jurisdiction { get; }

    public int Year { get; }

    /// <summary>The taxpayer's taxable income of every kind.</summary>
    public Money TaxableIncome { get; }

    public IReadOnlyList<BreakdownLine> Lines => lines;

    public Money GrossTax { get; }

    public Money CreditsAvailable { get; }

    public Money CreditsApplied { get; }

    public Money NetTax { get; }

    /// <summary>Net tax over taxable income, to four decimals; zero without income.</summary>
    public decimal EffectiveRate => TaxableIncome.IsZero ? 0m : decimal.Round(NetTax.Amount / TaxableIncome.Amount, 4, MidpointRounding.AwayFromZero);

    /// <summary>The line of a rule, top-level or nested.</summary>
    public BreakdownLine? Find(RuleCode code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return lines.Select(l => l.Find(code)).FirstOrDefault(l => l is not null);
    }

    private static Money Total(IEnumerable<BreakdownLine> selected)
    {
        Money total = Money.Zero;
        foreach (BreakdownLine line in selected)
        {
            total = total.Plus(line.Amount);
        }

        return total;
    }
}
