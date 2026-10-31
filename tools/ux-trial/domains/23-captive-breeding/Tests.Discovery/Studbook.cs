using CaptiveBreeding.Domain;

using JustDummies;

namespace CaptiveBreeding.Tests.Discovery;

/// <summary>
///     Builds a pedigree for a test. Ids, names and birth dates are dummies; what a test cares about
///     is who descends from whom, the sexes and who is alive.
/// </summary>
internal sealed class Studbook {

    private static readonly DateOnly EarliestFounderBirth = new DateOnly(1990, 1, 1);
    private static readonly DateOnly LatestFounderBirth   = new DateOnly(2000, 12, 31);

    private readonly Queue<StudbookId> _unusedIds;

    public Studbook() {
        _unusedIds = new Queue<StudbookId>(Any.ArrayOf(Any.StudbookId()).WithCount(30).Distinct().Generate());
        Pedigree   = Any.Pedigree().Generate();
    }

    public Pedigree Pedigree { get; }

    public Individual Founder(Sex sex) {
        Individual founder = Any.Individual()
                                .WithId(_unusedIds.Dequeue())
                                .WithSex(sex)
                                .WithBirthDate(d => d.Between(EarliestFounderBirth, LatestFounderBirth))
                                .Generate();
        Pedigree.Register(founder);

        return founder;
    }

    public Individual Offspring(Individual sire, Individual dam, Sex sex) {
        Individual offspring = Any.Individual()
                                  .WithId(_unusedIds.Dequeue())
                                  .WithSex(sex)
                                  .ChildOf(sire, dam)
                                  .Generate();
        Pedigree.Register(offspring);

        return offspring;
    }

    public StudbookId UnregisteredId() {
        return _unusedIds.Dequeue();
    }

}
