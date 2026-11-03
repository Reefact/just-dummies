using Drafting.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Drafting.Domain.Tests.Expert.Tests;

/// <summary>The hand-completed generators are code too: they must never draw a value the domain refuses.</summary>
public sealed class GeneratorsTests {

    private const int Draws = 300;

    [Fact]
    public void Part_numbers_and_references_are_always_drawn_valid() {
        for (int draw = 0; draw < Draws; draw++) {
            Any.PartNumber().Generate();
            Any.DrawingReference().Generate();
        }
    }

    [Fact]
    public void Scales_are_always_drawn_with_a_one_on_one_side() {
        for (int draw = 0; draw < Draws; draw++) {
            DrawingScale scale = Any.DrawingScale().Generate();

            Assert.True(scale.Numerator == 1 || scale.Denominator == 1, scale.ToString());
        }
    }

    [Fact]
    public void Dimensions_and_tolerances_are_always_drawn_within_the_tables() {
        for (int draw = 0; draw < Draws; draw++) {
            Dimension dimension = Any.Dimension().Generate();

            Assert.True(dimension.NominalInMillimetres <= StandardTolerance.LargestNominalMillimetres, dimension.ToString());
        }
    }

    [Fact]
    public void Fits_are_always_drawn_with_a_hole_a_shaft_and_one_size() {
        for (int draw = 0; draw < Draws; draw++) {
            Fit fit = Any.Fit().Generate();

            Assert.True(fit.Hole.Tolerance!.IsHole);
            Assert.False(fit.Shaft.Tolerance!.IsHole);
        }
    }

    [Fact]
    public void Items_are_always_drawn_without_their_own_part_among_their_components() {
        for (int draw = 0; draw < Draws; draw++) {
            BomItem item = Any.BomItem().Generate();

            Assert.DoesNotContain(item.Children, child => child.Contains(item.Part));
        }
    }

    [Fact]
    public void Drawings_are_always_drawn_whatever_the_status_asked_for() {
        foreach (DrawingStatus status in Enum.GetValues<DrawingStatus>()) {
            for (int draw = 0; draw < Draws / 4; draw++) {
                Drawing drawing = Any.Drawing().GenerateIn(status);

                Assert.Equal(status, drawing.Status);
            }
        }
    }

}
