using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Expert.Generators;

using JustDummies;

namespace CaptiveBreeding.Tests.Expert.Support;

/// <summary>
///     Test data builder for a studbook whose birth dates respect the generations: every individual is
///     born in the age band of its generation, counted back from <see cref="SeasonStart" />, so a parent is
///     always strictly older than its offspring and everyone is of breeding age for <see cref="AnyPolicy" />.
///     Ids, names and the exact days are drawn: the tests built on it care about who descends from whom.
/// </summary>
internal sealed class Herd {

    /// <summary>The age window <see cref="AnyPolicy" /> pins, which every generation's band falls inside.</summary>
    public const int MinimumBreedingAgeYears = 2;
    public const int MaximumBreedingAgeYears = 20;

    // Age bands, in years before the season start, one per generation: strictly disjoint and ordered.
    private static readonly (int Oldest, int Youngest)[] GenerationBands = [(20, 17), (15, 12), (10, 7), (5, 2)];

    private readonly Dictionary<StudbookId, int> _generations = [];
    private readonly List<string>                _usedIds     = [];

    public Herd() {
        SeasonStart = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31)).Generate();
        Pedigree    = Any.Pedigree().Generate();
    }

    public DateOnly SeasonStart { get; }

    public Pedigree Pedigree { get; }

    /// <summary>A policy whose breeding-age window is the herd's; the rest of the policy is drawn.</summary>
    public AnyKinshipPolicy AnyPolicy() {
        return Any.KinshipPolicy().WithMinimumBreedingAgeYears(MinimumBreedingAgeYears).WithMaximumBreedingAgeYears(MaximumBreedingAgeYears);
    }

    public BreedingPlan PlanWith(KinshipPolicy policy) {
        return new BreedingPlan(Pedigree, policy, SeasonStart);
    }

    /// <summary>An id that no individual of this herd carries, registered or not.</summary>
    public StudbookId UnusedId() {
        StudbookId id = Any.StudbookId().WithValue(value => value.Except(_usedIds)).Generate();
        _usedIds.Add(id.Value);

        return id;
    }

    public Individual Founder(Sex sex) {
        return Register(Draw(sex, generation: 0).Generate(), generation: 0);
    }

    public Individual Founder(Sex sex, DateOnly birthDate) {
        return Register(Draw(sex, generation: 0).WithBirthDate(birthDate).Generate(), generation: 0);
    }

    public Individual Offspring(Individual sire, Individual dam, Sex sex) {
        int generation = 1 + Math.Max(_generations[sire.Id], _generations[dam.Id]);

        return Register(Draw(sex, generation).WithSireId(sire.Id).WithDamId(dam.Id).Generate(), generation);
    }

    public Individual Offspring(Individual sire, Individual dam, Sex sex, DateOnly birthDate) {
        int generation = 1 + Math.Max(_generations[sire.Id], _generations[dam.Id]);

        return Register(Draw(sex, generation).WithBirthDate(birthDate).WithSireId(sire.Id).WithDamId(dam.Id).Generate(), generation);
    }

    public Individual Son(Individual sire, Individual dam) => Offspring(sire, dam, Sex.Male);

    public Individual Daughter(Individual sire, Individual dam) => Offspring(sire, dam, Sex.Female);

    public Individual Male() => Founder(Sex.Male);

    public Individual Female() => Founder(Sex.Female);

    /// <summary>Records a death one drawn day after the birth, at most a year later.</summary>
    public void Dies(Individual individual) {
        individual.RecordDeath(Any.DateOnly().Between(individual.BirthDate.AddDays(1), individual.BirthDate.AddYears(1)).Generate());
    }

    private AnyIndividual Draw(Sex sex, int generation) {
        if (generation >= GenerationBands.Length) { throw new InvalidOperationException("A herd has " + GenerationBands.Length + " generations at most."); }

        (int oldest, int youngest) = GenerationBands[generation];

        return Any.Individual()
                  .WithId(UnusedId())
                  .WithSex(sex)
                  .WithBirthDate(birthDate => birthDate.Between(SeasonStart.AddYears(-oldest), SeasonStart.AddYears(-youngest)));
    }

    private Individual Register(Individual individual, int generation) {
        _generations[individual.Id] = generation;
        Pedigree.Register(individual);

        return individual;
    }

}
