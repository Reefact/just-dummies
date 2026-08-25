using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     What an <c>Any.String()</c> chain says about the length of the values it draws, read from the constants at
///     the call site.
/// </summary>
/// <remarks>
///     <para>
///         Shared because two rules ask the same question of the same chain and must not answer it differently:
///         <c>JD030</c> names the interval a chain draws, whose lower end is this floor, and <c>JD015</c> refuses a
///         declared length this floor cannot fit under. Held in one place, they agree by construction; held in two,
///         they drift — which is how one of them came to double-count a repeated prefix while the other did not.
///     </para>
///     <para>
///         This is the arithmetic <c>StringSpec.BuildCandidate</c> performs, and it is deliberately no cleverer:
///         the fragments are laid out side by side without overlap analysis, so the length they need is the plain
///         sum of their lengths (ADR-0046).
///     </para>
/// </remarks>
internal static class StringShapeFacts {

    /// <summary>
    ///     The fewest characters any value the chain draws can have — what the anchored literals occupy, plus the
    ///     one position <c>NotBlank()</c> is owed where none of them already carries a non-blank character, and never
    ///     below the floor of one <c>NonEmpty()</c> and <c>NotBlank()</c> each set.
    /// </summary>
    internal static int Floor(IReadOnlyList<IInvocationOperation> constraints) {
        (int required, bool anchorsCarryNonBlank) = AnchorBudget(constraints);

        bool ownsAPosition = Declares(constraints, "NotBlank") && !anchorsCarryNonBlank;
        int  minimum       = Declares(constraints, "NonEmpty") || Declares(constraints, "NotBlank") ? 1 : 0;

        return System.Math.Max(minimum, ownsAPosition ? required + 1 : required);
    }

    /// <summary>
    ///     Whether the filler is the only place <c>NotBlank()</c>'s character can come from, so it costs a position
    ///     of its own. An anchored literal already carrying one settles the guarantee and costs nothing more.
    /// </summary>
    internal static bool FillerMustCarryNonBlank(IReadOnlyList<IInvocationOperation> constraints) {
        return Declares(constraints, "NotBlank") && !AnchorBudget(constraints).CarryNonBlank;
    }

    /// <summary>
    ///     The characters the anchored literals occupy, and whether any of them already carries a non-blank one.
    /// </summary>
    /// <remarks>
    ///     An anchor the compiler cannot resolve to a constant is left out of both answers, and that direction is the
    ///     safe one: the same blindness that hides its length also keeps <c>NotBlank</c>'s extra position from being
    ///     added on top of it, so an unreadable anchor can only understate the floor and never overstate it.
    /// </remarks>
    internal static (int Required, bool CarryNonBlank) AnchorBudget(IReadOnlyList<IInvocationOperation> constraints) {
        // A prefix and a suffix each own a single slot, so at most one of each ever reaches the draw: re-declaring
        // the same literal is a no-op, and declaring a different one is refused outright. Taking one of each is
        // therefore counting what the specification keeps, not sampling it. Containing accumulates instead, so every
        // fragment it contributes is a fragment the value has to carry.
        string anchored = string.Concat(Anchors(constraints, "StartingWith").Take(1))
                        + string.Concat(Anchors(constraints, "EndingWith").Take(1))
                        + string.Concat(Anchors(constraints, "Containing"));

        return (anchored.Length, anchored.Any(character => !char.IsWhiteSpace(character)));
    }

    /// <summary>The compile-time literals a named anchoring constraint contributes, in the order it was declared.</summary>
    internal static IEnumerable<string> Anchors(IReadOnlyList<IInvocationOperation> constraints, string name) {
        foreach (IInvocationOperation constraint in constraints) {
            if (constraint.TargetMethod.Name != name) { continue; }
            if (constraint.Arguments.Length != 1 || !ConstantFacts.TryGetString(constraint.Arguments[0].Value, out string fragment)) { continue; }

            yield return fragment;
        }
    }

    internal static bool Declares(IReadOnlyList<IInvocationOperation> constraints, string name) {
        return constraints.Any(constraint => constraint.TargetMethod.Name == name);
    }

}
