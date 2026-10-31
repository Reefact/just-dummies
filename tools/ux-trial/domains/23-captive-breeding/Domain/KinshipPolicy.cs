namespace CaptiveBreeding.Domain;

/// <summary>
/// The programme's breeding rules: the highest kinship a pair may have, how many pairings a
/// single animal may take part in per season, and the age window in which it may breed.
/// </summary>
public sealed class KinshipPolicy
{
    public KinshipPolicy(KinshipCoefficient maximumPairKinship, int maximumPairingsPerIndividual, int minimumBreedingAgeYears, int maximumBreedingAgeYears)
    {
        ArgumentNullException.ThrowIfNull(maximumPairKinship);
        if (maximumPairKinship.Value > 0.25)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumPairKinship), maximumPairKinship.Value,
                "A policy may not tolerate kinship above 0.25 (full siblings).");
        }

        if (maximumPairingsPerIndividual is < 1 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumPairingsPerIndividual), maximumPairingsPerIndividual, "Between 1 and 10 pairings per individual.");
        }

        if (minimumBreedingAgeYears is < 0 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumBreedingAgeYears), minimumBreedingAgeYears, "Minimum breeding age lies in [0, 50] years.");
        }

        if (maximumBreedingAgeYears <= minimumBreedingAgeYears || maximumBreedingAgeYears > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBreedingAgeYears), maximumBreedingAgeYears,
                "Maximum breeding age must exceed the minimum and be at most 100 years.");
        }

        MaximumPairKinship = maximumPairKinship;
        MaximumPairingsPerIndividual = maximumPairingsPerIndividual;
        MinimumBreedingAgeYears = minimumBreedingAgeYears;
        MaximumBreedingAgeYears = maximumBreedingAgeYears;
    }

    public KinshipCoefficient MaximumPairKinship { get; }

    public int MaximumPairingsPerIndividual { get; }

    public int MinimumBreedingAgeYears { get; }

    public int MaximumBreedingAgeYears { get; }

    public bool IsOfBreedingAge(Individual individual, DateOnly onDate)
    {
        ArgumentNullException.ThrowIfNull(individual);
        int age = onDate.Year - individual.BirthDate.Year;
        if (onDate < individual.BirthDate.AddYears(age))
        {
            age--;
        }

        return age >= MinimumBreedingAgeYears && age <= MaximumBreedingAgeYears;
    }
}
