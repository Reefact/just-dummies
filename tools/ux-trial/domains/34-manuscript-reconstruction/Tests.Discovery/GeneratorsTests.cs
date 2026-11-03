using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Discovery;

/// <summary>
///     The generators are test code too, and two of them carry hand-written rules. Drawing many of each
///     shows they never produce a value the real constructors refuse — which would make every other test flaky.
/// </summary>
public sealed class GeneratorsTests {

    private const int Draws = 500;

    [Fact]
    public void Every_generator_draws_only_values_its_constructor_accepts() {
        // Act: a constructor refusal would surface here as an exception
        Action drawAll = () => {
            Any.ListOf(Any.WitnessSiglum()).WithCount(Draws).Generate();
            Any.ListOf(Any.PassageReference()).WithCount(Draws).Generate();
            Any.ListOf(Any.Variant()).WithCount(Draws).Generate();
            Any.ListOf(Any.Lacuna()).WithCount(Draws).Generate();
            Any.ListOf(Any.CopyRelation()).WithCount(Draws).Generate();
            Any.ListOf(Any.AttestedReading()).WithCount(Draws).Generate();
            Any.ListOf(Any.Witness()).WithCount(Draws).Generate();
            Any.ListOf(Any.EditorialHypothesis()).WithCount(Draws).Generate();
            Any.ListOf(Any.Tradition()).WithCount(Draws).Generate();
        };

        // Assert
        Exception? failure = Record.Exception(drawAll);
        Assert.Null(failure);
    }

    [Fact]
    public void Drawn_variants_reach_every_kind() {
        // Arrange
        List<Variant> anyVariants = Any.ListOf(Any.Variant()).WithCount(Draws).Generate();

        // Assert: the draw is not stuck on one kind, so the kind-dependent rules are really exercised
        Assert.Equal(Enum.GetValues<VariantKind>().Length, anyVariants.Select(v => v.Kind).Distinct().Count());
    }

}
