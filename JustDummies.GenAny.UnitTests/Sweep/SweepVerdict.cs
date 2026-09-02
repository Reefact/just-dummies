namespace JustDummies.GenAny.UnitTests.Sweep;

/// <summary>
///     What the sweep concluded about one shape.
/// </summary>
/// <remarks>
///     Five outcomes rather than pass and fail, because three of them are outcomes the engine is entitled to
///     and a bench that folded them into "failed" would report the library's own honesty as a defect — the
///     mirror of the mistake ADR-0093 records on the other instrument, where a timeout was folded into
///     "killed". Only <see cref="Finding" /> is the engine's problem, and only <see cref="SweepBug" /> is
///     ours.
/// </remarks>
internal enum SweepVerdict {

    /// <summary>Every rule held: the file compiled, raised nothing, and drew values the domain accepted.</summary>
    Held,

    /// <summary>
    ///     The draw was refused with a first-class <c>AnyGenerationException</c> — the domain declares
    ///     something no generator of this library can honour, and the library said so (ADR-0046).
    /// </summary>
    RefusedByDesign,

    /// <summary>
    ///     Compilation is blocked by a <see cref="VerifySentinel.Verify" /> line: the engine read a guard it
    ///     cannot vouch for and refuses to ship the guess silently (§5.6, ADR-0083). Deleting the line leaves
    ///     a chain that compiles, which rule 3 proves rather than assumes.
    /// </summary>
    BlockedForVerification,

    /// <summary>
    ///     Compilation is blocked by a <see cref="VerifySentinel.Supply" /> line: an open parameter the
    ///     engine could name nothing for (§5.5). Distinct from the above — there is no base underneath to
    ///     verify, so rule 3 has nothing to say.
    /// </summary>
    Unresolved,

    /// <summary>
    ///     The generator produced a value the domain rejects, and §9 says it would.
    /// </summary>
    /// <remarks>
    ///     The residue the specification declares as a non-goal: a guard reached through a level of
    ///     indirection the tool does not follow — a local copy of the parameter above all — is one the tool
    ///     cannot tell from no guard at all, so it marks nothing and blocks nothing. That is a decision, not a
    ///     defect, and a bench that called it one would be reporting the specification back at itself.
    ///     <para>
    ///         Counted rather than tolerated, which is the point: this is the only instrument in the
    ///         repository that puts a number on how wide that residue is, and a shape that stops landing here
    ///         moves the committed counts — so the residue shrinking announces itself too.
    ///     </para>
    /// </remarks>
    KnownResidue,

    /// <summary>
    ///     A rule the engine must hold, broken — and an entry in <see cref="SweepDefects" /> already says so.
    /// </summary>
    /// <remarks>
    ///     The same contract a <c>defect:</c>-marked row of <see cref="GuardCorpus" /> carries: the mark
    ///     names what is wrong, the bench stays green while it stands, and it comes off with the fix rather
    ///     than with the test. An entry that stops claiming any shape fails the run, so a defect cannot be
    ///     fixed and left on the record.
    /// </remarks>
    KnownDefect,

    /// <summary>A rule the engine must hold, broken. This is what the sweep exists to produce.</summary>
    Finding,

    /// <summary>
    ///     The generated domain does not compile on its own. The sweep's own defect, never the engine's, and
    ///     it is reported in words that cannot be mistaken for a finding.
    /// </summary>
    SweepBug

}
