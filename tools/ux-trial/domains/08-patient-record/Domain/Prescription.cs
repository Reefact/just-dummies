using System.Text.RegularExpressions;

namespace Clinical.Domain;

/// <summary>A prescription: who prescribes what to whom, and where it stands.</summary>
public sealed class Prescription {

    public const int MaxLines = 10;

    private static readonly Regex RppsNumber = new Regex(@"^\d{11}$");

    private readonly List<PrescriptionLine> _lines;

    public Prescription(Guid id, Patient patient, string prescriberRpps, DateOnly issuedOn, IReadOnlyList<PrescriptionLine> lines) {
        if (id == Guid.Empty) {
            throw new ArgumentException("A prescription needs an identifier.", nameof(id));
        }
        ArgumentNullException.ThrowIfNull(patient);
        ArgumentNullException.ThrowIfNull(prescriberRpps);
        if (!RppsNumber.IsMatch(prescriberRpps)) {
            throw new ArgumentException("An RPPS number is eleven digits.", nameof(prescriberRpps));
        }
        if (issuedOn < patient.BirthDate) {
            throw new ArgumentException("A prescription cannot predate its patient.", nameof(issuedOn));
        }
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count == 0 || lines.Count > MaxLines) {
            throw new ArgumentException($"A prescription has between 1 and {MaxLines} lines.", nameof(lines));
        }

        Id             = id;
        Patient        = patient;
        PrescriberRpps = prescriberRpps;
        IssuedOn       = issuedOn;
        _lines         = lines.ToList();
        Status         = PrescriptionStatus.Draft;
    }

    public Guid Id { get; }

    public Patient Patient { get; }

    public string PrescriberRpps { get; }

    public DateOnly IssuedOn { get; }

    public IReadOnlyList<PrescriptionLine> Lines => _lines;

    public PrescriptionStatus Status { get; private set; }

    /// <summary>A French prescription must be presented at the pharmacy within three months of issue.</summary>
    public DateOnly LastDispensingDay => IssuedOn.AddMonths(3);

    /// <summary>The day the longest treatment of the prescription ends, if started on the issue date.</summary>
    public DateOnly TreatmentEndsOn => Lines.Select(line => line.Duration.LastDayFrom(IssuedOn)).Max();

    public bool RequiresParentalConsent => Patient.IsMinorOn(IssuedOn) && Lines.Any(line => line.Route == RouteOfAdministration.Intravenous);

    public void Sign() {
        if (Status != PrescriptionStatus.Draft) {
            throw new InvalidOperationException($"Only a draft can be signed; this prescription is {Status}.");
        }
        Status = PrescriptionStatus.Signed;
    }

    public void Dispense(DateOnly on) {
        if (Status != PrescriptionStatus.Signed) {
            throw new InvalidOperationException($"Only a signed prescription can be dispensed; this one is {Status}.");
        }
        if (on < IssuedOn || on > LastDispensingDay) {
            throw new InvalidOperationException($"A prescription issued on {IssuedOn} can be dispensed until {LastDispensingDay}, not on {on}.");
        }
        Status = PrescriptionStatus.Dispensed;
    }

    public void Cancel() {
        if (Status == PrescriptionStatus.Dispensed) {
            throw new InvalidOperationException("A dispensed prescription cannot be cancelled.");
        }
        Status = PrescriptionStatus.Cancelled;
    }

}
