using CatastropheLosses.Tests.Expert.Recipes;

using JustDummies;

namespace CatastropheLosses.Domain;

// Written by hand beside the scaffolded AnyPortfolio.cs: a portfolio is an aggregate filled after
// construction, which no constructor parameter describes.
public sealed partial class AnyPortfolio {

    /// <summary>An open portfolio holding the exposures <paramref name="exposures" /> draws.</summary>
    public IAny<Portfolio> Holding(IAny<IEnumerable<Exposure>> exposures) {
        return Any.Combine(this, exposures, (portfolio, drawn) => {
            foreach (Exposure exposure in drawn) {
                portfolio.Add(exposure);
            }

            return portfolio;
        });
    }

    /// <summary>A frozen portfolio, ready to be simulated, holding the exposures <paramref name="exposures" /> draws.</summary>
    public IAny<Portfolio> FrozenWith(IAny<IEnumerable<Exposure>> exposures) {
        return Holding(exposures).As(portfolio => {
            portfolio.Freeze();
            return portfolio;
        });
    }

    /// <summary>A frozen portfolio holding exactly these exposures; only its name and currency are drawn.</summary>
    public IAny<Portfolio> FrozenWith(params Exposure[] exposures) {
        return FrozenWith(Any.OneOf<IEnumerable<Exposure>>(exposures));
    }

    /// <summary>A frozen portfolio of arbitrary, distinct exposures.</summary>
    public IAny<Portfolio> Frozen() {
        return FrozenWith(Exposures.Distinct());
    }

}

public static class Exposures {

    /// <summary>Several exposures, each on its own property: a portfolio refuses a property twice.</summary>
    public static AnyList<Exposure> Distinct() {
        return Any.ListOf(Any.Exposure()).WithCountBetween(1, 12).Distinct(PropertyIdentity.Comparer);
    }

}
