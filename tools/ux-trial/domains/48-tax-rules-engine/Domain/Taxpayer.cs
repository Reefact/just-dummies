namespace TaxRules.Domain;

/// <summary>A person assessed under a schedule: identity, birth date, residence, household and the year's income.</summary>
public sealed class Taxpayer
{
    public const int MaxHouseholdSize = 20;
    public const int MaxIncomeComponents = 30;
    public static readonly DateOnly EarliestDateOfBirth = new(1900, 1, 1);

    private readonly List<IncomeComponent> incomes;

    public Taxpayer(TaxpayerId id, DateOnly dateOfBirth, JurisdictionCode residence, int householdSize, IEnumerable<IncomeComponent> incomes)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(residence);
        ArgumentNullException.ThrowIfNull(incomes);
        if (dateOfBirth < EarliestDateOfBirth)
        {
            throw new ArgumentOutOfRangeException(nameof(dateOfBirth), dateOfBirth, $"A date of birth is on or after {EarliestDateOfBirth:yyyy-MM-dd}.");
        }

        if (householdSize < 1 || householdSize > MaxHouseholdSize)
        {
            throw new ArgumentOutOfRangeException(nameof(householdSize), householdSize, $"A household counts 1 to {MaxHouseholdSize} members.");
        }

        List<IncomeComponent> list = incomes.ToList();
        if (list.Contains(null!))
        {
            throw new ArgumentException("An income list cannot contain null.", nameof(incomes));
        }

        if (list.Count > MaxIncomeComponents)
        {
            throw new ArgumentException($"A taxpayer declares at most {MaxIncomeComponents} income components.", nameof(incomes));
        }

        Id = id;
        DateOfBirth = dateOfBirth;
        Residence = residence;
        HouseholdSize = householdSize;
        this.incomes = list;
    }

    public TaxpayerId Id { get; }

    public DateOnly DateOfBirth { get; }

    public JurisdictionCode Residence { get; }

    /// <summary>Members of the household, the taxpayer included.</summary>
    public int HouseholdSize { get; }

    public IReadOnlyList<IncomeComponent> Incomes => incomes;

    /// <summary>Age in completed years on 31 December of the tax year.</summary>
    public int AgeAtEndOf(int taxYear)
    {
        if (taxYear < DateOfBirth.Year)
        {
            throw new ArgumentOutOfRangeException(nameof(taxYear), taxYear, $"The taxpayer was born in {DateOfBirth.Year}.");
        }

        return taxYear - DateOfBirth.Year;
    }

    /// <summary>The sum of the taxable amounts of every component whose kind is among <paramref name="kinds"/>.</summary>
    public Money TaxableIncome(IncomeKind kinds)
    {
        Guard.IncomeKinds(kinds, nameof(kinds));
        Money total = Money.Zero;
        foreach (IncomeComponent income in incomes.Where(i => (kinds & i.Kind) != 0))
        {
            total = total.Plus(income.TaxableAmount);
        }

        return total;
    }

    public override string ToString() => $"Taxpayer {Id} ({Residence})";
}
