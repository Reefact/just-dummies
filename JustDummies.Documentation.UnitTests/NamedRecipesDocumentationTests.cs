using System.Linq;

using JustDummies.Documentation.UnitTests.Fixtures;

using NFluent;

namespace JustDummies.Documentation.UnitTests;

/// <summary>
///     The two named recipes the scaffolder and strings pages teach, run rather than only compiled.
/// </summary>
/// <remarks>
///     A sample that compiles proves its names resolve, not that it draws what its page says. Both recipes below
///     are written on the pages with nothing but the public surface of the generator <c>any</c> scaffolded, so the
///     fixtures stand in for it and the recipe is restated here exactly as the page writes it.
/// </remarks>
public sealed class NamedRecipesDocumentationTests {

    [Fact(DisplayName = "A French animal id keeps the shape of the base and fixes the country.")]
    public void AFrenchAnimalIdKeepsTheShapeAndFixesTheCountry() {
        AnyAnimalId french = new AnyAnimalId().WithValue(_ => Any.StringMatching("FR[0-9]{12}"));

        AnimalId[] drawn = [.. Enumerable.Range(0, 200).Select(_ => french.Generate())];

        Check.That(drawn.Select(animal => animal.Value)).ContainsOnlyElementsThatMatch(value => value.StartsWith("FR") && value.Length == 14);
    }

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
