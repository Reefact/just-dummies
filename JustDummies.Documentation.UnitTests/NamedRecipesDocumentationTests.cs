using System.Linq;

using JustDummies.Documentation.UnitTests.Fixtures;

using NFluent;

namespace JustDummies.Documentation.UnitTests;

/// <summary>
///     The named recipes the scaffolder and strings pages teach, run rather than only compiled.
/// </summary>
/// <remarks>
///     A sample that compiles proves its names resolve, not that it draws what its page says. The recipes
///     are written on the pages with nothing but the public surface of the generator <c>any</c> scaffolded, so the
///     fixtures stand in for it and each recipe is restated here exactly as the page writes it.
/// </remarks>
public sealed class NamedRecipesDocumentationTests {

    [Fact(DisplayName = "Three horses of a race of twelve are three distinct numbers from one to twelve.")]
    public void ThreeHorsesOfARaceOfTwelveAreThreeDistinctNumbers() {
        AnyCombination combinations = new AnyCombination()
                                     .WithHorses(_ => Any.ListOf(new AnyHorseNumber().WithValue(number => number.Between(1, 12))).NonEmpty())
                                     .WithHorses(l => l.WithCount(3).Distinct());

        Combination[] drawn = [.. Enumerable.Range(0, 200).Select(_ => combinations.Generate())];

        Check.That(drawn).ContainsOnlyElementsThatMatch(combination => combination.Horses.Count == 3
                                                                    && combination.Horses.Select(horse => horse.Value).Distinct().Count() == 3
                                                                    && combination.Horses.All(horse => horse.Value is >= 1 and <= 12));
    }

}
