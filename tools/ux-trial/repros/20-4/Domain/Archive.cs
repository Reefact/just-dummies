namespace Repro20Four;

public sealed class Archive {
    public Archive(IReadOnlyCollection<double> values) {
        if (values.Count > 2_000_000) {
            throw new ArgumentException("too many", nameof(values));
        }
        Values = values;
    }
    public IReadOnlyCollection<double> Values { get; }
}

public sealed class Archive900k {
    public Archive900k(IReadOnlyCollection<double> values) {
        if (values.Count > 900_000) {
            throw new ArgumentException("too many", nameof(values));
        }
        Values = values;
    }
    public IReadOnlyCollection<double> Values { get; }
}
