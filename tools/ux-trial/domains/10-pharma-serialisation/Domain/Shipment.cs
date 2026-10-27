using System.Text.RegularExpressions;

namespace Serialisation.Domain;

/// <summary>A wholesaler shipment: a set of serialised packs that must never repeat a serial.</summary>
public sealed class Shipment {

    public Shipment(string reference, IReadOnlyList<SerialNumber> serials, DateOnly shippedOn) {
        if (reference is null || !Regex.IsMatch(reference, @"^SHP-\d{8}$")) {
            throw new ArgumentException("A shipment reference reads SHP- followed by 8 digits.", nameof(reference));
        }
        ArgumentNullException.ThrowIfNull(serials);
        if (serials.Count == 0) {
            throw new ArgumentException("A shipment carries at least one pack.", nameof(serials));
        }
        if (serials.Distinct().Count() != serials.Count) {
            throw new ArgumentException("A shipment cannot carry the same serial twice.", nameof(serials));
        }

        Reference = reference;
        Serials   = serials;
        ShippedOn = shippedOn;
    }

    public string                      Reference { get; }
    public IReadOnlyList<SerialNumber> Serials   { get; }
    public DateOnly                    ShippedOn { get; }

    /// <summary>The serials in this shipment that the batch does not know or can no longer dispense.</summary>
    public IReadOnlyList<SerialNumber> SuspiciousIn(Batch batch) {
        return Serials.Where(serial => batch.Verify(serial, ShippedOn) is VerificationOutcome.UnknownSerial or VerificationOutcome.AlreadyDecommissioned).ToList();
    }

}
