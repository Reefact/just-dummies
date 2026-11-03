using Clinical.Domain;

namespace HealthcareClaims.Tests.Regular.Generators;

public sealed partial class AnyPrescription {

    /// <summary>A prescription that has been signed but not yet dispensed.</summary>
    public AnyPrescription Signed() => InStatus(PrescriptionStatus.Signed);

    /// <summary>A prescription the pharmacy has dispensed: the only kind a claim can be made on.</summary>
    public AnyPrescription Dispensed() => InStatus(PrescriptionStatus.Dispensed);

    public AnyPrescription Cancelled() => InStatus(PrescriptionStatus.Cancelled);

    private AnyPrescription InStatus(PrescriptionStatus status) {
        AnyPrescription copy = new AnyPrescription(this);

        copy._status = status;

        return copy;
    }

    private static Prescription Advance(Prescription prescription, PrescriptionStatus status) {
        switch (status) {
            case PrescriptionStatus.Signed:
                prescription.Sign();
                break;
            case PrescriptionStatus.Dispensed:
                prescription.Sign();
                prescription.Dispense(prescription.IssuedOn);
                break;
            case PrescriptionStatus.Cancelled:
                prescription.Cancel();
                break;
        }

        return prescription;
    }

}
