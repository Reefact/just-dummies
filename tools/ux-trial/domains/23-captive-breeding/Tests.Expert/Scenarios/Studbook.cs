using CaptiveBreeding.Domain;

using JustDummies;

namespace CaptiveBreeding.Tests.Expert.Scenarios;

/// <summary>
///     A pedigree under construction. Every animal is registered with an id the studbook has not
///     used yet, an arbitrary house name and birth date; a test states only the family structure
///     and the sexes its rule is about.
/// </summary>
internal sealed class Studbook {

    public Pedigree Pedigree { get; } = Any.Pedigree().Generate();

    public Individual Male() {
        return Register(Any.Individual().WithSex(Sex.Male));
    }

    public Individual Female() {
        return Register(Any.Individual().WithSex(Sex.Female));
    }

    public Individual Founder() {
        return Register(Any.Individual());
    }

    public Individual SonOf(Individual sire, Individual dam) {
        return Register(Any.Individual().AsOffspringOf(sire, dam).WithSex(Sex.Male));
    }

    public Individual DaughterOf(Individual sire, Individual dam) {
        return Register(Any.Individual().AsOffspringOf(sire, dam).WithSex(Sex.Female));
    }

    public Individual OffspringOf(Individual sire, Individual dam) {
        return Register(Any.Individual().AsOffspringOf(sire, dam));
    }

    public void RecordDeathOf(params Individual[] individuals) {
        foreach (Individual individual in individuals) {
            DateOnly anyDeathDate = Any.DateOnly().Between(individual.BirthDate, individual.BirthDate.AddYears(30)).Generate();
            individual.RecordDeath(anyDeathDate);
        }
    }

    /// <summary>
    ///     A policy for a scenario in which age plays no part: its age window is wide enough for
    ///     every animal of this studbook once <see cref="PlanUnder" /> picks the season.
    /// </summary>
    public static AnyKinshipPolicy AnyPolicy() {
        // Founders are born within five years and each generation within three more, so the whole
        // cohort spans well under twenty years; a minimum age up to 30 keeps the oldest animal
        // below the policy's maximum (51 or more).
        return Any.KinshipPolicy().WithMinimumBreedingAgeYears(age => age.LessThanOrEqualTo(30));
    }

    /// <summary>A draft plan whose season opens once the youngest living animal is of breeding age.</summary>
    public BreedingPlan PlanUnder(KinshipPolicy policy) {
        DateOnly youngestBirth = Pedigree.Living.Max(i => i.BirthDate);
        DateOnly anySeasonStart = Any.DateOnly()
                                     .Between(youngestBirth.AddYears(policy.MinimumBreedingAgeYears), youngestBirth.AddYears(policy.MinimumBreedingAgeYears).AddDays(180))
                                     .Generate();
        if (Pedigree.Living.Any(i => !policy.IsOfBreedingAge(i, anySeasonStart))) {
            throw new InvalidOperationException("Scenario error: the season leaves a living animal out of breeding age; use Studbook.AnyPolicy().");
        }

        return new BreedingPlan(Pedigree, policy, anySeasonStart);
    }

    public Individual Register(AnyIndividual recipe) {
        Individual individual = recipe.WithId(id => id.Except(Pedigree.Individuals.Select(i => i.Id))).Generate();
        Pedigree.Register(individual);

        return individual;
    }

}
