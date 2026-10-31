using JustDummies;

namespace ParimutuelBetting.Domain;

public sealed partial class AnyCombination {

    /// <summary>Draws the horses among saddle-cloth numbers <paramref name="lowest" /> to <paramref name="highest" />.</summary>
    public AnyCombination WithHorsesBetween(int lowest, int highest) {
        return new AnyCombination(_betType,
                                  Any.ListOf(new AnyHorseNumber().WithValue(v => v.Between(lowest, highest))).WithCount(3).Distinct());
    }

}
