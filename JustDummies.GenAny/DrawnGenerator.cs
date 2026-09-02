using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace JustDummies.GenAny;

/// <summary>
///     A generator expression in three parts, so a guard-derived constraint can be slotted into the middle.
/// </summary>
/// <remarks>
///     §5.3 is precise about where a constraint belongs: with the generator for the parameter's <b>own</b> type,
///     before any conversion or composition. An <c>int?</c> guarded by <c>p &lt;= 0</c> emits
///     <c>Any.Int32().Positive().AsNullable()</c>, not the reverse, and a factory parameter guarded inside the
///     factory's body emits <c>Any.String().NonEmpty().As(OrderReference.Create)</c>. The conversion hop always
///     comes last, because it is the step that changes the type — which is exactly why the expression cannot be
///     built as one string and patched afterwards.
/// </remarks>
internal sealed class DrawnGenerator {

    private DrawnGenerator(string? core,
                           ITypeSymbol? builder,
                           string suffix,
                           IReadOnlyList<GuardConstraint> seeded,
                           IReadOnlyList<GuardConstraint> tightening,
                           Provenance provenance,
                           IReadOnlyList<string> ambiguousGenerators) {
        Core                = core;
        Builder             = builder;
        Suffix              = suffix;
        Seeded              = seeded;
        Tightening          = tightening;
        Provenance          = provenance;
        AmbiguousGenerators = ambiguousGenerators;
    }

    /// <summary>The expression before any constraint — <c>Any.Int32()</c>.</summary>
    internal string? Core { get; }

    /// <summary>
    ///     The builder type the constraints are checked against, or null when nothing more may be added.
    /// </summary>
    internal ITypeSymbol? Builder { get; }

    /// <summary>What follows the constraints — the conversion or composition hop.</summary>
    internal string Suffix { get; }

    /// <summary>
    ///     The constraints the base table's own row carries, before any guard is read.
    /// </summary>
    /// <remarks>
    ///     Carried here rather than baked into <see cref="Core" /> so a guard saying the same thing collapses
    ///     into it instead of colliding with it: a <c>string</c> row is already <c>.NonEmpty()</c>, and a
    ///     constructor guarding on <c>IsNullOrEmpty</c> must not turn that into a contradiction.
    /// </remarks>
    internal IReadOnlyList<GuardConstraint> Seeded { get; }

    /// <summary>
    ///     The constraints a composed factory's own guards declared (§5.3, §5.4).
    /// </summary>
    /// <remarks>
    ///     Apart from <see cref="Seeded" /> because the two answer differently under combination: a row's
    ///     refinement is the engine's own opinion and yields where a guard contradicts it, while these are the
    ///     developer's declarations and stand. Keeping them apart is also what lets the recap's <c>guard</c>
    ///     word be computed from the constraints <b>applied</b> rather than merely read — the same honesty rule
    ///     the constructor path keeps (§6).
    /// </remarks>
    internal IReadOnlyList<GuardConstraint> Tightening { get; }

    /// <summary>Where this came from, as the recap will report it.</summary>
    internal Provenance Provenance { get; }

    /// <summary>
    ///     The full names of every generator that named the parameter's type equally well, when composition
    ///     (§5.4) found more than one and would not choose between them. Empty otherwise.
    /// </summary>
    internal IReadOnlyList<string> AmbiguousGenerators { get; }

    /// <summary>Whether the table had an answer at all.</summary>
    internal bool Resolved => Core is not null;

    internal static DrawnGenerator From(string core,
                                        ITypeSymbol? builder,
                                        IReadOnlyList<GuardConstraint>? seeded = null,
                                        string suffix = "",
                                        Provenance provenance = Provenance.None) {
        return new DrawnGenerator(core, builder, suffix, seeded ?? [], tightening: [], provenance, ambiguousGenerators: []);
    }

    /// <summary>No row matched, and <paramref name="why" /> says what the recap should report.</summary>
    internal static DrawnGenerator Unresolved(Provenance why = Provenance.None) {
        return new DrawnGenerator(core: null, builder: null, suffix: string.Empty, seeded: [], tightening: [], why,
                                  ambiguousGenerators: []);
    }

    /// <summary>
    ///     No row matched because several generators named <paramref name="candidates" /> would each have
    ///     served — and none of them was the tool's to pick (§5.4).
    /// </summary>
    internal static DrawnGenerator Ambiguous(IReadOnlyList<string> candidates) {
        return new DrawnGenerator(core: null, builder: null, suffix: string.Empty, seeded: [], tightening: [],
                                  Provenance.None, candidates);
    }

    /// <summary>The same expression with one more hop after its constraints.</summary>
    internal DrawnGenerator Then(string suffix, Provenance added, IReadOnlyList<GuardConstraint>? more = null) {
        List<GuardConstraint> tightening = [.. Tightening];

        if (more is not null) { tightening.AddRange(more); }

        return new DrawnGenerator(Core, Builder, Suffix + suffix, Seeded, tightening, Provenance | added, AmbiguousGenerators);
    }

}
