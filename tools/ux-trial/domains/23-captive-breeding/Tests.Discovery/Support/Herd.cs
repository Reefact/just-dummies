using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Generators;
using JustDummies;

namespace CaptiveBreeding.Tests.Support;

/// <summary>
///     Builds a pedigree one animal at a time. Identifiers and house names are dummies (the
///     scenarios never look at them); sex, dates and parentage are what each test is about, so
///     they are always passed in.
/// </summary>
internal sealed class Herd {

    private readonly HashSet<StudbookId> _used = [];

    public Herd() {
        Pedigree = new Pedigree(Any.String().NotBlank().WithMaxLength(60).Generate(),
                                Any.String().NotBlank().WithMaxLength(60).Generate());
    }

    public Pedigree Pedigree { get; }

    public StudbookId Founder(Sex sex, DateOnly birthDate) {
        return Register(sex, birthDate, null, null);
    }

    public StudbookId Offspring(Sex sex, DateOnly birthDate, StudbookId sireId, StudbookId damId) {
        return Register(sex, birthDate, sireId, damId);
    }

    public StudbookId NewUnregisteredId() {
        return NextId();
    }

    public void Kill(StudbookId id, DateOnly deathDate) {
        Pedigree.Get(id).RecordDeath(deathDate);
    }

    private StudbookId Register(Sex sex, DateOnly birthDate, StudbookId? sireId, StudbookId? damId) {
        StudbookId id = NextId();

        Individual individual = Any.Individual()
                                   .WithId(id)
                                   .WithSex(sex)
                                   .WithBirthDate(birthDate)
                                   .WithSireId(sireId)
                                   .WithDamId(damId)
                                   .Generate();
        Pedigree.Register(individual);

        return id;
    }

    private StudbookId NextId() {
        StudbookId id = Any.StudbookId().Generate();
        while (!_used.Add(id)) {
            id = Any.StudbookId().Generate();
        }

        return id;
    }

}
