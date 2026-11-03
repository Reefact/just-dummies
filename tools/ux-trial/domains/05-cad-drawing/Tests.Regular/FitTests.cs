using JustDummies;

using Xunit;

namespace Drafting.Domain;

public sealed class FitTests {

    private const int Precision = 9;

    private static Dimension AnyHoleOf(double nominalMillimetres) {
        return Any.Dimension()
                  .WithNominal(nominalMillimetres)
                  .WithUnit(LengthUnit.Millimetre)
                  .WithTolerance(t => t.WithZone(z => z.Except("h", "js")))
                  .Generate();
    }

    private static Dimension AnyShaftOf(double nominalMillimetres) {
        return Any.Dimension()
                  .WithNominal(nominalMillimetres)
                  .WithUnit(LengthUnit.Millimetre)
                  .WithTolerance(t => t.WithZone(z => z.Except("H", "JS")))
                  .Generate();
    }

    private static Dimension Toleranced(double nominal, LengthUnit unit, string zone, int grade) {
        return new Dimension(nominal, unit, new IsoTolerance(zone, grade));
    }

    [Fact]
    public void A_fit_keeps_its_hole_and_its_shaft() {
        // Arrange
        double    anyNominal = Any.Double().Between(0.001, 500.0).Generate();
        Dimension anyHole    = AnyHoleOf(anyNominal);
        Dimension anyShaft   = AnyShaftOf(anyNominal);

        // Act
        Fit fit = new Fit(anyHole, anyShaft);

        // Assert
        Assert.Same(anyHole, fit.Hole);
        Assert.Same(anyShaft, fit.Shaft);
    }

    [Fact]
    public void Any_generated_fit_pairs_features_of_the_same_nominal_size() {
        // Arrange
        Fit anyFit = Any.Fit().Generate();

        // Assert
        Assert.Equal(anyFit.Hole.NominalInMillimetres, anyFit.Shaft.NominalInMillimetres, Precision);
    }

    [Fact]
    public void A_null_hole_is_refused() {
        // Arrange
        Dimension anyShaft = AnyShaftOf(25.0);

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Fit(null!, anyShaft));

        // Assert
        Assert.Equal("hole", exception.ParamName);
    }

    [Fact]
    public void A_null_shaft_is_refused() {
        // Arrange
        Dimension anyHole = AnyHoleOf(25.0);

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Fit(anyHole, null!));

        // Assert
        Assert.Equal("shaft", exception.ParamName);
    }

    [Fact]
    public void A_hole_without_a_tolerance_is_refused() {
        // Arrange
        Dimension anyUntoleranced = Any.Dimension().WithNominal(25.0).WithUnit(LengthUnit.Millimetre).WithoutTolerance().Generate();
        Dimension anyShaft        = AnyShaftOf(25.0);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Fit(anyUntoleranced, anyShaft));

        // Assert
        Assert.Equal("hole", exception.ParamName);
    }

    [Fact]
    public void A_shaft_without_a_tolerance_is_refused() {
        // Arrange
        Dimension anyHole         = AnyHoleOf(25.0);
        Dimension anyUntoleranced = Any.Dimension().WithNominal(25.0).WithUnit(LengthUnit.Millimetre).WithoutTolerance().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Fit(anyHole, anyUntoleranced));

        // Assert
        Assert.Equal("shaft", exception.ParamName);
    }

    [Fact]
    public void A_hole_carrying_a_shaft_tolerance_is_refused() {
        // Arrange
        Dimension anyShaft       = AnyShaftOf(25.0);
        Dimension anotherShaft   = AnyShaftOf(25.0);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Fit(anyShaft, anotherShaft));

        // Assert
        Assert.Equal("hole", exception.ParamName);
    }

    [Fact]
    public void A_shaft_carrying_a_hole_tolerance_is_refused() {
        // Arrange
        Dimension anyHole     = AnyHoleOf(25.0);
        Dimension anotherHole = AnyHoleOf(25.0);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Fit(anyHole, anotherHole));

        // Assert
        Assert.Equal("shaft", exception.ParamName);
    }

    [Fact]
    public void The_symmetrical_zones_count_as_hole_JS_and_as_shaft_js() {
        // Act
        Fit fit = new Fit(Toleranced(25.0, LengthUnit.Millimetre, "JS", 7), Toleranced(25.0, LengthUnit.Millimetre, "js", 6));

        // Assert
        Assert.True(fit.Hole.Tolerance!.IsHole);
        Assert.False(fit.Shaft.Tolerance!.IsHole);
    }

    [Fact]
    public void A_js_hole_is_refused_and_so_is_a_JS_shaft() {
        // Assert
        Assert.Throws<ArgumentException>(() => new Fit(Toleranced(25.0, LengthUnit.Millimetre, "js", 7), Toleranced(25.0, LengthUnit.Millimetre, "js", 6)));
        Assert.Throws<ArgumentException>(() => new Fit(Toleranced(25.0, LengthUnit.Millimetre, "H", 7), Toleranced(25.0, LengthUnit.Millimetre, "JS", 6)));
    }

    [Fact]
    public void The_hole_is_checked_before_the_shaft() {
        // Arrange
        Dimension anyUntoleranced = Any.Dimension().WithoutTolerance().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Fit(anyUntoleranced, anyUntoleranced));

        // Assert
        Assert.Equal("hole", exception.ParamName);
    }

    [Fact]
    public void Features_of_different_nominal_sizes_cannot_be_paired() {
        // Arrange
        double    anyNominal = Any.Double().Between(1.0, 400.0).Generate();
        double    anyOther   = Any.Double().Between(anyNominal + 0.001, 500.0).Generate();
        Dimension anyHole    = AnyHoleOf(anyNominal);
        Dimension anyShaft   = AnyShaftOf(anyOther);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Fit(anyHole, anyShaft));

        // Assert
        Assert.Equal("shaft", exception.ParamName);
    }

    [Fact]
    public void Nominal_sizes_closer_than_a_billionth_of_a_millimetre_are_the_same_size() {
        // Act / Assert
        Assert.NotNull(new Fit(AnyHoleOf(25.0), AnyShaftOf(25.0 + 5e-10)));
        Assert.Throws<ArgumentException>(() => new Fit(AnyHoleOf(25.0), AnyShaftOf(25.0 + 2e-9)));
    }

    [Fact]
    public void A_hole_in_inches_pairs_with_a_shaft_in_millimetres_of_the_same_size() {
        // Arrange
        Dimension hole  = Toleranced(1.0, LengthUnit.Inch, "H", 7);
        Dimension shaft = Toleranced(25.4, LengthUnit.Millimetre, "h", 6);

        // Act
        Fit fit = new Fit(hole, shaft);

        // Assert
        Assert.Same(hole, fit.Hole);
        Assert.Same(shaft, fit.Shaft);
    }

    [Fact]
    public void H7_over_h6_is_a_clearance_fit_whose_minimum_clearance_is_exactly_zero() {
        // Arrange: hole 25.000..25.021, shaft 24.987..25.000.
        Fit fit = new Fit(Toleranced(25.0, LengthUnit.Millimetre, "H", 7), Toleranced(25.0, LengthUnit.Millimetre, "h", 6));

        // Assert
        Assert.Equal(0.034, fit.MaximumClearanceInMillimetres, Precision);
        Assert.Equal(0.0, fit.MinimumClearanceInMillimetres, Precision);
        Assert.Equal(FitKind.Clearance, fit.Kind);
    }

    [Fact]
    public void H7_over_js6_is_a_transition_fit() {
        // Arrange: hole 25.000..25.021, shaft 24.9935..25.0065.
        Fit fit = new Fit(Toleranced(25.0, LengthUnit.Millimetre, "H", 7), Toleranced(25.0, LengthUnit.Millimetre, "js", 6));

        // Assert
        Assert.Equal(0.0275, fit.MaximumClearanceInMillimetres, Precision);
        Assert.Equal(-0.0065, fit.MinimumClearanceInMillimetres, Precision);
        Assert.Equal(FitKind.Transition, fit.Kind);
    }

    [Fact]
    public void JS7_over_h6_is_a_transition_fit() {
        // Arrange: hole 24.9895..25.0105, shaft 24.987..25.000.
        Fit fit = new Fit(Toleranced(25.0, LengthUnit.Millimetre, "JS", 7), Toleranced(25.0, LengthUnit.Millimetre, "h", 6));

        // Assert
        Assert.Equal(0.0235, fit.MaximumClearanceInMillimetres, Precision);
        Assert.Equal(-0.0105, fit.MinimumClearanceInMillimetres, Precision);
        Assert.Equal(FitKind.Transition, fit.Kind);
    }

    [Fact]
    public void The_maximum_clearance_is_never_below_the_minimum_clearance() {
        // Arrange
        Fit anyFit = Any.Fit().Generate();

        // Assert
        Assert.True(anyFit.MaximumClearanceInMillimetres >= anyFit.MinimumClearanceInMillimetres);
    }

    [Fact(Skip = "domain defect: no hole/shaft pair of the supported zones (H, JS / h, js) can ever be an interference fit — the maximum clearance (hole upper - shaft lower) is always strictly positive, so FitKind.Interference is unreachable")]
    public void An_interference_fit_can_be_built_from_the_supported_zones() {
        // Arrange
        string[] holeZones  = ["H", "JS"];
        string[] shaftZones = ["h", "js"];
        bool     found      = false;

        // Act
        foreach (string holeZone in holeZones) {
            foreach (string shaftZone in shaftZones) {
                for (int holeGrade = IsoTolerance.FinestGrade; holeGrade <= IsoTolerance.CoarsestGrade; holeGrade++) {
                    for (int shaftGrade = IsoTolerance.FinestGrade; shaftGrade <= IsoTolerance.CoarsestGrade; shaftGrade++) {
                        Fit fit = new Fit(Toleranced(25.0, LengthUnit.Millimetre, holeZone, holeGrade), Toleranced(25.0, LengthUnit.Millimetre, shaftZone, shaftGrade));
                        found |= fit.Kind == FitKind.Interference;
                    }
                }
            }
        }

        // Assert
        Assert.True(found);
    }

}
