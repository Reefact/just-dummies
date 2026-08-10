using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace JustDummies.GenAny;

/// <summary>
///     A generator expression in three parts, so a guard-derived constraint can be slotted into the middle.
/// </summary>
/// <remarks>
///     §5.3 is precise about where a constraint belongs: with the generator for the parameter's <b>own</b> type,
///     before any conversion or composition. An <c>int?</c> guarded by <c>p &lt;= 0</c> emits
///     <c>Any.Int32().Positive().As(value =&gt; (int?)value)</c>, not the reverse, and a factory parameter
///     guarded inside the factory's body emits <c>Any.String().NonEmpty().As(OrderReference.Create)</c>. The
///     <c>.As</c> hop always comes last, because it is the step that changes the type — which is exactly why the
///     expression cannot be built as one string and patched afterwards.
/// </remarks>
internal sealed class DrawnGenerator {

    private DrawnGenerator(string? core,
                           ITypeSymbol? builder,
                           string suffix,
                           IReadOnlyList<GuardConstraint> seeded,
                           Provenance provenance) {
        Core       = core;
        Builder    = builder;
        Suffix     = suffix;
        Seeded     = seeded;
        Provenance = provenance;
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
    ///     constructor guarding on <c>IsNullOrWhiteSpace</c> must not turn that into a contradiction.
    /// </remarks>
    internal IReadOnlyList<GuardConstraint> Seeded { get; }

    /// <summary>Where this came from, as the recap will report it.</summary>
    internal Provenance Provenance { get; }

    /// <summary>Whether the table had an answer at all.</summary>
    internal bool Resolved => Core is not null;

    internal static DrawnGenerator From(string core,
                                        ITypeSymbol? builder,
                                        IReadOnlyList<GuardConstraint>? seeded = null,
                                        string suffix = "",
                                        Provenance provenance = Provenance.None) {
        return new DrawnGenerator(core, builder, suffix, seeded ?? [], provenance);
    }

    /// <summary>No row matched, and <paramref name="why" /> says what the recap should report.</summary>
    internal static DrawnGenerator Unresolved(Provenance why = Provenance.None) {
        return new DrawnGenerator(core: null, builder: null, suffix: string.Empty, seeded: [], why);
    }

    /// <summary>The same expression with one more hop after its constraints.</summary>
    internal DrawnGenerator Then(string suffix, Provenance added, IReadOnlyList<GuardConstraint>? more = null) {
        List<GuardConstraint> seeded = [.. Seeded];

        if (more is not null) { seeded.AddRange(more); }

        return new DrawnGenerator(Core, Builder, Suffix + suffix, seeded, Provenance | added);
    }

}
