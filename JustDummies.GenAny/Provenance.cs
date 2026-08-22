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
[SuppressMessage(SonarRule.S2342.Category, SonarRule.S2342.Id, Justification = SuppressionJustification.S2342.TheSpecificationNamesItSingular)]
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
    ///     Something before the first assignment to state could be a guard on this parameter, and was not read:
    ///     an <c>if</c> that throws in a way the recognised set does not match — a cross-parameter rule, an
    ///     arithmetic condition, a regex — or a call reaching the parameter with no <c>if</c> at all, the shape
    ///     a guard delegated to a helper takes. The parameter carries the neutral generator, and the developer
    ///     is told where to look (§9).
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
    Unavailable = 64,

    /// <summary>
    ///     A guard was read and understood, and this generator carries no member to say it with.
    /// </summary>
    /// <remarks>
    ///     ADR-0059 has the engine look a member up before writing it, and drop it where it does not resolve.
    ///     That is right — the alternative is a chain that does not compile — but until this value existed the
    ///     drop had no channel at all, so the column stayed empty and the run reported every parameter
    ///     inferred. The invariant was gone and nothing said so.
    ///     <para>
    ///         Distinct from <see cref="Unavailable" />, which is about the <b>generator</b> for a type. Here
    ///         the generator is exactly right and it is one constraint that cannot be expressed on it — an
    ///         enum universe the closed set has no member for, <c>Positive</c> on an unsigned engine. Folding
    ///         the two under one word would make the recap ambiguous precisely where it is trying to be exact.
    ///     </para>
    /// </remarks>
    ConstraintUnavailable = 128

}
