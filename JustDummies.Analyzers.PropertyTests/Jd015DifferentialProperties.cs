using FsCheck;
using FsCheck.Fluent;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     Differential properties for <c>JD015</c>: random <c>Any.String()</c> chains are compiled for the analyzer and
///     executed by the run time, and the rule must report a chain exactly when the run time refuses it.
/// </summary>
/// <remarks>
///     <para>
///         The run time is the oracle: whether a chain admits a value is what executing it says, independent of any
///         rule. Ten of the defects the September 2026 review found were chains that threw at run time with no
///         diagnostic, each found by hand in exactly this way (issue #232). Generating the chains turns that audit into
///         a standing net: a spelling the rule does not yet read fails here the day it is generated, not at the next
///         review.
///     </para>
///     <para>
///         A disagreement either way is a finding. A report on a chain the run time draws is a false accusation; a
///         refused chain left silent is the blind spot the review kept finding.
///     </para>
/// </remarks>
public sealed class Jd015DifferentialProperties {

    /// <summary>
    ///     Each case compiles a snippet, so far fewer than a run-time property can afford — enough, over the life of the
    ///     suite, for the generator's randomness to cover the vocabulary.
    /// </summary>
    private const int Cases = 300;

    private const string Jd015 = "JD015";

    [Fact(DisplayName = "JD015 reports an Any.String() chain exactly when the run time refuses it.")]
    public void Jd015AgreesWithTheRunTime() {
        StringConstraintsAdmitNoValueAnalyzer analyzer = new();

        Prop.ForAll(StringChains.Arbitrary(),
                    chain => {
                        bool refused  = chain.RuntimeRefuses();
                        bool reported = chain.IsReportedBy(analyzer, Jd015);
                        if (refused == reported) { return true.ToProperty(); }

                        string disagreement = refused ? "the run time refuses it and JD015 is silent" : "JD015 reports it and the run time draws it";

                        return false.Label($"{chain}: {disagreement}.");
                    })
            .Check(Config.QuickThrowOnFailure.WithMaxTest(Cases));
    }

}
