namespace TaxRules.Domain;

/// <summary>Satisfied when the taxpayer resides in the jurisdiction (a country covers its subdivisions).</summary>
public sealed class ResidentOfCondition : ICondition, IEquatable<ResidentOfCondition>
{
    public ResidentOfCondition(JurisdictionCode jurisdiction)
    {
        ArgumentNullException.ThrowIfNull(jurisdiction);
        Jurisdiction = jurisdiction;
    }

    public JurisdictionCode Jurisdiction { get; }

    public int Depth => 1;

    public IReadOnlyList<ICondition> Operands => [];

    public bool IsSatisfiedBy(Taxpayer taxpayer, int taxYear)
    {
        ArgumentNullException.ThrowIfNull(taxpayer);
        return Jurisdiction.Covers(taxpayer.Residence);
    }

    public bool Equals(ResidentOfCondition? other) => other is not null && other.Jurisdiction.Equals(Jurisdiction);

    public override bool Equals(object? obj) => Equals(obj as ResidentOfCondition);

    public override int GetHashCode() => Jurisdiction.GetHashCode();

    public override string ToString() => $"resident of {Jurisdiction}";
}
