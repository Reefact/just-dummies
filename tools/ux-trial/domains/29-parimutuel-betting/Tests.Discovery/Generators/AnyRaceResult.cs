// Scaffolded by `any` (JustDummies), then reshaped by hand: the finishing order and the scratched horses are
// derived from the race in Generate(), because both depend on its field size.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RaceResult" /> values: a race, then a shuffle of its runners split into
///     scratched horses, finishers and horses that did not place (they appear nowhere).
/// </summary>
public sealed partial class AnyRaceResult : IAny<RaceResult> {

    private IAny<Race> _race;

    private AnyRace _raceRecipe;

    public AnyRaceResult()
        : this(race: AnyValidRace()) { }

    private static AnyRace AnyValidRace() {
        return new AnyRace();
    }

    private AnyRaceResult(AnyRace race) {
        _race       = race;
        _raceRecipe = race;
    }

    private AnyRaceResult(AnyRaceResult from) {
        _race       = from._race;
        _raceRecipe = from._raceRecipe;
    }

    public AnyRaceResult WithRace(Race value) {
        AnyRaceResult copy = new AnyRaceResult(this);

        copy._race = Any.Fixed(value);

        return copy;
    }

    public AnyRaceResult WithRace(Func<AnyRace, AnyRace> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRaceResult copy = new AnyRaceResult(this);

        copy._raceRecipe = configure(_raceRecipe);
        copy._race       = copy._raceRecipe;

        return copy;
    }

    public RaceResult Generate() {
        Race race = _race.Generate();

        List<HorseNumber> runners = Enumerable.Range(1, race.FieldSize).Select(n => new HorseNumber(n)).ToList();
        List<HorseNumber> shuffled = Any.PermutationOf(runners).Generate();

        int scratchedCount = Any.Int32().Between(0, race.FieldSize - 1).Generate();
        int finisherCount  = Any.Int32().Between(1, race.FieldSize - scratchedCount).Generate();

        return new RaceResult(race,
                              shuffled.Skip(scratchedCount).Take(finisherCount).ToArray(),
                              shuffled.Take(scratchedCount).ToArray());
    }

}
