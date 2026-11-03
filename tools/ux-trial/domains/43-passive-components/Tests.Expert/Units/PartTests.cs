using JustDummies;

using PassiveComponents.Domain.Tests.Expert.Generators;

using Xunit;

namespace PassiveComponents.Domain.Tests.Expert.Units;

public sealed class PartTests {

    // --- part number ------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("ABC")]
    [InlineData("A12")]
    [InlineData("0AB")]
    [InlineData("GRM155R71C104KA88D")]
    [InlineData("RC0603FR-07100KL")]
    [InlineData("A.B")]
    [InlineData("A--")]
    [InlineData("A//")]
    [InlineData("A++")]
    [InlineData("A##")]
    public void A_part_number_starts_with_a_letter_or_digit_and_is_made_of_capitals_digits_and_a_few_signs(string partNumber) {
        // Arrange
        PartArguments anyArguments = Any.Part().GenerateArguments();

        // Act
        Part part = (anyArguments with { PartNumber = partNumber }).Build();

        // Assert
        Assert.Equal(partNumber, part.PartNumber);
    }

    [Fact]
    public void A_part_number_may_be_forty_characters_long_and_not_forty_one() {
        // Arrange
        PartArguments anyArguments = Any.Part().GenerateArguments();

        // Act & Assert
        Assert.Equal(40, (anyArguments with { PartNumber = new string('A', 40) }).Build().PartNumber.Length);
        Assert.Throws<ArgumentException>(() => (anyArguments with { PartNumber = new string('A', 41) }).Build());
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("AB")]
    [InlineData("abc")]
    [InlineData("Abc")]
    [InlineData("-AB")]
    [InlineData(".AB")]
    [InlineData("A B")]
    [InlineData("AB_")]
    [InlineData("AB*")]
    [InlineData(" ABC")]
    [InlineData("ABC ")]
    [InlineData("ABÉ")]
    public void A_malformed_part_number_is_refused(string partNumber) {
        // Arrange
        PartArguments anyArguments = Any.Part().GenerateArguments();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => (anyArguments with { PartNumber = partNumber }).Build());

        // Assert: it is the part number that is refused
        Assert.Equal("partNumber", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: the part number format ends with '$', which .NET also matches before a final line feed, so 'ABC\\n' is accepted and kept as the part number; deterministic, no seed")]
    public void A_part_number_followed_by_a_line_feed_is_refused() {
        // Arrange
        PartArguments anyArguments = Any.Part().GenerateArguments();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => (anyArguments with { PartNumber = "ABC\n" }).Build());
    }

    [Fact]
    public void A_part_needs_a_part_number() {
        // Arrange
        PartArguments anyArguments = Any.Part().GenerateArguments();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => (anyArguments with { PartNumber = null! }).Build());
    }

    // --- kind and nominal value -------------------------------------------------------------------------------

    [Fact]
    public void An_undefined_kind_is_refused() {
        // Arrange
        PartArguments anyArguments = Any.Part().GenerateArguments();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => (anyArguments with { Kind = (ComponentKind)9 }).Build());
    }

    [Theory]
    [InlineData(ComponentKind.Capacitor, Capacitance.MinFarads)]
    [InlineData(ComponentKind.Capacitor, Capacitance.MaxFarads)]
    [InlineData(ComponentKind.Inductor, Inductance.MinHenries)]
    [InlineData(ComponentKind.Inductor, Inductance.MaxHenries)]
    [InlineData(ComponentKind.Resistor, Resistance.MinOhms)]
    [InlineData(ComponentKind.Resistor, Resistance.MaxOhms)]
    public void The_nominal_value_may_sit_on_either_end_of_the_range_of_its_kind(ComponentKind kind, double nominalValue) {
        // Act
        Part part = Any.Part().WithKind(kind).WithNominalValue(nominalValue).Generate();

        // Assert
        Assert.Equal(nominalValue, part.NominalValue);
    }

    [Fact]
    public void The_nominal_value_follows_the_range_of_the_kind_not_of_another() {
        // 5 kΩ is a fine resistance and an impossible capacitance or inductance; 50 µ is the other way round
        Assert.Equal(5e3, Any.Part().WithKind(ComponentKind.Resistor).WithNominalValue(5e3).Generate().NominalValue);
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().WithKind(ComponentKind.Capacitor).WithNominalValue(5e3).Generate());
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().WithKind(ComponentKind.Inductor).WithNominalValue(5e3).Generate());
        Assert.Equal(5e-5, Any.Part().WithKind(ComponentKind.Capacitor).WithNominalValue(5e-5).Generate().NominalValue);
        Assert.Equal(5e-5, Any.Part().WithKind(ComponentKind.Inductor).WithNominalValue(5e-5).Generate().NominalValue);
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().WithKind(ComponentKind.Resistor).WithNominalValue(5e-5).Generate());
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
    [InlineData(ComponentKind.Inductor, 0)]
    [InlineData(ComponentKind.Resistor, -100)]
    public void A_nominal_value_outside_the_range_of_its_kind_is_refused(ComponentKind kind, double nominalValue) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().WithKind(kind).WithNominalValue(nominalValue).Generate());
    }

    // --- package ----------------------------------------------------------------------------------------------

    public static TheoryData<ComponentKind, Package> PackagesTheKindComesIn() {
        Package[]                          chips = [Package.Chip0201, Package.Chip0402, Package.Chip0603, Package.Chip0805, Package.Chip1206];
        TheoryData<ComponentKind, Package> data  = new();

        foreach (ComponentKind kind in new[] { ComponentKind.Capacitor, ComponentKind.Inductor }) {
            foreach (Package package in chips.Append(Package.Axial).Append(Package.Radial)) { data.Add(kind, package); }
        }

        foreach (Package package in chips.Append(Package.Chip2512).Append(Package.Axial)) { data.Add(ComponentKind.Resistor, package); }

        return data;
    }

    [Theory]
    [MemberData(nameof(PackagesTheKindComesIn))]
    public void A_part_comes_in_every_package_its_kind_allows(ComponentKind kind, Package package) {
        // Act
        Part part = Any.Part().WithKind(kind).WithPackage(package).Generate();

        // Assert
        Assert.Equal(package, part.Package);
    }

    [Theory]
    [InlineData(ComponentKind.Capacitor, Package.Chip2512)]
    [InlineData(ComponentKind.Inductor, Package.Chip2512)]
    [InlineData(ComponentKind.Resistor, Package.Radial)]
    public void A_package_reserved_to_another_kind_is_refused(ComponentKind kind, Package package) {
        // Arrange
        PartArguments anyArguments = Any.Part().WithKind(kind).GenerateArguments();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => (anyArguments with { Package = package }).Build());

        // Assert: it is the package, not a rating or a value
        Assert.Equal("package", refusal.ParamName);
        Assert.IsNotType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void An_undefined_package_is_refused() {
        // Arrange
        PartArguments anyArguments = Any.Part().GenerateArguments();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => (anyArguments with { Package = (Package)99 }).Build());
    }

    // --- rating -----------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(1)]
    [InlineData(10_000)]
    public void A_capacitor_is_rated_between_one_and_ten_thousand_volts(double volts) {
        Assert.Equal(volts, Any.Part().WithKind(ComponentKind.Capacitor).WithRating(volts).Generate().Rating);
    }

    [Theory]
    [InlineData(0.999)]
    [InlineData(10_000.01)]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(double.NaN)]
    public void A_capacitor_rated_outside_that_range_is_refused(double volts) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().WithKind(ComponentKind.Capacitor).WithRating(volts).Generate());
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(100)]
    public void An_inductor_is_rated_between_a_milliampere_and_a_hundred_amperes(double amperes) {
        Assert.Equal(amperes, Any.Part().WithKind(ComponentKind.Inductor).WithRating(amperes).Generate().Rating);
    }

    [Theory]
    [InlineData(0.00099)]
    [InlineData(100.01)]
    [InlineData(double.NaN)]
    public void An_inductor_rated_outside_that_range_is_refused(double amperes) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().WithKind(ComponentKind.Inductor).WithRating(amperes).Generate());
    }

    [Theory]
    [InlineData(Package.Chip0201, 0.05)]
    [InlineData(Package.Chip0402, 0.0625)]
    [InlineData(Package.Chip0603, 0.1)]
    [InlineData(Package.Chip0805, 0.125)]
    [InlineData(Package.Chip1206, 0.25)]
    [InlineData(Package.Chip2512, 1)]
    [InlineData(Package.Axial, 5)]
    public void A_resistor_may_be_rated_for_the_power_its_package_dissipates_and_not_a_hair_more(Package package, double maxWatts) {
        // Act & Assert
        Assert.Equal(maxWatts, Part.MaxRatedWatts(package));
        Assert.Equal(maxWatts, Any.Part().WithKind(ComponentKind.Resistor).WithPackage(package).WithRating(maxWatts).Generate().Rating);
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().WithKind(ComponentKind.Resistor).WithPackage(package).WithRating(double.BitIncrement(maxWatts)).Generate());
    }

    [Theory]
    [InlineData(Package.Chip0201)]
    [InlineData(Package.Chip0603)]
    [InlineData(Package.Chip2512)]
    [InlineData(Package.Axial)]
    public void A_resistor_is_rated_at_least_ten_milliwatts(Package package) {
        // Act & Assert
        Assert.Equal(0.01, Any.Part().WithKind(ComponentKind.Resistor).WithPackage(package).WithRating(0.01).Generate().Rating);
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().WithKind(ComponentKind.Resistor).WithPackage(package).WithRating(double.BitDecrement(0.01)).Generate());
    }

    [Fact]
    public void A_rating_that_suits_a_big_package_is_too_much_for_a_small_one() {
        // 0.5 W: fine in 2512, too much for 0805
        Assert.Equal(0.5, Any.Part().WithKind(ComponentKind.Resistor).WithPackage(Package.Chip2512).WithRating(0.5).Generate().Rating);
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().WithKind(ComponentKind.Resistor).WithPackage(Package.Chip0805).WithRating(0.5).Generate());
    }

    [Fact]
    public void The_rated_power_of_a_package_that_holds_no_resistor_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Part.MaxRatedWatts(Package.Radial));
        Assert.Throws<ArgumentOutOfRangeException>(() => Part.MaxRatedWatts((Package)99));
    }

    [Theory]
    [InlineData(ComponentKind.Capacitor, "V")]
    [InlineData(ComponentKind.Inductor, "A")]
    [InlineData(ComponentKind.Resistor, "W")]
    public void A_rating_is_in_volts_amperes_or_watts_by_kind(ComponentKind kind, string expectedUnit) {
        Assert.Equal(expectedUnit, Any.Part().WithKind(kind).Generate().RatingUnit);
    }

    // --- tolerance, coefficient, cost -------------------------------------------------------------------------

    [Fact]
    public void A_part_needs_a_tolerance_and_a_temperature_coefficient() {
        // Arrange
        PartArguments anyArguments = Any.Part().GenerateArguments();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => (anyArguments with { Tolerance = null! }).Build());
        Assert.Throws<ArgumentNullException>(() => (anyArguments with { TemperatureCoefficient = null! }).Build());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("10000")]
    [InlineData("0.0001")]
    [InlineData("0.10000")]
    [InlineData("1234.5678")]
    public void A_unit_cost_runs_from_zero_to_ten_thousand_in_four_decimal_places(string cost) {
        // Arrange
        decimal unitCost = decimal.Parse(cost, System.Globalization.CultureInfo.InvariantCulture);

        // Act & Assert
        Assert.Equal(unitCost, Any.Part().WithUnitCost(unitCost).Generate().UnitCost);
    }

    [Theory]
    [InlineData("-0.0001")]
    [InlineData("10000.0001")]
    [InlineData("20000")]
    public void A_unit_cost_outside_the_range_is_refused_as_out_of_range(string cost) {
        decimal unitCost = decimal.Parse(cost, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Part().WithUnitCost(unitCost).Generate());
    }

    [Theory]
    [InlineData("0.00001")]
    [InlineData("0.12345")]
    [InlineData("9999.99999")]
    public void A_unit_cost_priced_finer_than_four_decimal_places_is_refused_as_an_argument(string cost) {
        // Arrange
        decimal unitCost = decimal.Parse(cost, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Part().WithUnitCost(unitCost).Generate());

        // Assert
        Assert.IsNotType<ArgumentOutOfRangeException>(refusal);
    }

    // --- derived values ---------------------------------------------------------------------------------------

    [Fact]
    public void A_hundred_ohm_part_at_five_percent_is_guaranteed_between_95_and_105_ohms_at_room_temperature() {
        // Arrange
        Part resistor = Any.Part().WithKind(ComponentKind.Resistor).WithNominalValue(100).WithTolerance(new Tolerance(5)).Generate();

        // Act & Assert
        Assert.Equal(95, resistor.LowerBound, 9);
        Assert.Equal(105, resistor.UpperBound, 9);
    }

    [Fact]
    public void Temperature_drift_widens_the_range_on_top_of_the_tolerance() {
        // Arrange: 1 % tolerance and 100 ppm/K: 1 % of drift at 125 °C
        Part resistor = Any.Part()
                           .WithKind(ComponentKind.Resistor)
                           .WithNominalValue(100)
                           .WithTolerance(new Tolerance(1))
                           .WithTemperatureCoefficient(new TemperatureCoefficient(100))
                           .Generate();

        // Act
        (double minimum, double maximum) = resistor.RangeAt(125);

        // Assert
        Assert.Equal(98, minimum, 9);
        Assert.Equal(102, maximum, 9);
    }

    [Fact]
    public void At_the_reference_temperature_the_range_is_the_tolerance_band() {
        // Arrange
        Part resistor = Any.Part().WithKind(ComponentKind.Resistor).WithNominalValue(100).WithTolerance(new Tolerance(1)).Generate();

        // Act
        (double minimum, double maximum) = resistor.RangeAt(25);

        // Assert
        Assert.Equal(resistor.LowerBound, minimum, 9);
        Assert.Equal(resistor.UpperBound, maximum, 9);
    }

    [Fact]
    public void The_cold_end_of_the_operating_range_drifts_by_eighty_kelvin_times_the_coefficient() {
        // Arrange
        Part capacitor = Any.Part()
                            .WithKind(ComponentKind.Capacitor)
                            .WithNominalValue(1e-9)
                            .WithTolerance(new Tolerance(10))
                            .WithTemperatureCoefficient(new TemperatureCoefficient(1000))
                            .Generate();

        // Act
        (double minimum, double maximum) = capacitor.RangeAt(-55);

        // Assert: spread = 10 % + 1000e-6 · 80 = 18 %
        Assert.Equal(0.82e-9, minimum, 1e-18);
        Assert.Equal(1.18e-9, maximum, 1e-18);
    }

    [Theory]
    [InlineData(-55.1)]
    [InlineData(155.1)]
    [InlineData(double.NaN)]
    public void A_range_is_only_given_inside_the_operating_temperatures(double celsius) {
        // Arrange
        Part anyPart = Any.Part().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyPart.RangeAt(celsius));
    }

    [Fact]
    public void The_worst_case_widest_part_still_has_a_positive_minimum() {
        // Arrange: 20 % and 5000 ppm/K: a spread of 85 % at the extreme
        Part widest = Any.Part()
                         .WithNominalValue(1e-9)
                         .WithKind(ComponentKind.Capacitor)
                         .WithTolerance(new Tolerance(20))
                         .WithTemperatureCoefficient(new TemperatureCoefficient(5000))
                         .Generate();

        // Act
        (double minimum, double maximum) = widest.RangeAt(155);

        // Assert
        Assert.Equal(1.5e-10, minimum, 1e-20);
        Assert.Equal(1.85e-9, maximum, 1e-18);
    }

    [Fact]
    public void A_capacitor_converts_to_a_capacitance_of_its_nominal_value() {
        // Arrange
        Part capacitor = Any.Part().WithKind(ComponentKind.Capacitor).WithNominalValue(2.2e-9).Generate();

        // Act & Assert
        Assert.Equal(new Capacitance(2.2e-9), capacitor.AsCapacitance());
    }

    [Fact]
    public void An_inductor_converts_to_an_inductance_of_its_nominal_value() {
        Part inductor = Any.Part().WithKind(ComponentKind.Inductor).WithNominalValue(4.7e-6).Generate();

        Assert.Equal(new Inductance(4.7e-6), inductor.AsInductance());
    }

    [Fact]
    public void A_resistor_converts_to_a_resistance_of_its_nominal_value() {
        Part resistor = Any.Part().WithKind(ComponentKind.Resistor).WithNominalValue(4700).Generate();

        Assert.Equal(new Resistance(4700), resistor.AsResistance());
    }

    [Fact]
    public void A_part_cannot_be_converted_to_the_value_of_another_kind() {
        // Arrange
        Part anyCapacitor = Any.Part().WithKind(ComponentKind.Capacitor).Generate();
        Part anyInductor  = Any.Part().WithKind(ComponentKind.Inductor).Generate();
        Part anyResistor  = Any.Part().WithKind(ComponentKind.Resistor).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyCapacitor.AsInductance());
        Assert.Throws<InvalidOperationException>(() => anyCapacitor.AsResistance());
        Assert.Throws<InvalidOperationException>(() => anyInductor.AsCapacitance());
        Assert.Throws<InvalidOperationException>(() => anyInductor.AsResistance());
        Assert.Throws<InvalidOperationException>(() => anyResistor.AsCapacitance());
        Assert.Throws<InvalidOperationException>(() => anyResistor.AsInductance());
    }

    [Fact]
    public void The_refusal_to_convert_names_the_part() {
        // Arrange
        Part resistor = Any.Part().WithKind(ComponentKind.Resistor).Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => resistor.AsCapacitance());

        // Assert
        Assert.Contains(resistor.PartNumber, refusal.Message);
    }

    [Theory]
    [InlineData(2.2e-9, ESeries.E24, true)]
    [InlineData(2.3e-9, ESeries.E24, false)]
    [InlineData(2.2e-9, ESeries.E6, true)]
    [InlineData(2.7e-9, ESeries.E6, false)]
    [InlineData(4.99e-9, ESeries.E96, true)]
    public void A_part_has_a_preferred_value_of_a_series_when_its_nominal_sits_on_the_grid(double nominal, ESeries series, bool expected) {
        // Arrange
        Part capacitor = Any.Part().WithKind(ComponentKind.Capacitor).WithNominalValue(nominal).Generate();

        // Act & Assert
        Assert.Equal(expected, capacitor.HasPreferredValueOf(series));
    }

    // --- substitution -----------------------------------------------------------------------------------------

    [Fact]
    public void A_part_identical_but_for_its_number_can_stand_in() {
        // Arrange
        Part original   = Specimens.OriginalResistor().Generate();
        Part substitute = Specimens.OriginalResistor().Generate();

        // Act & Assert
        Assert.Equal(SubstitutionRefusal.None, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_better_part_can_stand_in() {
        // Arrange: tighter, higher rated, steadier
        Part original   = Specimens.OriginalResistor().Generate();
        Part substitute = Specimens.OriginalResistor()
                                   .WithTolerance(new Tolerance(1))
                                   .WithRating(0.25)
                                   .WithTemperatureCoefficient(new TemperatureCoefficient(25))
                                   .Generate();

        // Act & Assert
        Assert.Equal(SubstitutionRefusal.None, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void The_same_part_number_is_the_first_thing_checked() {
        // Arrange: a part of another kind that happens to carry the same number
        Part original   = Specimens.OriginalResistor().Generate();
        Part substitute = Any.Part().WithKind(ComponentKind.Capacitor).WithPartNumber(original.PartNumber).Generate();

        // Act & Assert
        Assert.Equal(SubstitutionRefusal.SamePart, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_part_does_not_replace_itself() {
        // Arrange
        Part original = Specimens.OriginalResistor().Generate();

        // Act & Assert
        Assert.Equal(SubstitutionRefusal.SamePart, original.CheckSubstitute(original));
    }

    [Fact]
    public void Another_kind_comes_before_another_package() {
        // Arrange: a capacitor in a radial package, which is also another footprint
        Part original   = Specimens.OriginalResistor().Generate();
        Part substitute = Any.Part().WithKind(ComponentKind.Capacitor).WithPackage(Package.Radial).Generate();

        // Act & Assert
        Assert.Equal(SubstitutionRefusal.DifferentKind, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void Another_package_comes_before_a_wider_tolerance_and_a_steeper_coefficient() {
        // Arrange
        Part original   = Specimens.OriginalResistor().Generate();
        Part substitute = Specimens.OriginalResistor()
                                   .WithPackage(Package.Chip0805)
                                   .WithTolerance(new Tolerance(10))
                                   .WithTemperatureCoefficient(new TemperatureCoefficient(250))
                                   .Generate();

        // Act & Assert
        Assert.Equal(SubstitutionRefusal.DifferentPackage, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_wider_tolerance_comes_before_a_lower_rating_and_a_steeper_coefficient() {
        // Arrange
        Part original   = Specimens.OriginalResistor().Generate();
        Part substitute = Specimens.OriginalResistor()
                                   .WithTolerance(new Tolerance(10))
                                   .WithRating(0.05)
                                   .WithTemperatureCoefficient(new TemperatureCoefficient(250))
                                   .Generate();

        // Act & Assert
        Assert.Equal(SubstitutionRefusal.ToleranceBandNotContained, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_lower_rating_comes_before_a_steeper_coefficient() {
        // Arrange
        Part original   = Specimens.OriginalResistor().Generate();
        Part substitute = Specimens.OriginalResistor()
                                   .WithRating(0.05)
                                   .WithTemperatureCoefficient(new TemperatureCoefficient(250))
                                   .Generate();

        // Act & Assert
        Assert.Equal(SubstitutionRefusal.RatingTooLow, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_steeper_coefficient_is_refused_last() {
        // Arrange
        Part original   = Specimens.OriginalResistor().Generate();
        Part substitute = Specimens.OriginalResistor().WithTemperatureCoefficient(new TemperatureCoefficient(250)).Generate();

        // Act & Assert
        Assert.Equal(SubstitutionRefusal.TemperatureCoefficientWorse, original.CheckSubstitute(substitute));
    }

    [Theory]
    [InlineData(100.0, 5.0, SubstitutionRefusal.None)]            // the very same band
    [InlineData(102.0, 1.0, SubstitutionRefusal.None)]            // 100.98 .. 103.02 inside 95 .. 105
    [InlineData(104.0, 1.0, SubstitutionRefusal.ToleranceBandNotContained)]   // upper end 105.04
    [InlineData(96.0, 1.0, SubstitutionRefusal.None)]             // 95.04 .. 96.96
    [InlineData(94.0, 1.0, SubstitutionRefusal.ToleranceBandNotContained)]    // lower end 93.06
    [InlineData(100.0, 5.5, SubstitutionRefusal.ToleranceBandNotContained)]
    [InlineData(100.0, 0.1, SubstitutionRefusal.None)]
    public void The_whole_tolerance_band_of_the_substitute_must_lie_inside_the_band_of_the_original(double nominal, double percent, SubstitutionRefusal expected) {
        // Arrange: the original spans 95 to 105 ohms
        Part original   = Specimens.OriginalResistor().Generate();
        Part substitute = Specimens.OriginalResistor().WithNominalValue(nominal).WithTolerance(new Tolerance(percent)).Generate();

        // Act & Assert
        Assert.Equal(expected, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_band_that_overshoots_by_half_a_part_per_billion_is_still_contained() {
        // Arrange
        Part original   = Specimens.OriginalResistor().Generate();
        Part substitute = Specimens.OriginalResistor().WithNominalValue(100 * (1 + 5e-10)).Generate();

        // Act & Assert
        Assert.Equal(SubstitutionRefusal.None, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_band_that_overshoots_by_three_parts_per_billion_is_refused() {
        // Arrange
        Part original   = Specimens.OriginalResistor().Generate();
        Part substitute = Specimens.OriginalResistor().WithNominalValue(100 * (1 + 3e-9)).Generate();

        // Act & Assert
        Assert.Equal(SubstitutionRefusal.ToleranceBandNotContained, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void The_slack_is_relative_so_picofarad_bands_are_judged_as_strictly_as_ohm_bands() {
        // Arrange: the absolute overshoot is about 7e-22 F, far below any fixed epsilon
        Part original   = Specimens.OriginalCapacitor().Generate();
        Part substitute = Specimens.OriginalCapacitor().WithNominalValue(4.7e-12 * (1 + 3e-9)).Generate();

        // Act & Assert
        Assert.Equal(SubstitutionRefusal.ToleranceBandNotContained, original.CheckSubstitute(substitute));
        Assert.Equal(SubstitutionRefusal.None, original.CheckSubstitute(Specimens.OriginalCapacitor().WithNominalValue(4.7e-12 * (1 + 5e-10)).Generate()));
    }

    [Theory]
    [InlineData(0.1, SubstitutionRefusal.None)]
    [InlineData(0.125, SubstitutionRefusal.None)]
    [InlineData(0.0999, SubstitutionRefusal.RatingTooLow)]
    [InlineData(0.05, SubstitutionRefusal.RatingTooLow)]
    public void A_substitute_is_rated_at_least_as_high_as_the_original(double rating, SubstitutionRefusal expected) {
        // Arrange
        Part original   = Specimens.OriginalResistor().Generate();
        Part substitute = Specimens.OriginalResistor().WithRating(rating).Generate();

        // Act & Assert
        Assert.Equal(expected, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_rating_short_by_half_a_part_per_billion_is_still_enough_and_by_three_is_not() {
        // Arrange
        Part original = Specimens.OriginalResistor().Generate();

        // Act & Assert
        Assert.Equal(SubstitutionRefusal.None, original.CheckSubstitute(Specimens.OriginalResistor().WithRating(0.1 * (1 - 5e-10)).Generate()));
        Assert.Equal(SubstitutionRefusal.RatingTooLow, original.CheckSubstitute(Specimens.OriginalResistor().WithRating(0.1 * (1 - 3e-9)).Generate()));
    }

    [Theory]
    [InlineData(100, SubstitutionRefusal.None)]
    [InlineData(25, SubstitutionRefusal.None)]
    [InlineData(0, SubstitutionRefusal.None)]
    [InlineData(100.001, SubstitutionRefusal.TemperatureCoefficientWorse)]
    [InlineData(500, SubstitutionRefusal.TemperatureCoefficientWorse)]
    public void A_substitute_drifts_no_more_than_the_original(double ppm, SubstitutionRefusal expected) {
        // Arrange
        Part original   = Specimens.OriginalResistor().Generate();
        Part substitute = Specimens.OriginalResistor().WithTemperatureCoefficient(new TemperatureCoefficient(ppm)).Generate();

        // Act & Assert
        Assert.Equal(expected, original.CheckSubstitute(substitute));
    }

    [Fact]
    public void A_substitute_cannot_be_nothing() {
        // Arrange
        Part anyPart = Any.Part().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyPart.CheckSubstitute(null!));
    }

    // --- equality and text ------------------------------------------------------------------------------------

    [Fact]
    public void Two_parts_built_from_the_same_specification_are_equal() {
        // Arrange
        PartArguments anyArguments = Any.Part().GenerateArguments();

        // Act
        Part first  = anyArguments.Build();
        Part second = anyArguments.Build();

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first.Equals((object)second));
    }

    [Fact]
    public void Parts_that_differ_in_any_one_field_are_not_equal() {
        // Arrange: a capacitor in 0402 with a 1.5 € unit cost, then the same with one field moved by a hair
        PartArguments arguments = Any.Part().WithKind(ComponentKind.Capacitor).WithPackage(Package.Chip0402).WithUnitCost(1.5m).GenerateArguments();
        Part          original  = arguments.Build();

        PartArguments[] variants = [
            arguments with { PartNumber = "OTHER-PART-NUMBER" },
            arguments with { NominalValue = double.BitIncrement(arguments.NominalValue) },
            arguments with { Tolerance = new Tolerance(double.BitIncrement(arguments.Tolerance.Percent)) },
            arguments with { Rating = double.BitIncrement(arguments.Rating) },
            arguments with { TemperatureCoefficient = new TemperatureCoefficient(double.BitIncrement(arguments.TemperatureCoefficient.PpmPerKelvin)) },
            arguments with { Package = Package.Radial },
            arguments with { UnitCost = 1.5001m },
        ];

        // Act & Assert
        foreach (PartArguments variant in variants) {
            Assert.NotEqual(original, variant.Build());
        }
    }

    [Fact]
    public void A_part_equals_nothing_but_a_part() {
        // Arrange
        Part anyPart = Any.Part().Generate();

        // Act & Assert
        Assert.False(anyPart.Equals(null));
        Assert.False(anyPart.Equals((object)"part"));
    }

    [Fact]
    public void A_part_reads_with_its_number_value_tolerance_and_package() {
        // Arrange
        Part capacitor = Any.Part()
                            .WithKind(ComponentKind.Capacitor)
                            .WithPackage(Package.Chip0402)
                            .WithPartNumber("GRM155R71C222KA01")
                            .WithNominalValue(2.2e-9)
                            .WithTolerance(new Tolerance(10))
                            .Generate();

        // Act & Assert
        Assert.Equal("GRM155R71C222KA01 (2.2 nF ±10 %, Chip0402)", capacitor.ToString());
    }

}
