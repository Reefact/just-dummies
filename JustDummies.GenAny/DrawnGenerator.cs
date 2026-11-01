using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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

    [SuppressMessage(SonarRule.S107.Category, SonarRule.S107.Id, Justification = "The private constructor threads the expression's whole immutable state for From, Unresolved and Then; no caller writes this argument list, and a parameter object would only rename it.")]
    private DrawnGenerator(string? core,
                           ITypeSymbol? builder,
                           string? generatorType,
                           ITypeSymbol? draws,
                           string suffix,
                           IReadOnlyList<GuardConstraint> seeded,
                           IReadOnlyList<GuardConstraint> tightening,
                           Provenance provenance,
                           Unscaffoldable? unscaffoldable = null) {
        Core          = core;
        Builder       = builder;
        GeneratorType = generatorType;
        Draws         = draws;
        Suffix        = suffix;
        Seeded        = seeded;
        Tightening    = tightening;
        Provenance    = provenance;

        Unscaffoldable = unscaffoldable;
    }

    /// <summary>The expression before any constraint — <c>Any.Int32()</c>.</summary>
    internal string? Core { get; }

    /// <summary>
    ///     The builder type the constraints are checked against, built for its arguments —
    ///     <c>AnyList&lt;string&gt;</c>, never the factory's open <c>AnyList&lt;T&gt;</c> — or null when nothing
    ///     more may be added.
    /// </summary>
    internal ITypeSymbol? Builder { get; }

    /// <summary>
    ///     The type of <see cref="Core" />, as the emitted file spells it — <c>AnyInt32</c>,
    ///     <c>AnyList&lt;string&gt;</c>, <c>AnyOrderReference</c>.
    /// </summary>
    /// <remarks>
    ///     Where a <see cref="Builder" /> is known, the type the parameter's factory returns is read off the
    ///     chain's last call instead, since a constraint may return another generator (ADR-0109). This is the
    ///     answer where none is: a composed parameter, whose generator this compilation may not carry yet — yet
    ///     whose name is known all the same, since composition writes it blind.
    /// </remarks>
    internal string? GeneratorType { get; }

    /// <summary>
    ///     The type the whole expression draws, its conversion included — what a collection over it is built
    ///     for.
    /// </summary>
    internal ITypeSymbol? Draws { get; }

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
    ///     The abstract type this draw is left open for, and the generator to write for it — null unless
    ///     <see cref="Provenance" /> carries <see cref="GenAny.Provenance.AbstractType" />.
    /// </summary>
    internal Unscaffoldable? Unscaffoldable { get; }

    /// <summary>Whether the table had an answer at all.</summary>
    internal bool Resolved => Core is not null;

    internal static DrawnGenerator From(string core,
                                        ITypeSymbol? builder,
                                        string generatorType,
                                        ITypeSymbol draws,
                                        IReadOnlyList<GuardConstraint>? seeded = null,
                                        string suffix = "",
                                        Provenance provenance = Provenance.None) {
        return new DrawnGenerator(core, builder, generatorType, draws, suffix, seeded ?? [], tightening: [], provenance);
    }

    /// <summary>No row matched, and <paramref name="why" /> says what the recap should report.</summary>
    internal static DrawnGenerator Unresolved(Provenance why = Provenance.None) {
        return new DrawnGenerator(core: null,
                                  builder: null,
                                  generatorType: null,
                                  draws: null,
                                  suffix: string.Empty,
                                  seeded: [],
                                  tightening: [],
                                  why);
    }

    /// <summary>A composed type <c>any</c> cannot scaffold because it is abstract (§5.5).</summary>
    internal static DrawnGenerator UnresolvedAbstract(Unscaffoldable type) {
        return new DrawnGenerator(core: null,
                                  builder: null,
                                  generatorType: null,
                                  draws: null,
                                  suffix: string.Empty,
                                  seeded: [],
                                  tightening: [],
                                  Provenance.AbstractType,
                                  type);
    }

    /// <summary>
    ///     An unresolved draw that keeps what a draw it was built from had to say about an abstract type — a
    ///     collection whose element is one is left open for that element's reason.
    /// </summary>
    internal static DrawnGenerator UnresolvedLike(DrawnGenerator inner) {
        return inner.Unscaffoldable is { } type ? UnresolvedAbstract(type) : Unresolved();
    }

    /// <summary>
    ///     The same expression with one more hop after its constraints, now drawing <paramref name="draws" />.
    /// </summary>
    /// <remarks>
    ///     <see cref="GeneratorType" /> does not move: the hop comes after the factory, so the factory still
    ///     returns the generator the constraints were written on.
    /// </remarks>
    internal DrawnGenerator Then(string suffix, ITypeSymbol draws, Provenance added, IReadOnlyList<GuardConstraint>? more = null) {
        List<GuardConstraint> tightening = [.. Tightening];

        if (more is not null) { tightening.AddRange(more); }

        return new DrawnGenerator(Core, Builder, GeneratorType, draws, Suffix + suffix, Seeded, tightening, Provenance | added);
    }

}
