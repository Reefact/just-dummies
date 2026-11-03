using CatastropheLosses.Domain;

using JustDummies;

using Xunit;

namespace CatastropheLosses.Tests.Discovery;

/// <summary>The scaffolded and completed generators must only ever draw what the domain accepts.</summary>
public sealed class GeneratorTests {

    private const int Draws = 300;

    [Fact]
    public void The_exposure_generator_draws_valid_exposures() {
        // Act
        List<Exposure> exposures = Any.ListOf(Any.Exposure()).WithCount(Draws).Generate();

        // Assert
        Assert.Equal(Draws, exposures.Count);
    }

    [Fact]
    public void The_catastrophe_model_generator_draws_valid_models() {
        // Act
        List<CatastropheModel> models = Any.ListOf(Any.CatastropheModel()).WithCount(Draws).Generate();

        // Assert
        Assert.All(models, model => Assert.NotEmpty(model.Catalogue));
    }

    [Fact]
    public void The_loss_distribution_generator_draws_valid_distributions() {
        // Act
        List<LossDistribution> distributions = Any.ListOf(Any.LossDistribution()).WithCount(Draws).Generate();

        // Assert
        Assert.All(distributions, distribution => Assert.True(distribution.Years >= 1));
    }

    [Fact]
    public void The_portfolio_generator_draws_open_portfolios() {
        // Act
        List<Portfolio> portfolios = Any.ListOf(Any.Portfolio()).WithCount(Draws).Generate();

        // Assert
        Assert.All(portfolios, portfolio => Assert.False(portfolio.IsFrozen));
    }

}
