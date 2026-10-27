namespace Serialisation.Domain;

public enum VerificationOutcome {
    Authentic,
    UnknownSerial,
    Expired,
    AlreadyDecommissioned
}
