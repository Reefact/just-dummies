using JustDummies;

using Xunit;

namespace Drafting.Domain.Tests;

// The generators are hand-completed code, so they get a smoke test of their own: each draw must build a valid domain object.
public sealed class GeneratorTests {

    private const int Draws = 300;

    [Fact]
    public void The_part_number_generator_draws_valid_part_numbers() {
        for (int i = 0; i < Draws; i++) { Assert.NotNull(Any.PartNumber().Generate()); }
    }

    [Fact]
    public void The_drawing_reference_generator_draws_valid_references() {
        for (int i = 0; i < Draws; i++) { Assert.NotNull(Any.DrawingReference().Generate()); }
    }

    [Fact]
    public void The_scale_generator_draws_both_enlargements_and_reductions() {
        List<DrawingScale> scales = [];
        for (int i = 0; i < Draws; i++) { scales.Add(Any.DrawingScale().Generate()); }

        Assert.Contains(scales, s => s.Numerator == 1);
        Assert.Contains(scales, s => s.Denominator == 1);
    }

    [Fact]
    public void The_tolerance_generator_draws_every_zone() {
        List<IsoTolerance> tolerances = [];
        for (int i = 0; i < Draws; i++) { tolerances.Add(Any.IsoTolerance().Generate()); }

        Assert.Equal(["H", "JS", "h", "js"], tolerances.Select(t => t.Zone).Distinct().Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void The_dimension_generator_draws_valid_dimensions_with_and_without_tolerance() {
        List<Dimension> dimensions = [];
        for (int i = 0; i < Draws; i++) { dimensions.Add(Any.Dimension().Generate()); }

        Assert.Contains(dimensions, d => d.Tolerance is null);
        Assert.Contains(dimensions, d => d.Tolerance is not null);
        Assert.Contains(dimensions, d => d.Unit == LengthUnit.Inch);
    }

    [Fact]
    public void The_fit_generator_draws_valid_fits() {
        for (int i = 0; i < Draws; i++) { Assert.NotNull(Any.Fit().Generate()); }
    }

    [Fact]
    public void The_drawing_generator_draws_valid_drafts() {
        for (int i = 0; i < Draws; i++) { Assert.Equal(DrawingStatus.Draft, Any.Drawing().Generate().Status); }
    }

    [Fact]
    public void The_bom_item_generator_draws_valid_items() {
        for (int i = 0; i < Draws; i++) { Assert.NotNull(Any.BomItem().Generate()); }
    }

}
