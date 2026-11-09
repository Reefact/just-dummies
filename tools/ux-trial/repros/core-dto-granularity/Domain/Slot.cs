namespace Granularity.Domain;

public sealed class Slot {
    public Slot(DateTimeOffset start) {
        if (start.TimeOfDay != TimeSpan.Zero) { throw new ArgumentException("A slot starts at local midnight.", nameof(start)); }
        Start = start;
    }

    public DateTimeOffset Start { get; }
}
