using JustDummies;

namespace Clinical.Domain;

// Written by hand, beside the scaffolded file.
public sealed partial class AnyPrescription {

    /// <summary>A prescription that went through its life: signed, then dispensed on its issue date. A claim only accepts that one.</summary>
    public IAny<Prescription> Dispensed() {
        return this.As(prescription => {
            prescription.Sign();
            prescription.Dispense(prescription.IssuedOn);

            return prescription;
        });
    }

}
