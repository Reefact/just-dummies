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
    ///     arithmetic condition, a regex — or a call reaching the parameter, whether or not an <c>if</c>
    ///     surrounds it, the shape a guard delegated to a helper takes. A guard the set <b>does</b> match
    ///     earns the mark too where the engine cannot place it: below a write to its own parameter, under
    ///     something deciding whether it runs, or below a statement that can jump past it. The parameter
    ///     carries the neutral generator, and the developer is told where to look (§9).
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
    ConstraintUnavailable = 128,

    /// <summary>
    ///     The parameter reaches the very type being scaffolded — directly, through a nullable, through a
    ///     collection's elements, or through the construction of a type it composes — so it is left open.
    /// </summary>
    /// <remarks>
    ///     Composing it would write <c>new AnyNode()</c> inside <c>AnyNode</c>'s own constructor, which recurses
    ///     on construction and ends the test run in a stack overflow before any test reports. An empty list, a
    ///     null or a depth parameter would each be the engine deciding how deep the developer's data goes, which
    ///     is a cleverer generator rather than an honest one (ADR-0046); the bound is the developer's to write.
    /// </remarks>
    SelfReferencing = 256,

    /// <summary>
    ///     The parameter is, or holds, a composed type that is abstract — an interface or an abstract class — so it
    ///     is left open, naming the generator to write by hand (issue #375).
    /// </summary>
    /// <remarks>
    ///     Composition writes <c>new AnyX()</c> blind for every type <c>any</c> could scaffold, and the developer's
    ///     <c>CS0246</c> is the work list (ADR-0089). An abstract type is the one plain shape that list sent to a
    ///     dead end: <c>any generate IRowElement</c> is refused, since there is nothing to instantiate. It joins the
    ///     generic type, the other shape whose command is refused, in §5.5: the open parameter names the generator to write —
    ///     typically one choosing among the concrete generators — and nothing about those concrete types is looked
    ///     up, which would be composition asking the compilation the question ADR-0089 keeps it from asking.
    /// </remarks>
    AbstractType = 512

}
