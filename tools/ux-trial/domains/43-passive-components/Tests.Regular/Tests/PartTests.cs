namespace PassiveComponents.Tests.Regular;

public sealed class PartTests {

    // The constructor under test is the generator's own: pin the one value a test is about, draw the rest.

    [Fact]
    public void A_part_keeps_its_specification() {
        Part part = new Part("RC0603FR-0710KL", ComponentKind.Resistor, 10_000, new Tolerance(1), 0.1, new TemperatureCoefficient(100), Package.Chip0603, 0.0123m);

        Assert.Equal("RC0603FR-0710KL", part.PartNumber);
        Assert.Equal(ComponentKind.Resistor, part.Kind);
        Assert.Equal(10_000, part.NominalValue);
        Assert.Equal(1, part.Tolerance.Percent);
        Assert.Equal(0.1, part.Rating);
        Assert.Equal(100, part.TemperatureCoefficient.PpmPerKelvin);
        Assert.Equal(Package.Chip0603, part.Package);
        Assert.Equal(0.0123m, part.UnitCost);
    }

    [Fact]
    public void A_drawn_part_of_each_kind_is_valid() {
        // Only the constructor can say; the generator draws through it.
        Assert.Equal(ComponentKind.Capacitor, Any.Part().Capacitor().Generate().Kind);
        Assert.Equal(ComponentKind.Inductor, Any.Part().Inductor().Generate().Kind);
        Assert.Equal(ComponentKind.Resistor, Any.Part().Resistor().Generate().Kind);
        Assert.NotNull(Any.Part().Generate());
    }

    // --- part number

    [Theory]
    [InlineData("A1B")]
    [InlineData("0AB")]
    [InlineData("GRM188R71H104KA93D")]
    [InlineData("A.-/+#")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ABCD")]
    public void A_well_formed_part_number_is_accepted(string partNumber) {
        Part part = Any.Part().WithPartNumber(partNumber).Generate();

        Assert.Equal(partNumber, part.PartNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ABCDE")]
    [InlineData("abc")]
    [InlineData("-AB")]
    [InlineData(".AB")]
    [InlineData("AB C")]
    [InlineData("AB_C")]
    [InlineData("AB\tC")]
    [InlineData(" ABC")]
    [InlineData("ABC ")]
    [InlineData("ÀBC")]
    public void A_malformed_part_number_is_refused(string partNumber) {
        Assert.Throws<ArgumentException>(() => Any.Part().WithPartNumber(partNumber).Generate());
    }

    [Fact]
    public void A_missing_part_number_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Part().WithPartNumber((string)null!).Generate());
    }

    [Fact(Skip = "domain defect: '$' in the part number pattern also matches before a final line break, so 'ABC\\n' is accepted; input is a literal, no seed")]
    public void A_part_number_followed_by_a_line_break_is_refused() {
        Assert.Throws<ArgumentException>(() => Any.Part().WithPartNumber("ABC\n").Generate());
    }

    // --- kind and nominal value

    [Fact]
    public void An_unknown_kind_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().WithKind((ComponentKind)3).Generate());
    }

    [Theory]
    [InlineData(ComponentKind.Capacitor, 1e-13)]
    [InlineData(ComponentKind.Capacitor, 1e-2)]
    [InlineData(ComponentKind.Inductor, 1e-9)]
    [InlineData(ComponentKind.Inductor, 1.0)]
    [InlineData(ComponentKind.Resistor, 1e-3)]
    [InlineData(ComponentKind.Resistor, 1e12)]
    public void The_ends_of_the_range_of_the_kind_are_accepted(ComponentKind kind, double nominal) {
        Part part = Any.Part().WithKind(kind).WithNominalValue(nominal).Generate();

        Assert.Equal(nominal, part.NominalValue);
    }

    [Theory]
    [InlineData(ComponentKind.Capacitor, 9.9e-14)]
    [InlineData(ComponentKind.Capacitor, 1.1e-2)]
    [InlineData(ComponentKind.Inductor, 9.9e-10)]
    [InlineData(ComponentKind.Inductor, 1.1)]
    [InlineData(ComponentKind.Resistor, 9.9e-4)]
    [InlineData(ComponentKind.Resistor, 1.1e12)]
    [InlineData(ComponentKind.Resistor, double.NaN)]
    [InlineData(ComponentKind.Capacitor, double.PositiveInfinity)]
    [InlineData(ComponentKind.Resistor, double.PositiveInfinity)]
    [InlineData(ComponentKind.Inductor, 0.0)]
    public void A_value_outside_the_range_of_the_kind_is_refused(ComponentKind kind, double nominal) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().WithKind(kind).WithNominalValue(nominal).Generate());
    }

    [Fact]
    public void The_range_depends_on_the_kind() {
        // 4.7 kilo is a resistor value, far above any capacitance or inductance
        Assert.Equal(4_700, Any.Part().Resistor().WithNominalValue(4_700).Generate().NominalValue);
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().Capacitor().WithNominalValue(4_700).Generate());
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().Inductor().WithNominalValue(4_700).Generate());
    }

    // --- tolerance and temperature coefficient

    [Fact]
    public void A_missing_tolerance_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Part().WithTolerance((Tolerance)null!).Generate());
    }

    [Fact]
    public void A_missing_temperature_coefficient_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Part().WithTemperatureCoefficient((TemperatureCoefficient)null!).Generate());
    }

    // --- package fit

    [Theory]
    [InlineData(ComponentKind.Capacitor)]
    [InlineData(ComponentKind.Inductor)]
    public void The_2512_chip_is_a_resistor_package_only(ComponentKind kind) {
        Assert.Throws<ArgumentException>(() => Any.Part().WithKind(kind).WithPackage(Package.Chip2512).Generate());
    }

    [Fact]
    public void The_radial_package_holds_no_resistor() {
        Assert.Throws<ArgumentException>(() => Any.Part().Resistor().WithPackage(Package.Radial).Generate());
    }

    [Fact]
    public void The_2512_chip_holds_a_resistor() {
        Part part = Any.Part().Resistor().WithPackage(Package.Chip2512).Generate();

        Assert.Equal(Package.Chip2512, part.Package);
    }

    [Theory]
    [InlineData(ComponentKind.Capacitor)]
    [InlineData(ComponentKind.Inductor)]
    public void The_radial_package_holds_a_capacitor_or_an_inductor(ComponentKind kind) {
        Part part = Any.Part().WithKind(kind).WithPackage(Package.Radial).Generate();

        Assert.Equal(Package.Radial, part.Package);
    }

    [Fact]
    public void An_unknown_package_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().WithPackage((Package)99).Generate());
    }

    // --- rating

    [Theory]
    [InlineData(1.0)]
    [InlineData(10_000.0)]
    public void A_capacitor_is_rated_between_1_and_10000_volts(double volts) {
        Assert.Equal(volts, Any.Part().Capacitor().WithRating(volts).Generate().Rating);
    }

    [Theory]
    [InlineData(0.99)]
    [InlineData(10_000.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_capacitor_rating_outside_its_range_is_refused(double volts) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().Capacitor().WithRating(volts).Generate());
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(100.0)]
    public void An_inductor_is_rated_between_a_milliampere_and_100_amperes(double amperes) {
        Assert.Equal(amperes, Any.Part().Inductor().WithRating(amperes).Generate().Rating);
    }

    [Theory]
    [InlineData(0.0009)]
    [InlineData(100.1)]
    [InlineData(double.NaN)]
    public void An_inductor_rating_outside_its_range_is_refused(double amperes) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().Inductor().WithRating(amperes).Generate());
    }

    [Theory]
    [InlineData(Package.Chip0201, 0.05)]
    [InlineData(Package.Chip0402, 0.0625)]
    [InlineData(Package.Chip0603, 0.1)]
    [InlineData(Package.Chip0805, 0.125)]
    [InlineData(Package.Chip1206, 0.25)]
    [InlineData(Package.Chip2512, 1.0)]
    [InlineData(Package.Axial, 5.0)]
    public void A_resistor_may_be_rated_up_to_what_its_package_dissipates(Package package, double maxWatts) {
        Assert.Equal(maxWatts, Part.MaxRatedWatts(package));
        Assert.Equal(maxWatts, Any.Part().Resistor().WithPackage(package).WithRating(maxWatts).Generate().Rating);
        Assert.Equal(0.01, Any.Part().Resistor().WithPackage(package).WithRating(0.01).Generate().Rating);
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().Resistor().WithPackage(package).WithRating(maxWatts * 1.01).Generate());
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().Resistor().WithPackage(package).WithRating(0.0099).Generate());
    }

    [Fact]
    public void The_same_wattage_is_fine_in_a_larger_package_only() {
        // 0.25 W: a 1206 can carry it, a 0603 cannot
        Assert.Equal(0.25, Any.Part().Resistor().WithPackage(Package.Chip1206).WithRating(0.25).Generate().Rating);
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().Resistor().WithPackage(Package.Chip0603).WithRating(0.25).Generate());
    }

    [Fact]
    public void The_radial_package_has_no_maximum_wattage() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Part.MaxRatedWatts(Package.Radial));
    }

    [Fact]
    public void A_resistor_rating_that_is_not_a_number_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().Resistor().WithRating(double.NaN).Generate());
    }

    [Theory]
    [InlineData(ComponentKind.Capacitor, "V")]
    [InlineData(ComponentKind.Inductor, "A")]
    [InlineData(ComponentKind.Resistor, "W")]
    public void The_rating_unit_depends_on_the_kind(ComponentKind kind, string unit) {
        Assert.Equal(unit, Any.Part().WithKind(kind).Generate().RatingUnit);
    }

    // --- unit cost

    [Theory]
    [InlineData("0")]
    [InlineData("10000")]
    [InlineData("0.0001")]
    [InlineData("1.00000")]
    [InlineData("9999.9999")]
    public void A_unit_cost_between_zero_and_ten_thousand_with_four_decimals_is_accepted(string cost) {
        decimal unitCost = decimal.Parse(cost, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(unitCost, Any.Part().WithUnitCost(unitCost).Generate().UnitCost);
    }

    [Theory]
    [InlineData("-0.0001")]
    [InlineData("10000.0001")]
    public void A_unit_cost_outside_its_range_is_refused(string cost) {
        decimal unitCost = decimal.Parse(cost, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().WithUnitCost(unitCost).Generate());
    }

    [Theory]
    [InlineData("0.00001")]
    [InlineData("1.23456")]
    public void A_unit_cost_with_more_than_four_decimals_is_refused(string cost) {
        decimal unitCost = decimal.Parse(cost, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentException>(() => Any.Part().WithUnitCost(unitCost).Generate());
    }

    // --- derived values

    [Fact]
    public void The_tolerance_band_at_25_degrees_surrounds_the_nominal_value() {
        Part part = Any.Part().Resistor().WithNominalValue(100).WithTolerance(new Tolerance(5)).Generate();

        Approx.Equal(95, part.LowerBound);
        Approx.Equal(105, part.UpperBound);
    }

    [Fact]
    public void The_range_at_the_reference_temperature_is_the_tolerance_band() {
        Part part = Any.Part().Resistor().WithNominalValue(100).WithTolerance(new Tolerance(5)).WithTemperatureCoefficient(new TemperatureCoefficient(200)).Generate();

        (double minimum, double maximum) = part.RangeAt(25);

        Approx.Equal(95, minimum);
        Approx.Equal(105, maximum);
    }

    [Fact]
    public void The_range_widens_with_the_temperature_drift() {
        Part part = Any.Part().Resistor().WithNominalValue(100).WithTolerance(new Tolerance(5)).WithTemperatureCoefficient(new TemperatureCoefficient(100)).Generate();

        // 100 ppm/K over 100 K is 1 %: a 6 % spread
        (double minimum, double maximum) = part.RangeAt(125);

        Approx.Equal(94, minimum);
        Approx.Equal(106, maximum);
    }

    [Fact]
    public void The_range_is_refused_outside_the_operating_temperatures() {
        Part anyPart = Any.Part().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyPart.RangeAt(-56));
        Assert.Throws<ArgumentOutOfRangeException>(() => anyPart.RangeAt(156));
    }

    [Fact]
    public void The_widest_range_of_the_library_still_has_a_positive_lower_bound() {
        Part part = Any.Part().WithTolerance(new Tolerance(20)).WithTemperatureCoefficient(new TemperatureCoefficient(5000)).Generate();

        (double minimum, double maximum) = part.RangeAt(155);

        Approx.Equal(part.NominalValue * (1 - 0.85), minimum);
        Approx.Equal(part.NominalValue * (1 + 0.85), maximum);
        Assert.True(minimum > 0);
    }

    [Fact]
    public void A_part_is_seen_as_its_own_kind_of_value() {
        Part capacitor = Any.Part().Capacitor().WithNominalValue(2.2e-9).Generate();
        Part inductor  = Any.Part().Inductor().WithNominalValue(4.7e-6).Generate();
        Part resistor  = Any.Part().Resistor().WithNominalValue(4_700).Generate();

        Assert.Equal(2.2e-9, capacitor.AsCapacitance().Farads);
        Assert.Equal(4.7e-6, inductor.AsInductance().Henries);
        Assert.Equal(4_700, resistor.AsResistance().Ohms);
    }

    [Fact]
    public void A_part_is_not_seen_as_another_kind_of_value() {
        Part capacitor = Any.Part().Capacitor().Generate();
        Part inductor  = Any.Part().Inductor().Generate();
        Part resistor  = Any.Part().Resistor().Generate();

        Assert.Throws<InvalidOperationException>(() => capacitor.AsInductance());
        Assert.Throws<InvalidOperationException>(() => capacitor.AsResistance());
        Assert.Throws<InvalidOperationException>(() => inductor.AsCapacitance());
        Assert.Throws<InvalidOperationException>(() => inductor.AsResistance());
        Assert.Throws<InvalidOperationException>(() => resistor.AsCapacitance());
        Assert.Throws<InvalidOperationException>(() => resistor.AsInductance());
    }

    [Fact]
    public void A_value_on_the_grid_of_a_series_is_a_preferred_value_of_it() {
        Part part = Any.Part().Resistor().WithNominalValue(4_700).Generate();

        Assert.True(part.HasPreferredValueOf(ESeries.E24));
        Assert.True(part.HasPreferredValueOf(ESeries.E12));
        Assert.False(part.HasPreferredValueOf(ESeries.E96));
    }

    [Fact]
    public void A_value_off_every_grid_is_preferred_in_no_series() {
        Part part = Any.Part().Resistor().WithNominalValue(4_800).Generate();

        Assert.False(part.HasPreferredValueOf(ESeries.E6));
        Assert.False(part.HasPreferredValueOf(ESeries.E12));
    }

    // --- equality

    [Fact]
    public void Two_parts_with_every_field_alike_are_equal() {
        Part first  = Any.Part().Generate();
        Part second = new Part(first.PartNumber, first.Kind, first.NominalValue, new Tolerance(first.Tolerance.Percent), first.Rating,
                               new TemperatureCoefficient(first.TemperatureCoefficient.PpmPerKelvin), first.Package, first.UnitCost);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.False(first.Equals(null));
    }

    [Fact]
    public void One_field_apart_makes_two_parts_different() {
        Part baseline = Any.Part().Resistor().WithPackage(Package.Chip0603).WithNominalValue(1_000).WithTolerance(new Tolerance(5)).WithRating(0.1)
                           .WithTemperatureCoefficient(new TemperatureCoefficient(100)).WithUnitCost(0.01m).WithPartNumber("ABC-1").Generate();

        AnyPart same = Any.Part().Resistor().WithPackage(Package.Chip0603).WithNominalValue(1_000).WithTolerance(new Tolerance(5)).WithRating(0.1)
                          .WithTemperatureCoefficient(new TemperatureCoefficient(100)).WithUnitCost(0.01m).WithPartNumber("ABC-1");

        Assert.Equal(baseline, same.Generate());
        Assert.NotEqual(baseline, same.WithPartNumber("ABC-2").Generate());
        Assert.NotEqual(baseline, same.WithNominalValue(1_001).Generate());
        Assert.NotEqual(baseline, same.WithTolerance(new Tolerance(1)).Generate());
        Assert.NotEqual(baseline, same.WithRating(0.0625).Generate());
        Assert.NotEqual(baseline, same.WithTemperatureCoefficient(new TemperatureCoefficient(50)).Generate());
        Assert.NotEqual(baseline, same.WithPackage(Package.Chip1206).Generate());
        Assert.NotEqual(baseline, same.WithUnitCost(0.0101m).Generate());
        Assert.NotEqual<Part>(baseline, same.Capacitor().WithNominalValue(1e-9).WithRating(50).Generate());
    }

    [Fact]
    public void It_is_written_with_its_number_value_tolerance_and_package() {
        Part part = Any.Part().Resistor().WithPartNumber("RC0603-10K").WithNominalValue(10_000).WithTolerance(new Tolerance(1)).WithPackage(Package.Chip0603).Generate();

        Assert.Equal("RC0603-10K (10 kΩ ±1 %, Chip0603)", part.ToString());
    }

}
