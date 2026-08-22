#region Usings declarations

using System.Diagnostics.CodeAnalysis;

#endregion

namespace JustDummies;

/// <summary>Uniform sampling helpers shared by the generators.</summary>
internal static class RandomSampling {

    /// <summary>
    ///     Draws a uniform value in the inclusive range [<paramref name="minInclusive" />,
    ///     <paramref name="maxInclusive" />]. Unlike <see cref="Random.Next(int, int)" /> the upper bound is reachable,
    ///     which matters for full-range and boundary draws. The draw maps 8 random bytes onto the range size; the
    ///     modulo bias is at most 2^-32 for the ranges an <see cref="int" /> can express — irrelevant for arbitrary
    ///     test values. Deliberately NOT named NextInt64: on the net8.0 leg the framework's own
    ///     <c>Random.NextInt64(long, long)</c> instance method — whose upper bound is EXCLUSIVE — would win
    ///     overload resolution over a same-named extension and silently change the semantics.
    /// </summary>
    [SuppressMessage(SonarRule.S125.Category, SonarRule.S125.Id, Justification = SuppressionJustification.S125.ProseNotDisabledCode)]
    internal static long NextInt64Inclusive(this SeededRandom random, long minInclusive, long maxInclusive) {
        if (random is null) { throw new ArgumentNullException(nameof(random)); }
        if (minInclusive > maxInclusive) { throw new ArgumentOutOfRangeException(nameof(maxInclusive), "The maximum must be greater than or equal to the minimum."); }

        ulong rangeSize = (ulong)(maxInclusive - minInclusive) + 1UL;
        ulong draw      = random.NextUInt64();

        // rangeSize is 0 only when the range spans the full ulong width, which int-derived bounds never do;
        // guard anyway so the helper stays correct if reused with wider bounds.
        if (rangeSize == 0UL) { return unchecked((long)draw); }

        return minInclusive + (long)(draw % rangeSize);
    }

    /// <summary>Draws a uniform <see cref="int" /> in the inclusive range — see <see cref="NextInt64Inclusive" />.</summary>
    internal static int NextInt32Inclusive(this SeededRandom random, int minInclusive, int maxInclusive) {
        if (random is null) { throw new ArgumentNullException(nameof(random)); }

        return (int)random.NextInt64Inclusive(minInclusive, maxInclusive);
    }

    /// <summary>Draws 8 random bytes as a <see cref="ulong" /> — the raw material of the ordinal sampling.</summary>
    internal static ulong NextUInt64(this SeededRandom random) {
        if (random is null) { throw new ArgumentNullException(nameof(random)); }

        byte[] bytes = new byte[sizeof(ulong)];
        random.NextBytes(bytes);

        return BitConverter.ToUInt64(bytes, 0);
    }


}
