using Drafting.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace Drafting.Tests;

/// <summary>Harness: each scaffolded (and hand-resolved) generator must only ever build valid values.</summary>
[Reproducible]
public sealed class ScaffoldedGeneratorTests {

    private const int Draws = 500;

    private static void AlwaysBuilds<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            T value = generator.Generate();
            Assert.NotNull(value);
        }
    }

    [Fact]
    public void Scaffolded_AnyIsoTolerance_always_builds_a_valid_IsoTolerance() {
        AlwaysBuilds(new AnyIsoTolerance());
    }

    [Fact]
    public void Scaffolded_AnyDimension_always_builds_a_valid_Dimension() {
        AlwaysBuilds(new AnyDimension());
    }

    [Fact]
    public void Scaffolded_AnyFit_always_builds_a_valid_Fit() {
        AlwaysBuilds(new AnyFit());
    }

    [Fact]
    public void Scaffolded_AnyDrawingScale_always_builds_a_valid_DrawingScale() {
        AlwaysBuilds(new AnyDrawingScale());
    }

    [Fact]
    public void Scaffolded_AnyDrawingReference_always_builds_a_valid_DrawingReference() {
        AlwaysBuilds(Any.DrawingReference());
    }

    [Fact]
    public void Scaffolded_AnyPartNumber_always_builds_a_valid_PartNumber() {
        AlwaysBuilds(Any.PartNumber());
    }

    [Fact]
    public void Scaffolded_AnyBomItem_always_builds_a_valid_BomItem() {
        AlwaysBuilds(Any.BomItem());
    }

    [Fact]
    public void Scaffolded_AnyBomItem_with_depth_always_builds_a_valid_BomItem() {
        AlwaysBuilds(Any.BomItem().WithDepth(4));
    }

    [Fact]
    public void Scaffolded_AnyDrawing_always_builds_a_valid_Drawing() {
        AlwaysBuilds(Arbitrary.Drawing());
    }

    /// <summary>
    ///     `any generate ProjectionMethod` (an enum) emitted <c>return new ProjectionMethod();</c>: the
    ///     default value 0, which is not a declared member of this enum. Drawing's constructor rejects it.
    /// </summary>
    [Fact]
    public void Bug_Scaffolded_AnyProjectionMethod_draws_an_undeclared_enum_value() {
        for (int draw = 0; draw < Draws; draw++) {
            ProjectionMethod value = Arbitrary.ProjectionMethod().Generate();
            Assert.True(Enum.IsDefined(value), $"Drew undeclared value {(int)value}.");
        }
    }

}
