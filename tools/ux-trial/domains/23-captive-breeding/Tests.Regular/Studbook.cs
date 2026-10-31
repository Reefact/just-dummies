using CaptiveBreeding.Domain;

using JustDummies;

namespace CaptiveBreeding.Tests.Regular;

/// <summary>
///     Builds a pedigree whose shape (who descends from whom) the test chooses, and whose ids,
///     names and exact birth dates are drawn.
/// </summary>
internal sealed class Studbook {

    public static readonly DateOnly EarliestFounderBirth = new DateOnly(2000, 1, 1);
    public static readonly DateOnly LatestFounderBirth   = new DateOnly(2005, 12, 31);

    public Studbook() {
        Pedigree = Arbitrary.Pedigree().Generate();
    }

    public Pedigree Pedigree { get; }

    public Individual Founder(Sex sex) {
        Individual anyFounder = Arbitrary.Individual()
                                         .WithSex(sex)
                                         .WithBirthDate(d => d.Between(EarliestFounderBirth, LatestFounderBirth))
                                         .Generate();
        Pedigree.Register(anyFounder);

        return anyFounder;
    }

    public Individual Offspring(Individual sire, Individual dam, Sex sex) {
        Individual anyOffspring = Arbitrary.Individual().WithSex(sex).WithParents(sire, dam).Generate();
        Pedigree.Register(anyOffspring);

        return anyOffspring;
    }

}
