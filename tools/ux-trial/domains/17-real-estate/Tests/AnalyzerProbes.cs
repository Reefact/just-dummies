using JustDummies;
using RealEstate.Domain;
using Xunit;

namespace RealEstate.Tests;

/// <summary>
/// Code an analyzer should catch, kept out of the test run: none of these methods is a test.
/// Each comment names the rule expected; NOTES.md records which fired.
/// </summary>
internal static class AnalyzerProbes {

    // Expected: a generated value flowing into an assertion (a property with a sample of one).
    public static void DrawnValueInAssertion() {
        Surface anySurface = Arbitrary.Surface().Generate();
        Assert.Equal(anySurface.SquareMeters / 10_000d, anySurface.Hectares);
    }

    // Expected JD023: an integer chain narrowed to nothing.
    public static int CrossedEnergyBounds() {
        return Any.Int32().Between(420, 330).Generate();
    }

    // Expected: crossed constant double bounds (JD023 is documented for integers only).
    public static double CrossedSurfaceBounds() {
        return Any.Double().GreaterThan(100d).LessThan(10d).Generate();
    }

    // Expected JD014: a transposed Between on a double.
    public static double TransposedSurfaceRange() {
        return Any.Double().Between(100d, 10d).Generate();
    }

    // Expected JD015: a length that cannot hold the prefix.
    public static string ReferenceTooShortForItsCommune() {
        return Any.String().WithLength(3).StartingWith("2A004").Generate();
    }

    // Expected JD017: every declared member excluded.
    public static EnergyRating NoRatingLeft() {
        return Any.Enum<EnergyRating>().Except(EnergyRating.A, EnergyRating.B, EnergyRating.C, EnergyRating.D, EnergyRating.E, EnergyRating.F, EnergyRating.G).Generate();
    }

    // Expected JD016: eight distinct values of a seven-member enum.
    public static HashSet<EnergyRating> TooManyRatings() {
        return Any.SetOf(Any.Enum<EnergyRating>()).WithCount(8).Generate();
    }

    // Expected JD013: a pool of one list.
    public static List<string> PoolOfOneList() {
        List<string> communes = ["75056", "2A004", "97411"];
        return Any.OneOf(communes).Generate();
    }

    // Expected JD028: Distinct over a reference type without value equality.
    public static List<DocumentLink> DistinctWithoutEquality() {
        return Any.ListOf(Arbitrary.DocumentLink()).WithCount(5).Distinct().Generate();
    }

    // Expected: a constant pattern outside the regular subset (refused at run time).
    public static string LookaheadPattern() {
        return Any.StringMatching(@"\d{5} (?!000)\d{3}").Generate();
    }

    // Expected JD026: the empty relative reference.
    public static Uri EmptyRelativeDocument() {
        return Any.Uri().Relative().WithPathSegments(0).Generate();
    }

    // Expected JD030: a string chain that declares no length.
    public static string UnboundedSection() {
        return Any.String().Alpha().InUpperCase().Generate();
    }

    // Expected: decimal scale out of range (0..28).
    public static decimal ScaleTooLarge() {
        return Any.Decimal().WithScale(29).Generate();
    }

    // Expected JD025: a duplicated constant in a pool.
    public static EnergyRating DuplicatedPool() {
        return Any.Enum<EnergyRating>().OneOf(EnergyRating.A, EnergyRating.A, EnergyRating.B).Generate();
    }
}
