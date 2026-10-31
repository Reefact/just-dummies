using JustDummies;

namespace ParimutuelBetting.Domain;

public sealed partial class AnyHorseNumber {

    /// <summary>A saddle-cloth number declared on the race card: 1 to the field size.</summary>
    public AnyHorseNumber RunnerOf(Race race) {
        return WithValue(value => value.LessThanOrEqualTo(race.FieldSize));
    }

}
