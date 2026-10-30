namespace Payments.Domain;

public enum TransferStatus {
    Draft,
    Submitted,
    Executed,
    Rejected
}

public enum TransferPriority {
    Normal,
    Instant
}
