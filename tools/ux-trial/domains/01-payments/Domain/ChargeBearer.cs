namespace Payments.Domain;

/// <summary>ISO 20022 charge bearer codes; SEPA mandates SLEV (following the service level).</summary>
public enum ChargeBearer {
    Debtor = 1,
    Creditor = 2,
    Shared = 3,
    FollowingServiceLevel = 4
}
