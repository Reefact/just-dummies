// Scaffolded by `any`, then rewritten by hand: the three parameters of RaceResult are tied to one another
// (every horse a runner of the race, no horse twice, no scratched horse finishing), so no per-parameter
// recipe can honour them. The generator draws the field in one shuffled order and cuts it into
// "scratched", "finishers" and "the rest". A test that needs a particular order builds the RaceResult itself.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;
using ParimutuelBetting.Domain;

namespace ParimutuelBetting.Domain.Tests.Expert.Generators;

public sealed partial class AnyRaceResult : IAny<RaceResult> {

    private IAny<Race> _race;
    private AnyRace    _raceRecipe;

    private IAny<int> _scratchedCount;
    private IAny<int> _finishersCount;

    public AnyRaceResult()
        : this(new AnyRace(), null, null) { }

    private AnyRaceResult(AnyRace race, IAny<int>? scratchedCount, IAny<int>? finishersCount) {
        _race           = race;
        _raceRecipe     = race;
        _scratchedCount = scratchedCount ?? Any.Int32().Between(0, 3);
        _finishersCount = finishersCount ?? Any.Int32().Between(1, 4);
    }

    private AnyRaceResult(AnyRaceResult from) {
        _race           = from._race;
        _raceRecipe     = from._raceRecipe;
        _scratchedCount = from._scratchedCount;
        _finishersCount = from._finishersCount;
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

    /// <summary>The number of horses scratched before the off (clamped so that one finisher remains).</summary>
    public AnyRaceResult WithScratchedCount(int count) {
        AnyRaceResult copy = new AnyRaceResult(this);

        copy._scratchedCount = Any.Fixed(count);

        return copy;
    }

    /// <summary>The number of horses the official order names (clamped to the runners left after the scratchings).</summary>
    public AnyRaceResult WithFinishersCount(int count) {
        AnyRaceResult copy = new AnyRaceResult(this);

        copy._finishersCount = Any.Fixed(count);

        return copy;
    }

    public RaceResult Generate() {
        Race race = _race.Generate();

        List<HorseNumber> shuffled = Any.PermutationOf(Enumerable.Range(1, race.FieldSize).Select(number => new HorseNumber(number)).ToArray()).Generate();

        int scratched = Math.Min(_scratchedCount.Generate(), race.FieldSize - 1);
        int finishers = Math.Min(_finishersCount.Generate(), race.FieldSize - scratched);

        return new RaceResult(race,
                              shuffled.Skip(scratched).Take(finishers).ToArray(),
                              shuffled.Take(scratched).ToArray());
    }

}
