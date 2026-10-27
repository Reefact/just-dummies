using JustDummies;
using Laboratory.Domain;
using Xunit;

namespace Laboratory.Tests;

public sealed class LabResultTests {

    private static readonly MeasurementUnit MillimolesPerLitre = new("mmol/L");
    private static readonly ReferenceRange  Potassium          = new(3.5, 5.1, MillimolesPerLitre, criticalLow: 2.5, criticalHigh: 6.5);

    [Fact]
    public void A_result_above_its_range_is_out_of_range_without_calling_for_a_phone_call() {
        // Arrange
        LabResult anyHighPotassium = Any.LabResult()
                                        .WithRange(Potassium)
                                        .WithMeasurement(new Measurement(5.8, MillimolesPerLitre))
                                        .Generate();

        // Act
        bool outOfRange = anyHighPotassium.IsOutOfRange;
        bool phoneCall  = anyHighPotassium.RequiresPhoneCall;

        // Assert
        Assert.True(outOfRange);
        Assert.False(phoneCall);
    }

    [Fact]
    public void A_critically_low_result_calls_for_a_phone_call() {
        // Arrange
        LabResult anyCriticalPotassium = Any.LabResult()
                                            .WithRange(Potassium)
                                            .WithMeasurement(new Measurement(2.1, MillimolesPerLitre))
                                            .Generate();

        // Act
        bool phoneCall = anyCriticalPotassium.RequiresPhoneCall;

        // Assert
        Assert.True(phoneCall);
    }

    [Fact]
    public void A_new_result_is_preliminary_until_released() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();

        // Act
        anyResult.Release();

        // Assert
        Assert.Equal(ResultStatus.Final, anyResult.Status);
    }

    [Fact]
    public void A_final_result_cannot_be_released_again() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();
        anyResult.Release();

        // Act
        Action releasingAgain = anyResult.Release;

        // Assert
        Assert.Throws<InvalidOperationException>(releasingAgain);
    }

    [Fact]
    public void Amending_a_final_result_replaces_its_measurement() {
        // Arrange
        LabResult anyFinalPotassium = Any.LabResult().WithRange(Potassium).WithMeasurement(m => m.WithUnit(MillimolesPerLitre)).Generate();
        anyFinalPotassium.Release();
        Measurement corrected    = new(4.2, MillimolesPerLitre);
        TimeSpan    anyDelay     = Any.TimeSpan().Positive().LessThanOrEqualTo(TimeSpan.FromDays(7)).Generate();

        // Act
        anyFinalPotassium.Amend(corrected, anyFinalPotassium.ReportedAt + anyDelay);

        // Assert
        Assert.Equal(ResultStatus.Amended, anyFinalPotassium.Status);
        Assert.Same(corrected, anyFinalPotassium.Measurement);
    }

    [Fact]
    public void An_amendment_in_another_unit_is_refused() {
        // Arrange
        LabResult anyFinalPotassium = Any.LabResult().WithRange(Potassium).WithMeasurement(m => m.WithUnit(MillimolesPerLitre)).Generate();
        anyFinalPotassium.Release();
        MeasurementUnit anyOtherUnit = new AnyMeasurementUnit().WithSymbol(s => s.DifferentFrom(MillimolesPerLitre.Symbol)).Generate();
        Measurement     anyCorrected = new AnyMeasurement().WithUnit(anyOtherUnit).Generate();

        // Act
        Action amending = () => anyFinalPotassium.Amend(anyCorrected, anyFinalPotassium.ReportedAt.AddHours(1));

        // Assert
        Assert.Throws<ArgumentException>(amending);
    }

    [Fact]
    public void A_result_cannot_be_reported_before_its_specimen_was_received() {
        // Arrange
        Specimen    anySpecimen    = Arbitrary.Specimen().Generate();
        Measurement anyMeasurement = new AnyMeasurement().WithUnit(MillimolesPerLitre).Generate();
        AnalyteCode anyAnalyte     = new AnyAnalyteCode().Generate();

        // Act
        Action reporting = () => new LabResult(anyAnalyte, anySpecimen, anyMeasurement, Potassium, anySpecimen.ReceivedAt.AddTicks(-1));

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(reporting);
        Assert.Equal("reportedAt", refusal.ParamName);
    }

    [Fact]
    public void A_measurement_on_a_diluted_sample_is_scaled_back_to_the_undiluted_concentration() {
        // Arrange
        MeasurementUnit nanogramsPerMillilitre = new("ng/mL");
        Measurement     measured               = new(12.5, nanogramsPerMillilitre);

        // Act
        Measurement undiluted = measured.Undiluted(new DilutionFactor(4m));

        // Assert
        Assert.Equal(50.0, undiluted.Value);
        Assert.Same(nanogramsPerMillilitre, undiluted.Unit);
    }

}
