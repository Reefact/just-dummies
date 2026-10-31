using JustDummies;

namespace CaptiveBreeding.Domain;

public sealed partial class AnyIndividual {

    /// <summary>An offspring of the two given parents, born within three years after the younger one.</summary>
    public AnyIndividual ChildOf(Individual sire, Individual dam) {
        DateOnly youngestParentBirth = sire.BirthDate > dam.BirthDate ? sire.BirthDate : dam.BirthDate;

        return WithSireId(sire.Id)
              .WithDamId(dam.Id)
              .WithBirthDate(d => d.After(youngestParentBirth).BeforeOrEqualTo(youngestParentBirth.AddYears(3)));
    }

}
