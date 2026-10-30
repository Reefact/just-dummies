namespace Jd030.Domain;

public sealed class Batch {
    public Batch(IReadOnlyList<string> tags) {
        Tags = tags;
    }

    public IReadOnlyList<string> Tags { get; }
}
