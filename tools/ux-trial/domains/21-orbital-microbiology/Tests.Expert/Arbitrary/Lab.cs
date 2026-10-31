using JustDummies;

using OrbitalMicrobiology.Domain;

namespace OrbitalMicrobiology.Tests.Expert.Arbitrary;

/// <summary>A culture just inoculated with one species, and the inoculum it was given.</summary>
public sealed record InoculatedCulture(Culture Culture, Species Species, Concentration Inoculum);

/// <summary>Recipes for the scenarios the culture tests keep setting up.</summary>
public static class Lab {

    /// <summary>
    ///     An inoculum a chamber accepts for the species: positive, and a small fraction of the
    ///     carrying capacity, so the population has room to grow. The fraction is arbitrary; its
    ///     ceiling is what makes the inoculum valid, never what a test asserts.
    /// </summary>
    public static IAny<Concentration> AnyInoculumFor(ExperimentalConditions conditions, Species species) {
        double capacity = conditions.CarryingCapacity(species).CellsPerMillilitre;

        return AnyCapacityFraction.As(fraction => new Concentration(capacity * fraction));
    }

    /// <summary>
    ///     A nourished culture at the species' optimum, inoculated with that species at time zero.
    ///     <paramref name="configureCulture" /> refines the culture (its conditions, its id) the way
    ///     every scaffolded <c>With…</c> does. Pass <paramref name="inoculum" /> when the test is about it;
    ///     otherwise it is an arbitrary small fraction of the carrying capacity.
    /// </summary>
    public static IAny<InoculatedCulture> AnyInoculatedCulture(Species                       species,
                                                               Func<AnyCulture, AnyCulture>? configureCulture = null,
                                                               Concentration?                inoculum         = null) {
        AnyCulture cultures = Any.Culture().WithConditions(c => c.Nourished().AtTheOptimumOf(species));

        return Any.Combine(
            (configureCulture ?? (c => c)).Invoke(cultures),
            AnyCapacityFraction,
            (culture, fraction) => {
                Concentration dose = inoculum ?? new Concentration(culture.Conditions.CarryingCapacity(species).CellsPerMillilitre * fraction);
                culture.Inoculate(species, dose);

                return new InoculatedCulture(culture, species, dose);
            });
    }

    private static readonly IAny<double> AnyCapacityFraction = Any.Double().Between(1e-6, 1e-3);

}
