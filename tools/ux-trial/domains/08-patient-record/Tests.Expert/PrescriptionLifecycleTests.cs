using Clinical.Domain;
using Clinical.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Clinical.Tests.Expert;

public sealed class PrescriptionLifecycleTests {

    private static readonly DateOnly IssuedOn = Dates.On("2025-03-10");

    // A prescription issued on 2025-03-10 can be dispensed from that day to 2025-06-10, both included.

    // ---- Sign --------------------------------------------------------------------------------------

    [Fact]
    public void Signing_a_draft_makes_it_signed() {
        // Arrange
        Prescription draft = PrescriptionIn(PrescriptionStatus.Draft);

        // Act
        draft.Sign();

        // Assert
        Assert.Equal(PrescriptionStatus.Signed, draft.Status);
    }

    [Theory]
    [InlineData(PrescriptionStatus.Signed)]
    [InlineData(PrescriptionStatus.Dispensed)]
    [InlineData(PrescriptionStatus.Cancelled)]
    public void Only_a_draft_can_be_signed(PrescriptionStatus status) {
        // Arrange
        Prescription prescription = PrescriptionIn(status);

        // Act
        Assert.Throws<InvalidOperationException>(() => prescription.Sign());

        // Assert
        Assert.Equal(status, prescription.Status);
    }

    // ---- Dispense ----------------------------------------------------------------------------------

    [Theory]
    [InlineData("2025-03-10")]    // the issue date itself
    [InlineData("2025-04-22")]
    [InlineData("2025-06-10")]    // the last dispensing day itself
    public void A_signed_prescription_is_dispensed_from_its_issue_date_to_its_last_dispensing_day_both_included(string day) {
        // Arrange
        Prescription signed = PrescriptionIn(PrescriptionStatus.Signed);

        // Act
        signed.Dispense(Dates.On(day));

        // Assert
        Assert.Equal(PrescriptionStatus.Dispensed, signed.Status);
    }

    [Theory]
    [InlineData("2025-03-09")]    // the day before the issue date
    [InlineData("2025-06-11")]    // the day after the last dispensing day
    [InlineData("1999-01-01")]
    [InlineData("2030-01-01")]
    public void A_signed_prescription_dispensed_outside_its_window_is_refused_as_an_invalid_operation_and_stays_signed(string day) {
        // Arrange
        Prescription signed = PrescriptionIn(PrescriptionStatus.Signed);

        // Act
        Assert.Throws<InvalidOperationException>(() => signed.Dispense(Dates.On(day)));

        // Assert
        Assert.Equal(PrescriptionStatus.Signed, signed.Status);
    }

    [Theory]
    [InlineData(PrescriptionStatus.Draft)]
    [InlineData(PrescriptionStatus.Dispensed)]
    [InlineData(PrescriptionStatus.Cancelled)]
    public void Only_a_signed_prescription_can_be_dispensed(PrescriptionStatus status) {
        // Arrange
        Prescription prescription = PrescriptionIn(status);

        // Act
        Assert.Throws<InvalidOperationException>(() => prescription.Dispense(IssuedOn));

        // Assert
        Assert.Equal(status, prescription.Status);
    }

    [Fact]
    public void The_dispensing_window_follows_the_clamped_month_end() {
        // Arrange
        Prescription signed = Any.Prescription().WithIssuedOn(Dates.On("2025-01-31")).Generate();
        signed.Sign();

        // Act
        Assert.Throws<InvalidOperationException>(() => signed.Dispense(Dates.On("2025-05-01")));
        signed.Dispense(Dates.On("2025-04-30"));

        // Assert
        Assert.Equal(PrescriptionStatus.Dispensed, signed.Status);
    }

    // ---- Cancel ------------------------------------------------------------------------------------

    [Theory]
    [InlineData(PrescriptionStatus.Draft)]
    [InlineData(PrescriptionStatus.Signed)]
    [InlineData(PrescriptionStatus.Cancelled)]
    public void A_prescription_that_is_not_dispensed_can_be_cancelled(PrescriptionStatus status) {
        // Arrange
        Prescription prescription = PrescriptionIn(status);

        // Act
        prescription.Cancel();

        // Assert
        Assert.Equal(PrescriptionStatus.Cancelled, prescription.Status);
    }

    [Fact]
    public void A_dispensed_prescription_cannot_be_cancelled_and_stays_dispensed() {
        // Arrange
        Prescription dispensed = PrescriptionIn(PrescriptionStatus.Dispensed);

        // Act
        Assert.Throws<InvalidOperationException>(() => dispensed.Cancel());

        // Assert
        Assert.Equal(PrescriptionStatus.Dispensed, dispensed.Status);
    }

    [Fact]
    public void A_cancelled_prescription_can_never_be_signed_again() {
        // Arrange
        Prescription cancelled = PrescriptionIn(PrescriptionStatus.Cancelled);

        // Act
        Assert.Throws<InvalidOperationException>(() => cancelled.Sign());

        // Assert
        Assert.Equal(PrescriptionStatus.Cancelled, cancelled.Status);
    }

    // The prescription itself is a dummy here: only its status, and the window of 2025-03-10, matter.
    private static Prescription PrescriptionIn(PrescriptionStatus status) {
        Prescription prescription = Any.Prescription().WithIssuedOn(IssuedOn).Generate();
        switch (status) {
            case PrescriptionStatus.Draft:
                break;
            case PrescriptionStatus.Signed:
                prescription.Sign();
                break;
            case PrescriptionStatus.Dispensed:
                prescription.Sign();
                prescription.Dispense(IssuedOn);
                break;
            case PrescriptionStatus.Cancelled:
                prescription.Cancel();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }

        return prescription;
    }

}
