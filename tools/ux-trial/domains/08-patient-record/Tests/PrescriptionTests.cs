using Clinical.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace Clinical.Tests;

[Reproducible]
public sealed class PrescriptionTests {

    private static readonly DateOnly EndOfJanuary = new DateOnly(2026, 1, 31);

    [Fact]
    public void A_prescription_issued_on_31_January_can_be_dispensed_until_30_April() {
        // Arrange
        Prescription anyPrescription = Any.Prescription().WithIssuedOn(EndOfJanuary).Generate();

        // Act
        DateOnly lastDay = anyPrescription.LastDispensingDay;

        // Assert
        Assert.Equal(new DateOnly(2026, 4, 30), lastDay);
    }

    [Fact]
    public void A_signed_prescription_presented_on_its_last_dispensing_day_is_dispensed() {
        // Arrange
        Prescription anyPrescription = Any.Prescription().WithIssuedOn(EndOfJanuary).Generate();
        anyPrescription.Sign();

        // Act
        anyPrescription.Dispense(new DateOnly(2026, 4, 30));

        // Assert
        Assert.Equal(PrescriptionStatus.Dispensed, anyPrescription.Status);
    }

    [Fact]
    public void A_signed_prescription_presented_the_day_after_its_last_dispensing_day_is_refused() {
        // Arrange
        Prescription anyPrescription = Any.Prescription().WithIssuedOn(EndOfJanuary).Generate();
        anyPrescription.Sign();

        // Act
        Action dispensing = () => anyPrescription.Dispense(new DateOnly(2026, 5, 1));

        // Assert
        Assert.Throws<InvalidOperationException>(dispensing);
        Assert.Equal(PrescriptionStatus.Signed, anyPrescription.Status);
    }

    [Fact]
    public void A_draft_cannot_be_dispensed() {
        // Arrange
        Prescription anyDraft          = Any.Prescription().Generate();
        DateOnly     anyDayInTheWindow = Any.DateOnly().Between(anyDraft.IssuedOn, anyDraft.LastDispensingDay).Generate();

        // Act
        Action dispensing = () => anyDraft.Dispense(anyDayInTheWindow);

        // Assert
        Assert.Throws<InvalidOperationException>(dispensing);
    }

    [Fact]
    public void A_dispensed_prescription_cannot_be_cancelled() {
        // Arrange
        Prescription anyPrescription = Any.Prescription().Generate();
        anyPrescription.Sign();
        anyPrescription.Dispense(anyPrescription.IssuedOn);

        // Act
        Action cancelling = () => anyPrescription.Cancel();

        // Assert
        Assert.Throws<InvalidOperationException>(cancelling);
    }

    [Fact]
    public void A_minor_with_an_intravenous_line_needs_parental_consent() {
        // Arrange
        Prescription anyPrescriptionForAChildWithAnIvLine = Any.Prescription()
            .WithPatient(p => p.WithBirthDate(new DateOnly(2015, 6, 1)))
            .WithIssuedOn(new DateOnly(2026, 3, 1))
            .WithLines(l => l.ContainingAny(Any.PrescriptionLine().WithRoute(RouteOfAdministration.Intravenous)))
            .Generate();

        // Act
        bool requiresConsent = anyPrescriptionForAChildWithAnIvLine.RequiresParentalConsent;

        // Assert
        Assert.True(requiresConsent);
    }

    [Fact]
    public void An_adult_never_needs_parental_consent_even_with_an_intravenous_line() {
        // Arrange
        Prescription anyPrescriptionForAnAdultWithAnIvLine = Any.Prescription()
            .WithPatient(p => p.WithBirthDate(b => b.BeforeOrEqualTo(new DateOnly(2008, 3, 1))))
            .WithIssuedOn(new DateOnly(2026, 3, 1))
            .WithLines(l => l.ContainingAny(Any.PrescriptionLine().WithRoute(RouteOfAdministration.Intravenous)))
            .Generate();

        // Act
        bool requiresConsent = anyPrescriptionForAnAdultWithAnIvLine.RequiresParentalConsent;

        // Assert
        Assert.False(requiresConsent);
    }

    [Fact]
    public void The_treatment_ends_with_the_longest_line() {
        // Arrange
        PrescriptionLine anyFiveDayLine = Any.PrescriptionLine().WithDuration(new TreatmentDuration(5)).Generate();
        PrescriptionLine anyTenDayLine  = Any.PrescriptionLine().WithDuration(new TreatmentDuration(10)).Generate();
        Prescription anyPrescription = Any.Prescription()
            .WithIssuedOn(new DateOnly(2026, 3, 1))
            .WithLines([anyFiveDayLine, anyTenDayLine])
            .Generate();

        // Act
        DateOnly endsOn = anyPrescription.TreatmentEndsOn;

        // Assert
        Assert.Equal(new DateOnly(2026, 3, 10), endsOn);
    }

}
