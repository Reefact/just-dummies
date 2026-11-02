namespace NetworkAcl.Domain;

/// <summary>
/// A TCP flags test, as in <c>--tcp-flags mask expected</c>: the flags under <see cref="Mask"/> must equal
/// <see cref="Expected"/>; flags outside the mask are ignored. A match that no valid segment can satisfy is refused.
/// </summary>
public sealed class TcpFlagMatch : IEquatable<TcpFlagMatch>
{
    private const TcpFlags AllFlags = (TcpFlags)0xFF;

    public TcpFlagMatch(TcpFlags mask, TcpFlags expected)
    {
        if (mask == TcpFlags.None)
        {
            throw new ArgumentException("A flags test examines at least one flag.", nameof(mask));
        }

        if ((expected & ~mask) != TcpFlags.None)
        {
            throw new ArgumentException($"Expected flags {expected} are not all under the mask {mask}.", nameof(expected));
        }

        if (HasForbiddenPair(expected))
        {
            throw new ArgumentException($"No valid segment carries {expected}.", nameof(expected));
        }

        if (mask == AllFlags && expected == TcpFlags.None)
        {
            throw new ArgumentException("A segment with no flag at all is invalid; this test would never match.", nameof(expected));
        }

        Mask = mask;
        Expected = expected;
    }

    /// <summary>A connection opening: SYN set, ACK clear.</summary>
    public static TcpFlagMatch NewConnection { get; } = new(TcpFlags.Syn | TcpFlags.Ack, TcpFlags.Syn);

    /// <summary>Any segment of an established connection: ACK set.</summary>
    public static TcpFlagMatch Established { get; } = new(TcpFlags.Ack, TcpFlags.Ack);

    public TcpFlags Mask { get; }

    public TcpFlags Expected { get; }

    /// <summary>
    /// Whether a segment may carry these flags: at least one flag, and none of the contradictory pairs
    /// SYN+FIN, SYN+RST, FIN+RST.
    /// </summary>
    public static bool IsValidCombination(TcpFlags flags) => flags != TcpFlags.None && !HasForbiddenPair(flags);

    public bool Matches(TcpFlags flags) => (flags & Mask) == Expected;

    /// <summary>True when this test accepts every flag set <paramref name="other"/> accepts: it examines no more flags, and agrees on those.</summary>
    public bool Covers(TcpFlagMatch other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return (Mask & ~other.Mask) == TcpFlags.None && (other.Expected & Mask) == Expected;
    }

    /// <summary>True when some flag set could satisfy both tests (they agree on every flag both examine).</summary>
    public bool Overlaps(TcpFlagMatch other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return ((Expected ^ other.Expected) & Mask & other.Mask) == TcpFlags.None;
    }

    private static bool HasForbiddenPair(TcpFlags flags) =>
        Both(flags, TcpFlags.Syn, TcpFlags.Fin) || Both(flags, TcpFlags.Syn, TcpFlags.Rst) || Both(flags, TcpFlags.Fin, TcpFlags.Rst);

    private static bool Both(TcpFlags flags, TcpFlags a, TcpFlags b) => (flags & (a | b)) == (a | b);

    public bool Equals(TcpFlagMatch? other) => other is not null && other.Mask == Mask && other.Expected == Expected;

    public override bool Equals(object? obj) => Equals(obj as TcpFlagMatch);

    public override int GetHashCode() => HashCode.Combine(Mask, Expected);

    public override string ToString() => $"{Mask}/{Expected}";
}
