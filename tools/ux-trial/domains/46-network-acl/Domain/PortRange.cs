namespace NetworkAcl.Domain;

/// <summary>An inclusive range of transport ports, <c>low</c> to <c>high</c>, with low ≤ high.</summary>
public sealed class PortRange : IEquatable<PortRange>
{
    public PortRange(ushort low, ushort high)
    {
        if (low > high)
        {
            throw new ArgumentException($"Port range {low}-{high} is reversed: the low port must not exceed the high port.", nameof(low));
        }

        Low = low;
        High = high;
    }

    public static PortRange All { get; } = new(ushort.MinValue, ushort.MaxValue);

    public ushort Low { get; }

    public ushort High { get; }

    public int Count => High - Low + 1;

    public bool IsAll => Low == ushort.MinValue && High == ushort.MaxValue;

    public static PortRange Single(ushort port) => new(port, port);

    public bool Contains(ushort port) => port >= Low && port <= High;

    public bool Contains(PortRange other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return other.Low >= Low && other.High <= High;
    }

    public bool Overlaps(PortRange other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return other.Low <= High && Low <= other.High;
    }

    public bool Equals(PortRange? other) => other is not null && other.Low == Low && other.High == High;

    public override bool Equals(object? obj) => Equals(obj as PortRange);

    public override int GetHashCode() => HashCode.Combine(Low, High);

    public override string ToString() => IsAll ? "any" : Low == High ? $"{Low}" : $"{Low}-{High}";
}
