namespace JustDummies;

/// <summary>
///     Order-preserving mappings between the discrete domains the generators expose and the unsigned 64-bit
///     <b>ordinal space</b> the shared interval engine works in. Every discrete type whose values fit 64 bits —
///     the integers, ticks-based time types, day numbers — maps onto <c>[0, 2^64-1]</c> so that one engine owns
///     bounds, exclusions, conflicts, and sampling for all of them.
/// </summary>
internal static class OrdinalMapping {

    private const ulong SignBit = 1UL << 63;

    /// <summary>Maps a signed 64-bit value to its ordinal: flips the sign bit, so ordering is preserved.</summary>
    internal static ulong FromInt64(long value) {
        return unchecked((ulong)value ^ SignBit);
    }

    /// <summary>Maps an ordinal back to the signed 64-bit value it came from.</summary>
    internal static long ToInt64(ulong ordinal) {
        return unchecked((long)(ordinal ^ SignBit));
    }

}
