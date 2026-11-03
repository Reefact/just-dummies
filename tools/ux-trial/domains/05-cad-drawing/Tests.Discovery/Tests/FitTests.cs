using JustDummies;

using Xunit;

namespace Drafting.Domain.Tests;

public sealed class FitTests {

    private static Dimension Hole(double nominal, string zone, int grade, LengthUnit unit = LengthUnit.Millimetre) {
        return new Dimension(nominal, unit, new IsoTolerance(zone, grade));
    }

    private static Dimension Shaft(double nominal, string zone, int grade, LengthUnit unit = LengthUnit.Millimetre) {
        return new Dimension(nominal, unit, new IsoTolerance(zone, grade));
    }

    [Fact]
    public void A_null_hole_is_refused() {
        // Arrange
        Dimension anyShaft = Any.Dimension().WithTolerance(t => t.Shaft()).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Fit(null!, anyShaft));
    }

    [Fact]
    public void A_null_shaft_is_refused() {
        // Arrange
        Dimension anyHole = Any.Dimension().WithTolerance(t => t.Hole()).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Fit(anyHole, null!));
    }

    [Fact]
    public void A_hole_without_a_tolerance_is_refused() {
        // Arrange
        Dimension holeWithoutTolerance = Any.Dimension().WithoutTolerance().Generate();
        Dimension anyShaft             = Any.Dimension().WithTolerance(t => t.Shaft()).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Fit(holeWithoutTolerance, anyShaft));

        // Assert
        Assert.Equal("hole", error.ParamName);
    }

    [Fact]
    public void A_hole_with_a_shaft_zone_is_refused() {
        // Arrange
        Dimension holeWithAShaftZone = Any.Dimension().WithTolerance(t => t.Shaft()).Generate();
        Dimension anyShaft           = Any.Dimension().WithTolerance(t => t.Shaft()).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Fit(holeWithAShaftZone, anyShaft));

        // Assert
        Assert.Equal("hole", error.ParamName);
    }

    [Fact]
    public void A_shaft_without_a_tolerance_is_refused() {
        // Arrange
        Dimension anyHole               = Any.Dimension().WithTolerance(t => t.Hole()).Generate();
        Dimension shaftWithoutTolerance = Any.Dimension().WithoutTolerance().Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Fit(anyHole, shaftWithoutTolerance));

        // Assert
        Assert.Equal("shaft", error.ParamName);
    }

    [Fact]
    public void A_shaft_with_a_hole_zone_is_refused() {
        // Arrange
        Dimension anyHole            = Any.Dimension().WithTolerance(t => t.Hole()).Generate();
        Dimension shaftWithAHoleZone = Any.Dimension().WithTolerance(t => t.Hole()).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Fit(anyHole, shaftWithAHoleZone));

        // Assert
        Assert.Equal("shaft", error.ParamName);
    }

    [Theory]
    [InlineData("JS", "js")]
    [InlineData("H", "js")]
    [InlineData("JS", "h")]
    [InlineData("H", "h")]
    public void Each_hole_zone_pairs_with_each_shaft_zone(string holeZone, string shaftZone) {
        // Act
        Fit fit = new Fit(Hole(25, holeZone, 7), Shaft(25, shaftZone, 6));

        // Assert
        Assert.True(fit.Hole.Tolerance!.IsHole);
        Assert.False(fit.Shaft.Tolerance!.IsHole);
    }

    [Fact]
    public void The_hole_is_checked_before_the_shaft_and_both_before_the_sizes() {
        // Arrange: a hole with a shaft zone AND a different size
        Dimension badHole = Shaft(30, "h", 6);

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Fit(badHole, Shaft(25, "h", 6)));

        // Assert
        Assert.Equal("hole", error.ParamName);
    }

    [Fact]
    public void Two_features_of_different_nominal_sizes_do_not_make_a_fit() {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Fit(Hole(25, "H", 7), Shaft(26, "h", 6)));

        // Assert
        Assert.Equal("shaft", error.ParamName);
    }

    [Fact]
    public void A_hole_in_inches_pairs_with_a_shaft_in_millimetres_of_the_same_size() {
        // Act
        Fit fit = new Fit(Hole(1.0, "H", 7, LengthUnit.Inch), Shaft(25.4, "h", 6));

        // Assert
        Assert.Equal(25.4, fit.Hole.NominalInMillimetres, 9);
        Assert.Equal(25.4, fit.Shaft.NominalInMillimetres, 9);
    }

    [Fact]
    public void Sizes_closer_than_a_nanometre_are_the_same_size() {
        Assert.NotNull(new Fit(Hole(25.0, "H", 7), Shaft(25.0 + 5e-10, "h", 6)));
    }

    [Fact]
    public void Sizes_a_hundredth_of_a_micrometre_apart_are_different_sizes() {
        Assert.Throws<ArgumentException>(() => new Fit(Hole(25.0, "H", 7), Shaft(25.0 + 1e-8, "h", 6)));
    }

    [Fact]
    public void H7_over_h6_is_a_clearance_fit_with_no_play_at_the_tightest() {
        // Arrange: the classic sliding fit, Ø25 H7/h6
        Fit fit = new Fit(Hole(25, "H", 7), Shaft(25, "h", 6));

        // Assert
        Assert.Equal(FitKind.Clearance, fit.Kind);
        Assert.Equal(0.0, fit.MinimumClearanceInMillimetres, 9);
        Assert.Equal(0.034, fit.MaximumClearanceInMillimetres, 9);
    }

    [Fact]
    public void H7_over_js6_is_a_transition_fit() {
        // Arrange
        Fit fit = new Fit(Hole(25, "H", 7), Shaft(25, "js", 6));

        // Assert
        Assert.Equal(FitKind.Transition, fit.Kind);
        Assert.Equal(-0.0065, fit.MinimumClearanceInMillimetres, 9);
        Assert.Equal(0.0275, fit.MaximumClearanceInMillimetres, 9);
    }

    [Fact]
    public void JS7_over_h6_is_a_transition_fit() {
        // Arrange
        Fit fit = new Fit(Hole(25, "JS", 7), Shaft(25, "h", 6));

        // Assert
        Assert.Equal(FitKind.Transition, fit.Kind);
        Assert.Equal(-0.0105, fit.MinimumClearanceInMillimetres, 9);
        Assert.Equal(0.0235, fit.MaximumClearanceInMillimetres, 9);
    }

    [Fact]
    public void JS_over_js_is_a_transition_fit_with_symmetrical_play() {
        // Arrange
        Fit fit = new Fit(Hole(25, "JS", 7), Shaft(25, "js", 7));

        // Assert
        Assert.Equal(FitKind.Transition, fit.Kind);
        Assert.Equal(-fit.MaximumClearanceInMillimetres, fit.MinimumClearanceInMillimetres, 9);
    }

    [Fact]
    public void H_over_h_of_the_same_grade_has_no_play_at_the_tightest_so_it_is_a_clearance_fit() {
        // Arrange
        Fit fit = new Fit(Hole(50, "H", 8), Shaft(50, "h", 8));

        // Assert
        Assert.Equal(FitKind.Clearance, fit.Kind);
        Assert.Equal(0.0, fit.MinimumClearanceInMillimetres, 9);
        Assert.Equal(0.078, fit.MaximumClearanceInMillimetres, 9);
    }

    [Fact]
    public void The_clearances_do_not_depend_on_the_unit_the_features_are_drawn_in() {
        // Arrange
        Fit inMillimetres = new Fit(Hole(25.4, "H", 7), Shaft(25.4, "h", 6));
        Fit inInches      = new Fit(Hole(1.0, "H", 7, LengthUnit.Inch), Shaft(1.0, "h", 6, LengthUnit.Inch));

        // Assert
        Assert.Equal(inMillimetres.MaximumClearanceInMillimetres, inInches.MaximumClearanceInMillimetres, 9);
        Assert.Equal(inMillimetres.MinimumClearanceInMillimetres, inInches.MinimumClearanceInMillimetres, 9);
    }

    public static TheoryData<string, string, int, int> EveryZonePairAndGrade() {
        TheoryData<string, string, int, int> data = new();
        foreach (string holeZone in new[] { "H", "JS" }) {
            foreach (string shaftZone in new[] { "h", "js" }) {
                for (int holeGrade = 6; holeGrade <= 11; holeGrade++) {
                    for (int shaftGrade = 6; shaftGrade <= 11; shaftGrade++) {
                        data.Add(holeZone, shaftZone, holeGrade, shaftGrade);
                    }
                }
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(EveryZonePairAndGrade))]
    public void Whatever_the_zones_and_grades_the_largest_clearance_is_positive(string holeZone, string shaftZone, int holeGrade, int shaftGrade) {
        // Arrange
        Fit fit = new Fit(Hole(80, holeZone, holeGrade), Shaft(80, shaftZone, shaftGrade));

        // Assert
        Assert.True(fit.MaximumClearanceInMillimetres > 0);
        Assert.NotEqual(FitKind.Interference, fit.Kind);
    }

    [Fact(Skip = "domain defect: no hole/shaft pair can be an Interference fit (only H, JS holes and h, js shafts exist), so FitKind.Interference is unreachable; a press fit such as H7/p6 cannot be expressed")]
    public void A_press_fit_is_an_interference_fit() {
        Assert.Fail("An interference fit cannot be constructed with the supported zones.");
    }

}
