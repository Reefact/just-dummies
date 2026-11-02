namespace TaxRules.Domain;

/// <summary>Satisfied when the taxpayer is at least <see cref="Years"/> old on 31 December of the tax year.</summary>
public sealed class AgeAtLeastCondition : ICondition, IEquatable<AgeAtLeastCondition>
{
    public const int MaxYears = 120;

    public AgeAtLeastCondition(int years)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(years);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(years, MaxYears);
        Years = years;
    }

    public int Years { get; }

    public int Depth => 1;

    public IReadOnlyList<ICondition> Operands => [];

    public bool IsSatisfiedBy(Taxpayer taxpayer, int taxYear)
    {
        ArgumentNullException.ThrowIfNull(taxpayer);
        return taxpayer.AgeAtEndOf(taxYear) >= Years;
    }

    public bool Equals(AgeAtLeastCondition? other) => other is not null && other.Years == Years;

    public override bool Equals(object? obj) => Equals(obj as AgeAtLeastCondition);

    public override int GetHashCode() => HashCode.Combine(nameof(AgeAtLeastCondition), Years);

    public override string ToString() => $"aged {Years} or more";
}
