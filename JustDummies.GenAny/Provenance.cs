using System;
using System.Diagnostics.CodeAnalysis;

namespace JustDummies.GenAny;

/// <summary>
///     Where a parameter's expression came from, and what the engine could not read while producing it.
/// </summary>
/// <remarks>
///     The console recap prints this in its right-hand column (§6), and it is not decoration: it is the
///     mechanism that keeps the tool honest about what it inferred and what it guessed. A developer who cannot
///     tell "inferred, and here is why" from "gave up" has to re-derive the whole file by hand.
///     <para>
///         Flags rather than one value, because a parameter can be several at once: the worked example's
///         <c>reference</c> is <c>factory, guard</c> — composed through <c>OrderReference.Create</c>, and
///         tightened by the guard inside that factory's own body.
///     </para>
/// </remarks>
[Flags]
[SuppressMessage(SonarRule.S2342.Category, SonarRule.S2342.Id, Justification = "The specification names this concept in the singular (§6, \"provenance\"), and the property that carries it is Provenance; a plural type name would make every use site read `Provenances Provenance`.")]
public enum Provenance {

    /// <summary>Nothing to report: straight from the base table (§5.2), and the recap's column stays empty.</summary>
    None = 0,

    /// <summary>A constructor or factory guard tightened it (§5.3).</summary>
    Guard = 1,

    /// <summary>Composed through a recognised static factory (§5.4).</summary>
    Factory = 2,

    /// <summary>Composed through a generator already scaffolded for the type (§5.4).</summary>
    Scaffolded = 4,

    /// <summary>
    ///     Two guards set the same bound, or a lower bound above an upper one. Both were dropped rather than
    ///     reconciled: the library would refuse the chain at construction, and guessing which one the developer
    ///     meant is not the engine's call.
    /// </summary>
    GuardsNotCombined = 8,

    /// <summary>
    ///     The constructor's body was unavailable — a type from a package rather than from the solution — so no
    ///     guard could be read. Not the same as having none.
    /// </summary>
    NoSource = 16,

    /// <summary>
    ///     The body throws in a way the recognised set does not match: a cross-parameter rule, an arithmetic
    ///     condition, a regex. The parameter carries the neutral generator, and the developer is told where to
    ///     look (§9).
    /// </summary>
    UnreadGuards = 32,

    /// <summary>
    ///     The generator exists in the library but not in the asset this project resolves — <c>Any.DateOnly()</c>
    ///     on a project below .NET 8.
    /// </summary>
    /// <remarks>
    ///     Worth its own value rather than reading as "not inferred": the truth is "inferred, but not available
    ///     here — retarget, or write it yourself", and one word turns a dead end into an instruction.
    /// </remarks>
    Unavailable = 64

}
