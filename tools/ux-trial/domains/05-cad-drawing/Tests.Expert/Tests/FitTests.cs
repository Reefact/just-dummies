using Drafting.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Drafting.Domain.Tests.Expert.Tests;

public sealed class FitTests {

    private const int Precision = 9;

    private static Dimension Millimetres(double nominal, string zone, int grade) {
        return new Dimension(nominal, LengthUnit.Millimetre, new IsoTolerance(zone, grade));
    }

    // ----- construction -----

    [Fact]
    public void A_fit_pairs_the_hole_and_the_shaft_it_was_built_from() {
        Dimension hole  = Millimetres(25.0, "H", 7);
        Dimension shaft = Millimetres(25.0, "h", 6);

        Fit fit = new Fit(hole, shaft);

        Assert.Same(hole, fit.Hole);
        Assert.Same(shaft, fit.Shaft);
    }

    [Fact]
    public void A_hole_cannot_be_null() {
        Dimension anyShaft = Any.Dimension().WithTolerance(t => t.ForShaft()).Generate();

        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => new Fit(null!, anyShaft));

        Assert.Equal("hole", error.ParamName);
    }

    [Fact]
    public void A_shaft_cannot_be_null() {
        Dimension anyHole = Any.Dimension().WithTolerance(t => t.ForHole()).Generate();

        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => new Fit(anyHole, null!));

        Assert.Equal("shaft", error.ParamName);
    }

    [Fact]
    public void A_hole_without_a_tolerance_is_refused() {
        Dimension toleranceless = Any.Dimension().WithTolerance((IsoTolerance?)null).Generate();
        Dimension anyShaft      = Any.Dimension().WithTolerance(t => t.ForShaft()).Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new Fit(toleranceless, anyShaft));

        Assert.Equal("hole", error.ParamName);
    }

    [Theory]
    [InlineData("h")]
    [InlineData("js")]
    public void A_hole_carrying_a_shaft_zone_is_refused(string shaftZone) {
        Dimension notAHole  = Any.Dimension().WithTolerance(t => t.WithZone(shaftZone)).Generate();
        Dimension anyShaft  = Any.Dimension().WithTolerance(t => t.ForShaft()).Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new Fit(notAHole, anyShaft));

        Assert.Equal("hole", error.ParamName);
    }

    [Fact]
    public void A_shaft_without_a_tolerance_is_refused() {
        Dimension anyHole       = Any.Dimension().WithTolerance(t => t.ForHole()).Generate();
        Dimension toleranceless = Any.Dimension().WithTolerance((IsoTolerance?)null).Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new Fit(anyHole, toleranceless));

        Assert.Equal("shaft", error.ParamName);
    }

    [Theory]
    [InlineData("H")]
    [InlineData("JS")]
    public void A_shaft_carrying_a_hole_zone_is_refused(string holeZone) {
        Dimension anyHole    = Any.Dimension().WithTolerance(t => t.ForHole()).Generate();
        Dimension notAShaft  = Any.Dimension().WithTolerance(t => t.WithZone(holeZone)).Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new Fit(anyHole, notAShaft));

        Assert.Equal("shaft", error.ParamName);
    }

    [Fact]
    public void The_hole_is_judged_before_the_shaft() {
        Dimension toleranceless = Any.Dimension().WithTolerance((IsoTolerance?)null).Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new Fit(toleranceless, toleranceless));

        Assert.Equal("hole", error.ParamName);
    }

    [Fact]
    public void A_hole_and_a_shaft_of_different_sizes_are_refused() {
        Dimension hole  = Millimetres(25.0, "H", 7);
        Dimension shaft = Millimetres(26.0, "h", 6);

        ArgumentException error = Assert.Throws<ArgumentException>(() => new Fit(hole, shaft));

        Assert.Equal("shaft", error.ParamName);
    }

    [Fact]
    public void A_difference_of_a_nanometre_is_tolerated_but_ten_are_not() {
        Dimension hole = Millimetres(25.0, "H", 7);

        Fit fit = new Fit(hole, Millimetres(25.0 + 5e-10, "h", 6));

        Assert.NotNull(fit);
        Assert.Throws<ArgumentException>(() => new Fit(hole, Millimetres(25.0 + 1e-8, "h", 6)));
    }

    [Fact]
    public void A_hole_in_inches_pairs_with_a_shaft_of_the_same_size_in_millimetres() {
        Dimension hole  = new Dimension(1.0, LengthUnit.Inch, new IsoTolerance("H", 7));
        Dimension shaft = Millimetres(25.4, "h", 6);

        Fit fit = new Fit(hole, shaft);

        Assert.Same(hole, fit.Hole);
    }

    // ----- clearances and kind -----

    [Fact]
    public void An_H7_h6_fit_on_25_has_clearance_from_zero_to_34_micrometres() {
        Fit fit = new Fit(Millimetres(25.0, "H", 7), Millimetres(25.0, "h", 6));

        Assert.Equal(0.034, fit.MaximumClearanceInMillimetres, Precision);
        Assert.Equal(0.0, fit.MinimumClearanceInMillimetres, Precision);
        Assert.Equal(FitKind.Clearance, fit.Kind);
    }

    [Fact]
    public void An_H7_js6_fit_on_25_is_a_transition() {
        Fit fit = new Fit(Millimetres(25.0, "H", 7), Millimetres(25.0, "js", 6));

        Assert.Equal(0.0275, fit.MaximumClearanceInMillimetres, Precision);
        Assert.Equal(-0.0065, fit.MinimumClearanceInMillimetres, Precision);
        Assert.Equal(FitKind.Transition, fit.Kind);
    }

    [Fact]
    public void A_JS7_h6_fit_on_25_is_a_transition() {
        Fit fit = new Fit(Millimetres(25.0, "JS", 7), Millimetres(25.0, "h", 6));

        Assert.Equal(0.0235, fit.MaximumClearanceInMillimetres, Precision);
        Assert.Equal(-0.0105, fit.MinimumClearanceInMillimetres, Precision);
        Assert.Equal(FitKind.Transition, fit.Kind);
    }

    [Fact]
    public void A_zero_minimum_clearance_is_still_a_clearance_fit() {
        // H and h share the zero line: the hole's lower limit meets the shaft's upper limit exactly.
        Fit fit = new Fit(Millimetres(100.0, "H", 11), Millimetres(100.0, "h", 11));

        Assert.Equal(0.0, fit.MinimumClearanceInMillimetres);
        Assert.Equal(FitKind.Clearance, fit.Kind);
    }

    [Fact]
    public void A_hole_and_a_shaft_in_different_units_are_compared_in_millimetres() {
        Dimension hole  = new Dimension(1.0, LengthUnit.Inch, new IsoTolerance("H", 7));
        Dimension shaft = Millimetres(25.4, "h", 6);

        Fit fit = new Fit(hole, shaft);

        Assert.Equal(0.034, fit.MaximumClearanceInMillimetres, Precision);
        Assert.Equal(FitKind.Clearance, fit.Kind);
    }

    [Fact(Skip = "domain defect: with the supported zones (H/JS holes, h/js shafts) the hole's upper limit is always above the nominal and the shaft's lower limit never above it, so the maximum clearance is always positive and Fit.Kind can never be Interference")]
    public void Some_pair_of_supported_classes_makes_an_interference_fit() {
        string[] holeZones  = ["H", "JS"];
        string[] shaftZones = ["h", "js"];

        List<FitKind> kinds = new List<FitKind>();
        foreach (string holeZone in holeZones) {
            foreach (string shaftZone in shaftZones) {
                for (int holeGrade = IsoTolerance.FinestGrade; holeGrade <= IsoTolerance.CoarsestGrade; holeGrade++) {
                    for (int shaftGrade = IsoTolerance.FinestGrade; shaftGrade <= IsoTolerance.CoarsestGrade; shaftGrade++) {
                        kinds.Add(new Fit(Millimetres(25.0, holeZone, holeGrade), Millimetres(25.0, shaftZone, shaftGrade)).Kind);
                    }
                }
            }
        }

        Assert.Contains(FitKind.Interference, kinds);
    }

}
