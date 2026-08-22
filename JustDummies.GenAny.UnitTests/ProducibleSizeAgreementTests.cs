using System;

using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     The engine holds a copy of the library's producible cap, and this is what stops it drifting.
/// </summary>
/// <remarks>
///     ADR-0063 keeps the engine from referencing the library, so it cannot ask what the largest size argument
///     is; it carries the number instead. A copy nothing compares is a copy that goes stale, and this repository
///     already has one that did — <c>JD014</c> declares <c>WithMaxLength</c> uncapped while
///     <c>AnyString.WithMaxLength</c> caps it, a leftover from before ADR-0076 unified the rule. The result was
///     a call the analyzer blessed and the library refused.
///     <para>
///         So neither side names the number here. The engine's boundary is found by asking it, the library's by
///         calling it, and the assertion is that they are the same edge: the largest size the engine will write
///         is one the library accepts, and the first it refuses to write is one the library rejects. Move either
///         constant alone and this fails.
///     </para>
/// </remarks>
public sealed class ProducibleSizeAgreementTests {

    /// <summary>
    ///     A hint for the search, not the answer.
    /// </summary>
    /// <remarks>
    ///     Deliberately well above any plausible cap so that widening the real one does not silently pin the
    ///     search's own edge instead — if the two ever met, this test would stop measuring anything.
    /// </remarks>
    private const int FarAboveTheCap = 64_000_000;

    [Fact(DisplayName = "The engine stops declaring sizes exactly where the library stops accepting them.")]
    public void TheEngineStopsExactlyWhereTheLibraryDoes() {
        int largest = LargestTheEngineWillDeclare();

        Check.WithCustomMessage($"The engine refused every size up to {FarAboveTheCap}.").That(largest).IsStrictlyPositive();
        Check.That(largest).IsStrictlyLessThan(FarAboveTheCap);

        Check.ThatCode(() => Any.String().WithMinLength(largest)).DoesNotThrow();
        Check.ThatCode(() => Any.String().WithMinLength(largest + 1)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>The count family answers to the same number, and the engine writes one constant for both.</summary>
    [Fact(DisplayName = "The count family stops at the same size as the length family.")]
    public void TheCountFamilyStopsAtTheSameSize() {
        int largest = LargestTheEngineWillDeclare();

        Check.ThatCode(() => Any.ListOf(Any.Int32()).WithMinCount(largest)).DoesNotThrow();
        Check.ThatCode(() => Any.ListOf(Any.Int32()).WithMinCount(largest + 1)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     The largest floor the engine is willing to read out of a guard, found by bisection.
    /// </summary>
    /// <remarks>
    ///     Above its cap the engine leaves the parameter neutral and marks it <c>unread guards</c> (§9), which
    ///     is the observable difference this searches on — no internals, no constant, just what comes out.
    /// </remarks>
    private static int LargestTheEngineWillDeclare() {
        int declared = 0;
        int refused  = FarAboveTheCap;

        while (refused - declared > 1) {
            int probe = declared + ((refused - declared) / 2);

            if (Declares(probe)) { declared = probe; } else { refused = probe; }
        }

        return declared;
    }

    private static bool Declares(int size) {
        ScaffoldedParameter parameter = Subject.GuardedBy(
            "string",
            $"if (value.Length < {size}) {{ throw new ArgumentException(nameof(value)); }}");

        return parameter.Expression == $"Any.String().WithMinLength({size})";
    }

}
