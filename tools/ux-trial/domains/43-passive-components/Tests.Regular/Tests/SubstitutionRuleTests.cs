namespace PassiveComponents.Tests.Regular;

/// <summary>The rule a part applies to a would-be substitute: first failure wins.</summary>
public sealed class SubstitutionRuleTests {

    private static AnyPart Original() => Specs.Resistor(100, 5, 0.1, 100);

    [Fact]
    public void A_tighter_slightly_different_resistor_may_replace_a_looser_one() {
        // 102 ohm +-1 % -> [100.98, 103.02] sits inside 100 ohm +-5 % -> [95, 105]
        Part original   = Original().Generate();
        Part substitute = Specs.Resistor(102, 1, 0.1, 100).Generate();

        Assert.Equal(SubstitutionRefusal.None, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_better_part_in_every_respect_may_replace_the_original() {
        Part original   = Original().Generate();
        Part substitute = Specs.Resistor(100, 1, 0.1, 25).WithRating(0.1).Generate();

        Assert.Equal(SubstitutionRefusal.None, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_part_with_the_same_number_is_refused_first() {
        Part original = Original().Generate();
        Part sameNumber = Original().WithPartNumber(original.PartNumber).WithNominalValue(1_000_000).WithRating(0.05).Generate();

        Assert.Equal(SubstitutionRefusal.SamePart, original.CheckSubstitute(sameNumber));
    }

    [Fact]
    public void A_part_of_another_kind_is_refused_before_its_package_or_its_band() {
        Part original = Original().Generate();
        Part capacitor = Specs.Capacitor(1e-9, 20, 50).WithPackage(Package.Chip0805).Generate();

        Assert.Equal(SubstitutionRefusal.DifferentKind, original.CheckSubstitute(capacitor));
    }

    [Fact]
    public void A_part_in_another_package_is_refused_before_its_band() {
        Part original   = Original().Generate();
        Part substitute = Specs.Resistor(200, 20, 0.01, 5000).WithPackage(Package.Chip0402).Generate();

        Assert.Equal(SubstitutionRefusal.DifferentPackage, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_wider_band_is_refused_before_the_rating() {
        Part original   = Original().Generate();
        Part substitute = Specs.Resistor(100, 10, 0.02, 5000).Generate();

        Assert.Equal(SubstitutionRefusal.ToleranceBandNotContained, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_band_that_leans_out_on_one_side_is_refused() {
        // 104 ohm +-1 % -> [102.96, 105.04]: the upper end passes 105
        Part original   = Original().Generate();
        Part substitute = Specs.Resistor(104, 1, 0.1, 100).Generate();

        Assert.Equal(SubstitutionRefusal.ToleranceBandNotContained, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_lower_rating_is_refused_before_the_temperature_coefficient() {
        Part original   = Original().Generate();
        Part substitute = Specs.Resistor(100, 5, 0.0625, 5000).Generate();

        Assert.Equal(SubstitutionRefusal.RatingTooLow, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_worse_temperature_coefficient_is_refused() {
        Part original   = Original().Generate();
        Part substitute = Specs.Resistor(100, 5, 0.1, 250).Generate();

        Assert.Equal(SubstitutionRefusal.TemperatureCoefficientWorse, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_band_exceeding_the_original_by_less_than_a_relative_billionth_is_contained() {
        Part original   = Original().Generate();
        Part substitute = Specs.Resistor(100 * (1 + 5e-10), 5, 0.1, 100).Generate();

        Assert.Equal(SubstitutionRefusal.None, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_band_exceeding_the_original_by_more_than_a_relative_billionth_is_not_contained() {
        Part original   = Original().Generate();
        Part substitute = Specs.Resistor(100 * (1 + 3e-9), 5, 0.1, 100).Generate();

        Assert.Equal(SubstitutionRefusal.ToleranceBandNotContained, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_rating_short_by_less_than_a_relative_billionth_is_enough() {
        Part original   = Original().Generate();
        Part substitute = Specs.Resistor(100, 5, 0.1 * (1 - 5e-10), 100).Generate();

        Assert.Equal(SubstitutionRefusal.None, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_temperature_coefficient_above_by_less_than_a_relative_billionth_is_no_worse() {
        Part original   = Original().Generate();
        Part substitute = Specs.Resistor(100, 5, 0.1, 100 * (1 + 5e-10)).Generate();

        Assert.Equal(SubstitutionRefusal.None, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_capacitor_is_judged_on_its_voltage() {
        Part original = Specs.Capacitor(100e-9, 10, 50).Generate();

        Assert.Equal(SubstitutionRefusal.None, original.CheckSubstitute(Specs.Capacitor(100e-9, 5, 63).Generate()));
        Assert.Equal(SubstitutionRefusal.RatingTooLow, original.CheckSubstitute(Specs.Capacitor(100e-9, 5, 25).Generate()));
    }

    [Fact]
    public void An_inductor_is_judged_on_its_current() {
        Part original = Specs.Inductor(10e-6, 2).Generate();

        Assert.Equal(SubstitutionRefusal.None, original.CheckSubstitute(Specs.Inductor(10e-6, 3).Generate()));
        Assert.Equal(SubstitutionRefusal.RatingTooLow, original.CheckSubstitute(Specs.Inductor(10e-6, 1.5).Generate()));
    }

    [Fact]
    public void A_substitute_is_required() {
        Part anyPart = Any.Part().Generate();

        Assert.Throws<ArgumentNullException>(() => anyPart.CheckSubstitute(null!));
    }

}
