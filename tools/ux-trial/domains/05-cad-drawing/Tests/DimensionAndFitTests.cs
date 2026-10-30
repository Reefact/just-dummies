using Drafting.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace Drafting.Tests;

[Reproducible]
public sealed class DimensionAndFitTests {

    private const double Micrometre = 0.001;

    [Fact]
    public void A_25_mm_H7_hole_spans_21_micrometres_above_its_nominal() {
        // Arrange
        Dimension hole = new(25, LengthUnit.Millimetre, new IsoTolerance("H", 7));

        // Act
        double lower = hole.LowerLimitInMillimetres;
        double upper = hole.UpperLimitInMillimetres;

        // Assert
        Assert.Equal(25.000, lower, Micrometre / 10);
        Assert.Equal(25.021, upper, Micrometre / 10);
    }

    [Fact]
    public void A_one_inch_h6_shaft_reads_its_tolerance_from_the_18_to_30_mm_row() {
        // Arrange
        Dimension shaft = new(1, LengthUnit.Inch, new IsoTolerance("h", 6));

        // Act
        double lower = shaft.LowerLimitInMillimetres;

        // Assert: 1 in = 25.4 mm, IT6 for 18-30 mm is 13 µm
        Assert.Equal(25.387, lower, Micrometre / 10);
    }

    [Fact]
    public void A_js_shaft_is_centred_on_its_nominal() {
        // Arrange
        Dimension shaft = new(40, LengthUnit.Millimetre, new IsoTolerance("js", 9));

        // Act
        double upper = shaft.UpperLimitInMillimetres;
        double lower = shaft.LowerLimitInMillimetres;

        // Assert: IT9 for 30-50 mm is 62 µm, split either side
        Assert.Equal(40.031, upper, Micrometre / 10);
        Assert.Equal(39.969, lower, Micrometre / 10);
    }

    [Fact]
    public void A_measurement_one_micrometre_over_the_upper_limit_is_rejected() {
        // Arrange
        Dimension hole = new(25, LengthUnit.Millimetre, new IsoTolerance("H", 7));

        // Act
        bool accepted = hole.Accepts(25.022);

        // Assert
        Assert.False(accepted);
    }

    [Fact]
    public void An_H_hole_on_an_h_shaft_of_the_same_size_is_a_clearance_fit() {
        // Arrange: size, unit and grades are arbitrary; the zones are what the rule is about.
        Fit fit = new AnyFit()
                  .WithHole(hole => hole.WithTolerance(t => t.WithZone("H")))
                  .WithShaft(shaft => shaft.WithTolerance(t => t.WithZone("h")))
                  .Generate();

        // Act
        FitKind kind = fit.Kind;

        // Assert
        Assert.Equal(FitKind.Clearance, kind);
    }

    [Fact]
    public void Ø25_H7_js6_is_a_transition_fit() {
        // Arrange
        Fit fit = new(new Dimension(25, LengthUnit.Millimetre, new IsoTolerance("H", 7)),
                      new Dimension(25, LengthUnit.Millimetre, new IsoTolerance("js", 6)));

        // Act
        double minimumClearance = fit.MinimumClearanceInMillimetres;
        FitKind kind            = fit.Kind;

        // Assert: H7 0/+21, js6 ±6.5 µm
        Assert.Equal(-0.0065, minimumClearance, Micrometre / 10);
        Assert.Equal(FitKind.Transition, kind);
    }

    [Fact]
    public void A_fit_refuses_a_shaft_of_another_size() {
        // Arrange: whatever the hole, a shaft 1 mm bigger is not its mate.
        Dimension anyHole = new AnyDimension()
                            .WithUnit(LengthUnit.Millimetre)
                            .WithNominal(n => n.LessThanOrEqualTo(400))
                            .WithTolerance(t => t.WithZone("H"))
                            .Generate();
        Dimension shaft = new(anyHole.Nominal + 1, LengthUnit.Millimetre, new IsoTolerance("h", 6));

        // Act
        Exception? refusal = Record.Exception(() => new Fit(anyHole, shaft));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_1_to_5_scale_draws_100_mm_as_20_mm_of_paper() {
        // Arrange
        DrawingScale scale = new(1, 5);

        // Act
        double paper = scale.PaperLengthOf(100);

        // Assert
        Assert.Equal(20, paper, 1e-9);
    }

}
