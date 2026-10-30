namespace Insurance.Domain;

public sealed class Claim {

    public Claim(Guid id, DateOnly occurredOn, decimal damage, Responsibility responsibility) {
        if (id == Guid.Empty) {
            throw new ArgumentException("A claim has an identifier.", nameof(id));
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(damage);

        Id             = id;
        OccurredOn     = occurredOn;
        Damage         = damage;
        Responsibility = responsibility;
        InsuredShare = responsibility switch {
            Responsibility.None    => 1.00m,
            Responsibility.Partial => 0.50m,
            Responsibility.Full    => 0.00m,
            _                      => throw new ArgumentOutOfRangeException(nameof(responsibility), responsibility, "Unknown responsibility.")
        };
    }

    public Guid           Id             { get; }
    public DateOnly       OccurredOn     { get; }
    public decimal        Damage         { get; }
    public Responsibility Responsibility { get; }

    /// <summary>Share of the damage a third party's insurer reimburses under the IRSA convention, as seen from the policyholder.</summary>
    public decimal InsuredShare { get; }

}
