// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RaceResult --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RaceResult" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRaceResult : IAny<RaceResult> {

    private IAny<Race>                             _race;
    private IAny<IReadOnlyList<HorseNumber>>       _finishingOrder;
    private IAny<IReadOnlyCollection<HorseNumber>> _scratched;

    private AnyRace              _raceRecipe;
    private AnyList<HorseNumber> _finishingOrderRecipe;
    private AnyList<HorseNumber> _scratchedRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRaceResult()
        : this(race:           AnyValidRace(),
               finishingOrder: AnyValidFinishingOrder(),
               scratched:      AnyValidScratched()) { }

    private static AnyRace AnyValidRace() {
        return new AnyRace();
    }

    private static AnyList<HorseNumber> AnyValidFinishingOrder() {
        return Any.ListOf(new AnyHorseNumber()).NonEmpty();
    }

    private static AnyList<HorseNumber> AnyValidScratched() {
        return Any.ListOf(new AnyHorseNumber());
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRaceResult(AnyRace              race,
                          AnyList<HorseNumber> finishingOrder,
                          AnyList<HorseNumber> scratched) {
        _race                 = race;
        _raceRecipe           = race;
        _finishingOrder       = finishingOrder;
        _finishingOrderRecipe = finishingOrder;
        _scratched            = scratched;
        _scratchedRecipe      = scratched;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRaceResult(AnyRaceResult from) {
        _race                 = from._race;
        _raceRecipe           = from._raceRecipe;
        _finishingOrder       = from._finishingOrder;
        _finishingOrderRecipe = from._finishingOrderRecipe;
        _scratched            = from._scratched;
        _scratchedRecipe      = from._scratchedRecipe;
    }

    /// <summary>Pins <c>race</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRaceResult WithRace(Race value) {
        AnyRaceResult copy = new AnyRaceResult(this);

        copy._race = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>race</c> is drawn from — <c>AnyValidRace()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRace, AnyRace&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRaceResult WithRace(Func<AnyRace, AnyRace> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRaceResult copy = new AnyRaceResult(this);

        copy._raceRecipe = configure(_raceRecipe);
        copy._race       = copy._raceRecipe;

        return copy;
    }

    /// <summary>Pins <c>finishingOrder</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRaceResult WithFinishingOrder(IReadOnlyList<HorseNumber> value) {
        AnyRaceResult copy = new AnyRaceResult(this);

        copy._finishingOrder = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins <c>scratched</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRaceResult WithScratched(IReadOnlyCollection<HorseNumber> value) {
        AnyRaceResult copy = new AnyRaceResult(this);

        copy._scratched = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="RaceResult" />.</summary>
    public RaceResult Generate() {
        Race race = _race.Generate();

        List<HorseNumber> runners = Enumerable.Range(1, race.FieldSize).Select(number => new HorseNumber(number)).ToList();

        // Drawn from the runners of the race drawn first; a pinned list is taken as given.
        IReadOnlyCollection<HorseNumber> scratched;
        if (!ReferenceEquals(_scratched, _scratchedRecipe)) {
            scratched = _scratched.Generate();
        } else if (!ReferenceEquals(_finishingOrder, _finishingOrderRecipe)) {
            scratched = Array.Empty<HorseNumber>();
        } else {
            scratched = Any.PermutationOf(runners).Generate().Take(Any.Int32().Between(0, race.FieldSize - 1).Generate()).ToList();
        }

        IReadOnlyList<HorseNumber> finishingOrder;
        if (!ReferenceEquals(_finishingOrder, _finishingOrderRecipe)) {
            finishingOrder = _finishingOrder.Generate();
        } else {
            List<HorseNumber> running = runners.Where(horse => !scratched.Contains(horse)).ToList();
            finishingOrder = Any.PermutationOf(running).Generate().Take(Any.Int32().Between(1, running.Count).Generate()).ToList();
        }

        return new RaceResult(race, finishingOrder, scratched);
    }

}
