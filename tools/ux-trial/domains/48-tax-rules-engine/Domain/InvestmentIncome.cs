namespace TaxRules.Domain;

/// <summary>Interest or dividends. Dividends are included at <see cref="DividendInclusion"/> of their gross amount.</summary>
public sealed class InvestmentIncome : IncomeComponent, IEquatable<InvestmentIncome>
{
    public static readonly Rate DividendInclusion = new(0.60m);

    public InvestmentIncome(string payer, Money gross, InvestmentKind investmentKind)
        : base(payer)
    {
        ArgumentNullException.ThrowIfNull(gross);
        if (!Enum.IsDefined(investmentKind))
        {
            throw new ArgumentOutOfRangeException(nameof(investmentKind), investmentKind, "Unknown investment kind.");
        }

        Gross = gross;
        InvestmentKind = investmentKind;
    }

    public Money Gross { get; }

    public InvestmentKind InvestmentKind { get; }

    public override IncomeKind Kind => IncomeKind.Investment;

    public override Money TaxableAmount => InvestmentKind == InvestmentKind.Dividend ? Gross.Times(DividendInclusion) : Gross;

    public bool Equals(InvestmentIncome? other) =>
        other is not null && other.Source == Source && other.Gross.Equals(Gross) && other.InvestmentKind == InvestmentKind;

    public override bool Equals(object? obj) => Equals(obj as InvestmentIncome);

    public override int GetHashCode() => HashCode.Combine(Source, Gross, InvestmentKind);

    public override string ToString() => $"{InvestmentKind} from {Source}: {Gross}";
}
