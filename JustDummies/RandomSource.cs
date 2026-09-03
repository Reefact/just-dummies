#region Usings declarations

using System.Diagnostics.CodeAnalysis;

#endregion

namespace JustDummies;

/// <summary>
///     The random context a generator draws from when it generates: a pseudo-random generator paired with the seed
///     that created it, so any failure can name the seed that replays the run. Generators hold a
///     <see cref="RandomSource" /> and resolve it at <see cref="IDummy{T}.Generate" /> time — never at construction
///     time — which is what lets a recipe built outside an <c>Dummy.Reproducibly(...)</c> scope generate
///     deterministically inside one.
/// </summary>
[SuppressMessage(SonarRule.S1694.Category, SonarRule.S1694.Id, Justification = SuppressionJustification.S1694.ClosedInternalHierarchyRoot)]
internal abstract class RandomSource {

    /// <summary>The seeded generator to draw from right now. Every draw goes through it, serialized on its own lock.</summary>
    internal abstract SeededRandom Current { get; }

    /// <summary>
    ///     The reproduction guidance to append to a generation-failure message, phrased for this kind of source: one
    ///     <b>sentence</b>, which <b>embeds</b> a replay <b>snippet</b> — the code the reader copies. The two words are
    ///     a whole and its part, and they are never interchangeable: guidance is the sentence, a snippet is the
    ///     fragment it names.
    /// </summary>
    /// <remarks>
    ///     Which snippet the sentence names depends on how the run was pinned, and getting that wrong is the whole
    ///     point of this method existing. The ambient source names <c>Dummy.Reproducibly(seed, ...)</c> — or whatever
    ///     snippet the opener of the current <see cref="Dummy.UseSeed(int, string)" /> scope supplied, since a run pinned
    ///     by a test-framework adapter is replayed by changing what the adapter reads, not by adding a call the test
    ///     never had. A fixed <c>Dummy.WithSeed(...)</c> context replays deterministically on its own, so pinning the
    ///     ambient source would not apply. Naming a snippet the reader's code does not contain is exactly the
    ///     misleading diagnostic this method exists to avoid.
    /// </remarks>
    internal abstract string ReplayGuidance(int seed);

    /// <summary>
    ///     The reproduction guidance for a failure whose seeded draws this source drove but whose result also depends on
    ///     a generator that does not draw from this source — a foreign <see cref="IDummy{T}" />, or a derivation built over
    ///     one (including a <c>Combine</c> that mixes a foreign operand with a sourced one). It names the same replay
    ///     mechanism as <see cref="ReplayGuidance" /> for the seeded part, but scopes the promise to it: the foreign values
    ///     are not reproducible from this seed alone, so claiming a full replay would be the misleading diagnostic the
    ///     seed reporting exists to avoid.
    /// </summary>
    internal abstract string PartialReplayGuidance(int seed);

}
