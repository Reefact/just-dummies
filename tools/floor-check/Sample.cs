using System.Collections.Generic;
using System.Globalization;

using JustDummies;

namespace FloorCheck;

/// <summary>
///     The source the floor job gives the oldest supported compiler to analyze.
///     <para>
///         It is not a demonstration of the library and should not grow into one. Its only job is to be code
///         the bundled analyzers have a reason to look at, so that Roslyn's <c>ReportAnalyzer</c> table lists
///         them — which is what proves they loaded from the package's <c>analyzers/dotnet/cs</c> folder under
///         Roslyn 4.8 rather than silently failing with CS8032.
///     </para>
///     <para>
///         Everything here must stay CLEAN: an Error-severity JD diagnostic fails this build, and that failure
///         would be indistinguishable from the load failure the job exists to detect. Draws are therefore
///         materialized through <c>Generate()</c> (JD006), results are used rather than discarded, and no
///         generator is interpolated into a string (JD005).
///     </para>
/// </summary>
internal static class Sample {

    // Returns a value derived from every draw. This project is a LIBRARY: it is built, never run, so the
    // results need a consumer only to keep the compiler from reporting unused locals — which the floor SDK
    // would report and which would be noise in the one job whose signal is a clean diagnostic.
    public static int Exercise() {
        // Scalars, with the constraint families the analyzers reason about.
        int    roll   = Dummy.Int32().Between(1, 6).Generate();
        int    count  = Dummy.Int32().Positive().Generate();
        string label  = Dummy.String().NonEmpty().WithMaxLength(50).Generate();
        double ratio  = Dummy.Double().Between(0d, 1d).Generate();
        bool   toggle = Dummy.Boolean().Generate();

        // Composition: As, pairs, and an explicit pool.
        string identifier = Dummy.Int32().Between(1, 999)
                               .As(value => "ID-" + value.ToString(CultureInfo.InvariantCulture))
                               .Generate();
        (int, string) pair = Dummy.PairOf(Dummy.Int32().Between(1, 9),
                                        Dummy.String().NonEmpty().WithMaxLength(4)).Generate();

        // Collections, where the cardinality rules live.
        List<int> values = Dummy.ListOf(Dummy.Int32().Between(0, 9)).WithCount(4).Generate();

        // Reproducibility: a pinned scope, awaited nowhere and discarded nowhere, so the JD001-JD004 family
        // has a well-formed call site to inspect rather than a violation.
        int drawn = 0;
        Dummy.Reproducibly(() => drawn = Dummy.Int32().Between(10, 20).Generate());

        return roll
             + count
             + label.Length
             + (int)(ratio * 100d)
             + (toggle ? 1 : 0)
             + identifier.Length
             + pair.Item1
             + pair.Item2.Length
             + values.Count
             + drawn;
    }
}
