using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Regular;

/// <summary>The scaffolded and completed generators hand out objects the domain accepts.</summary>
public sealed class GeneratorSmokeTests {

    private const int Draws = 300;

    [Fact]
    public void Every_generator_draws_valid_domain_objects() {
        for (int i = 0; i < Draws; i++) {
            Assert.NotNull(Any.GeoPoint().Generate());
            Assert.NotNull(Any.Footprint().Generate());
            Assert.NotNull(Any.VulnerabilityCurve().Generate());
            Assert.NotNull(Any.InsuredProperty().Generate());
            Assert.NotNull(Any.PolicyTerms().Generate());
            Assert.NotNull(Any.Exposure().Generate());
            Assert.NotNull(Any.CatastropheEvent().Generate());
            Assert.NotNull(Any.Portfolio().Generate());
            Assert.NotNull(Any.LossDistribution().Generate());
            Assert.NotNull(Any.CatastropheModel().Generate());
        }
    }

}
