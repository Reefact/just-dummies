using CaptiveBreeding.Domain;

using JustDummies;

namespace CaptiveBreeding.Domain.Tests.Regular.Support;

/// <summary>Builds a pedigree whose shape the test chooses; everything it does not choose is drawn.</summary>
internal sealed class PedigreeBuilder {

    private readonly Queue<StudbookId> _ids;

    public PedigreeBuilder() {
        _ids = new Queue<StudbookId>(Any.ListOf(new AnyStudbookId()).WithCount(60).Distinct().Generate());

        Pedigree = Any.Pedigree().Generate();
    }

    public Pedigree Pedigree { get; }

    /// <summary>An id that no individual of this builder carries.</summary>
    public StudbookId NextId() {
        return _ids.Dequeue();
    }

    public Individual Founder(Sex sex) {
        DateOnly birth = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2004, 12, 31)).Generate();

        return Founder(sex, birth);
    }

    public Individual Founder(Sex sex, DateOnly birth) {
        Individual founder = Any.Individual().WithId(NextId()).WithSex(sex).WithBirthDate(birth).Generate();
        Pedigree.Register(founder);

        return founder;
    }

    public Individual Child(Individual sire, Individual dam, Sex sex) {
        DateOnly olderParent = sire.BirthDate > dam.BirthDate ? sire.BirthDate : dam.BirthDate;
        DateOnly birth       = Any.DateOnly().Between(olderParent.AddDays(1), olderParent.AddYears(4)).Generate();

        return Child(sire, dam, sex, birth);
    }

    public Individual Child(Individual sire, Individual dam, Sex sex, DateOnly birth) {
        Individual child = Any.Individual().WithId(NextId()).WithSex(sex).WithBirthDate(birth).ChildOf(sire.Id, dam.Id).Generate();
        Pedigree.Register(child);

        return child;
    }

}
