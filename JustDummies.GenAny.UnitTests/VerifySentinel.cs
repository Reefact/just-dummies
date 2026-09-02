using System;
using System.Collections.Generic;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     The two lines the engine writes when it will not ship a generator silently, and what §5.6 tells a
///     developer to do with the first of them.
/// </summary>
/// <remarks>
///     Both are deliberate compilation failures, and they are not the same failure. <see cref="Verify" />
///     stands over a generator the engine built and cannot vouch for: deleting the line leaves a working
///     chain, which is exactly why blocking was the right call rather than a formality (ADR-0083).
///     <see cref="Supply" /> stands where the engine had nothing to offer at all (§5.5), and deleting it
///     leaves a hole.
///     <para>
///         Spelled once, here, because two benches read them — the named corpus and the generative sweep —
///         and a prefix copied is a prefix that drifts out of step with the emitter.
///     </para>
/// </remarks>
internal static class VerifySentinel {

    /// <summary>The sentinel over a generator the engine built but cannot vouch for (§5.6).</summary>
    internal const string Verify = "TODO_verify_the_generator_for_";

    /// <summary>The sentinel where the engine could name no generator at all (§5.5).</summary>
    internal const string Supply = "TODO_supply_a_generator_for_";

    /// <summary>Whether the line carries either sentinel.</summary>
    internal static bool OnALine(string line) {
        return line.Contains(Verify, StringComparison.Ordinal) || line.Contains(Supply, StringComparison.Ordinal);
    }

    /// <summary>
    ///     What a developer does per §5.6's own instruction: delete the sentinel statement, and the blank line
    ///     the emitter puts after it — nothing else, so what compiled before compiles the same way now.
    /// </summary>
    internal static string StrippedFrom(string source) {
        List<string> kept     = [];
        bool         skipNext = false;

        foreach (string line in source.Split('\n')) {
            if (skipNext) {
                skipNext = false; // the blank line WriteFactories emits right after the sentinel.

                continue;
            }

            if (line.TrimStart().StartsWith("_ = " + Verify, StringComparison.Ordinal)) {
                skipNext = true;

                continue;
            }

            kept.Add(line);
        }

        return string.Join('\n', kept);
    }

}
