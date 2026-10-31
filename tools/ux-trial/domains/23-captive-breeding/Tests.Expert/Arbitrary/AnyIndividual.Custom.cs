using JustDummies;

namespace CaptiveBreeding.Domain;

// Written by hand, beside the scaffolded file: `any generate Individual --force` never touches it.
public sealed partial class AnyIndividual {

    /// <summary>
    ///     An individual bred in the programme: both parents recorded, born after both of them.
    ///     Parentage and birth date are tied (parents strictly older), so they are set together
    ///     here rather than through two independent <c>With…</c> calls.
    /// </summary>
    public AnyIndividual AsOffspringOf(Individual sire, Individual dam) {
        DateOnly youngestParentBirth = sire.BirthDate > dam.BirthDate ? sire.BirthDate : dam.BirthDate;
        AnyDateOnly bornAfterBothParents = Any.DateOnly().Between(youngestParentBirth.AddYears(1), youngestParentBirth.AddYears(3));

        return new AnyIndividual(_id, _name, _sex, bornAfterBothParents, new FixedValue<StudbookId?>(sire.Id), new FixedValue<StudbookId?>(dam.Id));
    }

}
