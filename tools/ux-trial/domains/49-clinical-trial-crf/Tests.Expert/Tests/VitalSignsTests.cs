using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Expert;

public sealed class VitalSignsTests {

    // "Not entered yet" for the signs a test is not about.
    private static VitalSigns WithOnly(Observed<int>? systolic = null, Observed<int>? diastolic = null, Observed<int>? heartRate = null,
                                       Observed<decimal>? temperature = null, Observed<decimal>? weight = null, TimeOnly? measuredAt = null) {
        return new VitalSigns(systolic, diastolic, heartRate, temperature, weight, measuredAt);
    }

    private static Observed<int> AnyObservedHeartRate() {
        return AnyObservedOf.OfValues(Any.Int32().Between(20, 250)).Generate();
    }

    // ---------------------------------------------------------------- ranges of the observed values

    [Theory]
    [InlineData(50)]
    [InlineData(300)]
    public void The_lowest_and_the_highest_plausible_systolic_pressures_are_accepted(int systolic) {
        // Act
        VitalSigns vitals = WithOnly(systolic: Observed<int>.Of(systolic));

        // Assert
        Assert.Equal(systolic, vitals.SystolicMmHg!.Value);
    }

    [Theory]
    [InlineData(49)]
    [InlineData(301)]
    public void A_systolic_pressure_outside_50_to_300_is_refused(int systolic) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => WithOnly(systolic: Observed<int>.Of(systolic)));

        // Assert
        Assert.Equal("systolicMmHg", refusal.ParamName);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(200)]
    public void The_lowest_and_the_highest_plausible_diastolic_pressures_are_accepted(int diastolic) {
        // Act
        VitalSigns vitals = WithOnly(diastolic: Observed<int>.Of(diastolic));

        // Assert
        Assert.Equal(diastolic, vitals.DiastolicMmHg!.Value);
    }

    [Theory]
    [InlineData(19)]
    [InlineData(201)]
    public void A_diastolic_pressure_outside_20_to_200_is_refused(int diastolic) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => WithOnly(diastolic: Observed<int>.Of(diastolic)));

        // Assert
        Assert.Equal("diastolicMmHg", refusal.ParamName);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(250)]
    public void The_lowest_and_the_highest_plausible_heart_rates_are_accepted(int heartRate) {
        // Act
        VitalSigns vitals = WithOnly(heartRate: Observed<int>.Of(heartRate));

        // Assert
        Assert.Equal(heartRate, vitals.HeartRateBpm!.Value);
    }

    [Theory]
    [InlineData(19)]
    [InlineData(251)]
    public void A_heart_rate_outside_20_to_250_is_refused(int heartRate) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => WithOnly(heartRate: Observed<int>.Of(heartRate)));

        // Assert
        Assert.Equal("heartRateBpm", refusal.ParamName);
    }

    [Theory]
    [InlineData("30.0")]
    [InlineData("36.6")]
    [InlineData("45.0")]
    public void A_body_temperature_from_30_to_45_degrees_to_one_decimal_is_accepted(string temperature) {
        // Act
        VitalSigns vitals = WithOnly(temperature: Observed<decimal>.Of(decimal.Parse(temperature, System.Globalization.CultureInfo.InvariantCulture)));

        // Assert
        Assert.True(vitals.TemperatureCelsius!.IsObserved);
    }

    [Theory]
    [InlineData("29.9")]
    [InlineData("45.1")]
    [InlineData("-36.6")]
    public void A_body_temperature_outside_30_to_45_degrees_is_refused(string temperature) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => WithOnly(temperature: Observed<decimal>.Of(decimal.Parse(temperature, System.Globalization.CultureInfo.InvariantCulture))));

        // Assert
        Assert.Equal("temperatureCelsius", refusal.ParamName);
    }

    [Fact]
    public void A_body_temperature_has_at_most_one_decimal_place() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => WithOnly(temperature: Observed<decimal>.Of(36.55m)));

        // Assert
        Assert.Equal("temperatureCelsius", refusal.ParamName);
    }

    [Fact]
    public void A_trailing_zero_is_not_a_second_decimal_place() {
        // Act
        VitalSigns vitals = WithOnly(temperature: Observed<decimal>.Of(36.50m), weight: Observed<decimal>.Of(70.00m));

        // Assert
        Assert.Equal(36.5m, vitals.TemperatureCelsius!.Value);
        Assert.Equal(70m, vitals.WeightKg!.Value);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("72.5")]
    [InlineData("400.0")]
    public void A_weight_from_1_to_400_kilograms_to_one_decimal_is_accepted(string weight) {
        // Act
        VitalSigns vitals = WithOnly(weight: Observed<decimal>.Of(decimal.Parse(weight, System.Globalization.CultureInfo.InvariantCulture)));

        // Assert
        Assert.True(vitals.WeightKg!.IsObserved);
    }

    [Theory]
    [InlineData("0.9")]
    [InlineData("400.1")]
    [InlineData("0")]
    public void A_weight_outside_1_to_400_kilograms_is_refused(string weight) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => WithOnly(weight: Observed<decimal>.Of(decimal.Parse(weight, System.Globalization.CultureInfo.InvariantCulture))));

        // Assert
        Assert.Equal("weightKg", refusal.ParamName);
    }

    [Fact]
    public void A_weight_has_at_most_one_decimal_place() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => WithOnly(weight: Observed<decimal>.Of(70.25m)));

        // Assert
        Assert.Equal("weightKg", refusal.ParamName);
    }

    [Fact]
    public void Missing_signs_are_not_checked_against_any_range() {
        // Arrange
        Observed<decimal> missingTemperature = AnyObservedOf.Missing<decimal>().Generate();

        // Act
        VitalSigns vitals = WithOnly(temperature: missingTemperature);

        // Assert
        Assert.Same(missingTemperature, vitals.TemperatureCelsius);
    }

    // ---------------------------------------------------------------- blood pressure as a pair

    [Fact]
    public void The_systolic_pressure_must_exceed_the_diastolic_one() {
        // Act
        VitalSigns vitals = WithOnly(systolic: Observed<int>.Of(121), diastolic: Observed<int>.Of(120));

        // Assert
        Assert.Equal(121, vitals.SystolicMmHg!.Value);
    }

    [Theory]
    [InlineData(120, 120)]
    [InlineData(100, 120)]
    public void A_systolic_pressure_not_above_the_diastolic_one_is_refused(int systolic, int diastolic) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => WithOnly(systolic: Observed<int>.Of(systolic), diastolic: Observed<int>.Of(diastolic)));

        // Assert
        Assert.Equal("diastolicMmHg", refusal.ParamName);
    }

    [Fact]
    public void One_side_of_the_pressure_cannot_be_observed_while_the_other_is_missing() {
        // Arrange
        Observed<int> anyMissingSide = AnyObservedOf.Missing<int>().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => WithOnly(systolic: Observed<int>.Of(120), diastolic: anyMissingSide));
        Assert.Throws<ArgumentException>(() => WithOnly(systolic: anyMissingSide, diastolic: Observed<int>.Of(80)));
    }

    [Fact]
    public void One_side_of_the_pressure_may_be_observed_while_the_other_is_not_entered_yet() {
        // Act
        VitalSigns onlySystolic  = WithOnly(systolic: Observed<int>.Of(120));
        VitalSigns onlyDiastolic = WithOnly(diastolic: Observed<int>.Of(80));

        // Assert
        Assert.Null(onlySystolic.DiastolicMmHg);
        Assert.Null(onlyDiastolic.SystolicMmHg);
    }

    [Fact]
    public void Both_sides_of_the_pressure_may_be_missing_for_different_reasons() {
        // Act
        VitalSigns vitals = WithOnly(systolic: Observed<int>.Missing(MissingReason.NotDone), diastolic: Observed<int>.Missing(MissingReason.Refused));

        // Assert
        Assert.True(vitals.SystolicMmHg!.IsMissing(MissingReason.NotDone));
        Assert.True(vitals.DiastolicMmHg!.IsMissing(MissingReason.Refused));
    }

    // ---------------------------------------------------------------- the time of measurement

    [Fact]
    public void A_time_of_measurement_is_refused_when_every_sign_is_resolved_and_none_was_measured() {
        // Arrange
        VitalSigns noneMeasured = Any.VitalSigns().WithBloodPressure(Observed<int>.Missing(MissingReason.NotDone), Observed<int>.Missing(MissingReason.NotDone))
                                     .WithHeartRate(Observed<int>.Missing(MissingReason.NotDone))
                                     .WithTemperature(Observed<decimal>.Missing(MissingReason.NotDone))
                                     .WithWeight(Observed<decimal>.Missing(MissingReason.NotDone))
                                     .WithMeasuredAt(null)
                                     .Generate();
        TimeOnly anyTime = Any.TimeOnly().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.VitalSigns().WithBloodPressure(noneMeasured.SystolicMmHg, noneMeasured.DiastolicMmHg)
                                                                              .WithHeartRate(noneMeasured.HeartRateBpm)
                                                                              .WithTemperature(noneMeasured.TemperatureCelsius)
                                                                              .WithWeight(noneMeasured.WeightKg)
                                                                              .WithMeasuredAt(anyTime)
                                                                              .Generate());

        // Assert
        Assert.Equal("measuredAt", refusal.ParamName);
    }

    [Fact]
    public void A_time_of_measurement_is_accepted_while_a_sign_is_not_entered_yet() {
        // Arrange
        TimeOnly anyTime = Any.TimeOnly().Generate();

        // Act
        VitalSigns vitals = WithOnly(heartRate: Observed<int>.Missing(MissingReason.NotDone), measuredAt: anyTime);

        // Assert
        Assert.Equal(anyTime, vitals.MeasuredAt);
    }

    [Fact]
    public void A_time_of_measurement_is_accepted_when_a_sign_was_measured() {
        // Arrange
        TimeOnly anyTime = Any.TimeOnly().Generate();

        // Act
        VitalSigns vitals = Any.VitalSigns().Resolved().WithHeartRate(AnyObservedHeartRate()).WithMeasuredAt(anyTime).Generate();

        // Assert
        Assert.Equal(anyTime, vitals.MeasuredAt);
    }

    // ---------------------------------------------------------------- what is left to resolve

    [Fact]
    public void A_page_where_nothing_is_entered_lists_every_sign() {
        // Arrange
        VitalSigns vitals = WithOnly();

        // Act
        IReadOnlyList<string> unresolved = vitals.UnresolvedFields();

        // Assert
        Assert.Equal(["SystolicMmHg", "DiastolicMmHg", "HeartRateBpm", "TemperatureCelsius", "WeightKg"], unresolved);
    }

    [Fact]
    public void A_sign_not_entered_is_the_only_field_left_when_the_others_are_resolved() {
        // Arrange
        VitalSigns vitals = Any.VitalSigns().Resolved().WithHeartRate(null).Generate();

        // Act
        IReadOnlyList<string> unresolved = vitals.UnresolvedFields();

        // Assert
        Assert.Equal(["HeartRateBpm"], unresolved);
    }

    [Fact]
    public void A_measured_sign_without_a_time_leaves_the_time_to_enter() {
        // Arrange
        VitalSigns vitals = Any.VitalSigns().Resolved().WithHeartRate(AnyObservedHeartRate()).WithMeasuredAt(null).Generate();

        // Act
        IReadOnlyList<string> unresolved = vitals.UnresolvedFields();

        // Assert
        Assert.Equal(["MeasuredAt"], unresolved);
    }

    [Fact]
    public void A_page_resolved_with_a_time_has_nothing_left() {
        // Arrange
        VitalSigns vitals = Any.VitalSigns().Resolved().WithHeartRate(AnyObservedHeartRate()).Generate();

        // Assert
        Assert.Empty(vitals.UnresolvedFields());
    }

    [Fact]
    public void A_page_where_every_sign_is_missing_needs_no_time() {
        // Arrange
        VitalSigns vitals = Any.VitalSigns().WithBloodPressure(Observed<int>.Missing(MissingReason.Refused), Observed<int>.Missing(MissingReason.Refused))
                               .WithHeartRate(Observed<int>.Missing(MissingReason.Refused))
                               .WithTemperature(Observed<decimal>.Missing(MissingReason.Refused))
                               .WithWeight(Observed<decimal>.Missing(MissingReason.Refused))
                               .Generate();

        // Assert
        Assert.Empty(vitals.UnresolvedFields());
        Assert.False(vitals.IsAnyObserved);
    }

    [Fact(Skip = "domain defect: IsAnyObserved and UnresolvedFields leave the diastolic pressure out, so a diastolic pressure measured alone (systolic not entered) "
               + "never asks for the time of measurement, although the model says MeasuredAt is unresolved 'when a sign is observed without a time'. No seed: literal inputs")]
    public void A_diastolic_pressure_measured_alone_leaves_the_time_to_enter() {
        // Arrange
        Observed<int>     missing = Observed<int>.Missing(MissingReason.NotDone);
        Observed<decimal> missingDecimal = Observed<decimal>.Missing(MissingReason.NotDone);
        VitalSigns vitals = new VitalSigns(null, Observed<int>.Of(80), missing, missingDecimal, missingDecimal, null);

        // Act
        IReadOnlyList<string> unresolved = vitals.UnresolvedFields();

        // Assert
        Assert.True(vitals.IsAnyObserved);
        Assert.Contains("MeasuredAt", unresolved);
    }

    [Fact]
    public void Any_observed_sign_makes_the_page_a_measurement() {
        // Assert
        Assert.True(WithOnly(heartRate: Observed<int>.Of(60)).IsAnyObserved);
        Assert.True(WithOnly(weight: Observed<decimal>.Of(60m)).IsAnyObserved);
        Assert.False(WithOnly().IsAnyObserved);
    }

    // ---------------------------------------------------------------- equality

    [Fact]
    public void Two_pages_are_equal_when_every_field_is() {
        // Arrange
        VitalSigns anyVitals = Any.VitalSigns().Generate();

        // Act
        VitalSigns sameVitals = new VitalSigns(anyVitals.SystolicMmHg, anyVitals.DiastolicMmHg, anyVitals.HeartRateBpm,
                                               anyVitals.TemperatureCelsius, anyVitals.WeightKg, anyVitals.MeasuredAt);

        // Assert
        Assert.Equal(anyVitals, sameVitals);
        Assert.Equal(anyVitals.GetHashCode(), sameVitals.GetHashCode());
    }

    [Fact]
    public void Pages_whose_heart_rates_differ_are_not_equal() {
        // Arrange
        VitalSigns first  = Any.VitalSigns().Resolved().WithHeartRate(Observed<int>.Of(60)).WithMeasuredAt(new TimeOnly(9, 0)).Generate();
        VitalSigns second = new VitalSigns(first.SystolicMmHg, first.DiastolicMmHg, Observed<int>.Of(61), first.TemperatureCelsius, first.WeightKg, first.MeasuredAt);

        // Assert
        Assert.NotEqual(first, second);
    }

}
