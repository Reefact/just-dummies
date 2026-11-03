using JustDummies;

namespace ParimutuelBetting.Domain.Tests.Expert.Generators;

// Written by hand, beside the scaffold: `any generate Combination --force` leaves this file alone.
public sealed partial class AnyCombination {

    /// <summary>A combination that names only runners of a race of <paramref name="fieldSize" /> horses.</summary>
    public AnyCombination AmongRunnersOf(int fieldSize) {
        AnyCombination copy = new AnyCombination(this);

        // The race owns "1 to fieldSize"; the count (the bet type's arity) is still set by Generate().
        copy._horsesRecipe = Any.ListOf(new AnyHorseNumber().WithValue(number => number.LessThanOrEqualTo(fieldSize))).Distinct();

        return copy;
    }

}
