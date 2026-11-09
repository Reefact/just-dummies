namespace Repro.Domain;

public sealed class Session {

    public Session(DateTime opensAt, DateTime closesAt) {
        if (opensAt >= closesAt) {
            throw new ArgumentException("A session opens before it closes.", nameof(closesAt));
        }
        OpensAt  = opensAt;
        ClosesAt = closesAt;
    }

    public DateTime OpensAt { get; }

    public DateTime ClosesAt { get; }

}
