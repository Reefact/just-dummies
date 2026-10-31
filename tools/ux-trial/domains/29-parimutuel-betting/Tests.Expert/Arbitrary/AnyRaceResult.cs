// Scaffolded by `any` (JustDummies), then reshaped by hand: the finishing order and the scratched
// horses share one invariant with the race (distinct runners of that race, never both finishing and
// scratched), which no per-parameter recipe can honour. Generate() deals the runners of the drawn race
// into the two lists instead. Do not `any generate RaceResult --force` over it.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>An arbitrary official result: some runners finish, some may have been scratched.</summary>
public sealed partial class AnyRaceResult : IAny<RaceResult> {

    private readonly IAny<Race> _race;
    private readonly bool       _withScratchings;
    private readonly int        _minimumFinishers;

    public AnyRaceResult()
        : this(race: new AnyRace(), withScratchings: true, minimumFinishers: 1) { }

    private AnyRaceResult(IAny<Race> race, bool withScratchings, int minimumFinishers) {
        _race             = race;
        _withScratchings  = withScratchings;
        _minimumFinishers = minimumFinishers;
    }

    public AnyRaceResult WithRace(Race value) {
        return new AnyRaceResult(new FixedValue<Race>(value), _withScratchings, _minimumFinishers);
    }

    public AnyRaceResult WithRace(Func<AnyRace, AnyRace> configure) {
        return new AnyRaceResult(configure(new AnyRace()), _withScratchings, _minimumFinishers);
    }

    /// <summary>Every declared runner went to post.</summary>
    public AnyRaceResult WithoutScratchings() {
        return new AnyRaceResult(_race, withScratchings: false, _minimumFinishers);
    }

    /// <summary>A result that can settle any bet type: at least three horses past the post.</summary>
    public AnyRaceResult SettlingEveryBetType() {
        return new AnyRaceResult(_race, _withScratchings, minimumFinishers: 3);
    }

    public RaceResult Generate() {
        Race race = _race.Generate();

        // The whole card in an arbitrary order, then dealt: scratched horses first, finishers next.
        List<HorseNumber> card       = Any.ListOf(new AnyHorseNumber().RunnerOf(race)).WithCount(race.FieldSize).Distinct().Generate();
        int               scratched  = _withScratchings ? Any.Int32().Between(0, race.FieldSize - _minimumFinishers).Generate() : 0;
        int               finishers  = Any.Int32().Between(_minimumFinishers, race.FieldSize - scratched).Generate();

        return new RaceResult(race,
                              card.Skip(scratched).Take(finishers).ToArray(),
                              card.Take(scratched).ToArray());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
