using JustDummies;

namespace CaptiveBreeding.Domain;

public sealed partial class AnyIndividual {

    /// <summary>A founder: wild-caught, no recorded parents (the default recipe).</summary>
    public AnyIndividual AsFounder() {
        return new AnyIndividual(_id, _name, _sex, _birthDate, new FixedValue<StudbookId?>(null), new FixedValue<StudbookId?>(null));
    }

    /// <summary>Born to these two parents, after both of them.</summary>
    public AnyIndividual WithParents(Individual sire, Individual dam) {
        DateOnly youngestParent = sire.BirthDate > dam.BirthDate ? sire.BirthDate : dam.BirthDate;
        return new AnyIndividual(_id, _name, _sex,
                                 Any.DateOnly().Between(youngestParent.AddYears(2), youngestParent.AddYears(6)),
                                 new FixedValue<StudbookId?>(sire.Id), new FixedValue<StudbookId?>(dam.Id));
    }

}
