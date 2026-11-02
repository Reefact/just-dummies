namespace TaxRules.Domain;

/// <summary>Satisfied when the household counts at least <see cref="Members"/> people (2 to 20).</summary>
public sealed class HouseholdSizeAtLeastCondition : ICondition, IEquatable<HouseholdSizeAtLeastCondition>
{
    public HouseholdSizeAtLeastCondition(int members)
    {
        if (members < 2 || members > Taxpayer.MaxHouseholdSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(members),
                members,
                $"A household-size condition asks for 2 to {Taxpayer.MaxHouseholdSize} members; every household has one.");
        }

        Members = members;
    }

    public int Members { get; }

    public int Depth => 1;

    public IReadOnlyList<ICondition> Operands => [];

    public bool IsSatisfiedBy(Taxpayer taxpayer, int taxYear)
    {
        ArgumentNullException.ThrowIfNull(taxpayer);
        return taxpayer.HouseholdSize >= Members;
    }

    public bool Equals(HouseholdSizeAtLeastCondition? other) => other is not null && other.Members == Members;

    public override bool Equals(object? obj) => Equals(obj as HouseholdSizeAtLeastCondition);

    public override int GetHashCode() => HashCode.Combine(nameof(HouseholdSizeAtLeastCondition), Members);

    public override string ToString() => $"household of {Members} or more";
}
