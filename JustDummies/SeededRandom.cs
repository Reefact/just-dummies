#region Usings declarations

using System.Diagnostics.CodeAnalysis;

#endregion

namespace JustDummies;

/// <summary>
///     A pseudo-random generator that remembers the seed it was created from, and the <b>only</b> door to it: the
///     underlying <see cref="Random" /> is never handed out, so every draw goes through this type and is serialized
///     on its own lock.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Random" /> is not thread-safe, and a source reaches several threads by two ordinary routes: the
///         ambient state flows with the execution context into every task a test spawns, and an
///         <see cref="AnyContext" /> is shared by whoever holds it. Left unsynchronized, concurrent draws converge the
///         generator's two internal indices and it returns zero <b>for ever</b> — so every generator settles on the
///         minimum of its declared range (<c>0</c>, <c>""</c>, <see cref="Guid.Empty" />) and the source never
///         recovers. Silent, and exactly the values most likely to make an assertion pass for the wrong reason.
///     </para>
///     <para>
///         Keeping the <see cref="Random" /> private is what makes the guarantee hold: a synchronized subclass would
///         leak any member left un-overridden, whereas here a draw that bypasses the lock does not compile. An
///         uncontended lock leaves single-threaded sequences bit-identical, so a pinned seed replays exactly as
///         before, and the cost is immaterial on paths that are not hot loops.
///     </para>
///     <para>
///         What this does <b>not</b> buy is a value-level guarantee across threads: the lock is per primitive draw, so
///         two threads interleave inside a multi-draw generation (a string consumes one draw per character). Neither
///         the sequence nor the multiset of generated values is stable under parallelism — see
///         <see cref="Any.UseSeed(int)" /> for the per-work-item scope that is.
///     </para>
/// </remarks>
internal sealed class SeededRandom {

    #region Fields declarations

    private readonly object _gate = new();
    private readonly Random _random;
    private          long   _draws;

    #endregion

    [SuppressMessage(SonarRule.S2245.Category, SonarRule.S2245.Id, Justification = SuppressionJustification.S2245.PredictabilityIsTheContract)]
    internal SeededRandom(int seed) {
        Seed    = seed;
        _random = new Random(seed);
    }

    internal int Seed { get; }

    /// <summary>
    ///     How many primitive draws have been taken from this generator. Not a statistic: it is the quantity the
    ///     seed golden master pins alongside the values, because a change that leaves a factory's own output
    ///     identical while consuming one extra draw shifts every value produced after it in the same scope — and a
    ///     golden master watching only values stays green through exactly that (ADR-0049). Read under the same lock
    ///     the draws take, so a count read after a concurrent batch is the count that batch reached.
    /// </summary>
    internal long Draws {
        get { lock (_gate) { return _draws; } }
    }

    /// <summary>Draws a non-negative <see cref="int" /> below <paramref name="maxExclusive" />.</summary>
    internal int Next(int maxExclusive) {
        lock (_gate) {
            _draws++;

            return _random.Next(maxExclusive);
        }
    }

    /// <summary>Draws an <see cref="int" /> in the half-open range [<paramref name="minInclusive" />, <paramref name="maxExclusive" />).</summary>
    internal int Next(int minInclusive, int maxExclusive) {
        lock (_gate) {
            _draws++;

            return _random.Next(minInclusive, maxExclusive);
        }
    }

    /// <summary>Fills <paramref name="buffer" /> with random bytes.</summary>
    internal void NextBytes(byte[] buffer) {
        if (buffer is null) { throw new ArgumentNullException(nameof(buffer)); }

        lock (_gate) {
            _draws++;
            _random.NextBytes(buffer);
        }
    }

    /// <summary>Draws a <see cref="double" /> in the half-open range [0, 1).</summary>
    internal double NextDouble() {
        lock (_gate) {
            _draws++;

            return _random.NextDouble();
        }
    }

}
