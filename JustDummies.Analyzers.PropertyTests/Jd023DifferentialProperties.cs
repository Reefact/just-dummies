using FsCheck;
using FsCheck.Fluent;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     The differential properties for <c>JD023</c> and <c>JD038</c>: random chains on the exact-type generators are
///     compiled for the analyzer and executed by the run time, and JD023 must report a chain exactly when the run time
///     refuses it as admitting no value, while JD038 must report a built chain exactly when the generator counts one
///     value (issue #356).
/// </summary>
/// <remarks>
///     <para>
///         A chain the run time refuses on an argument, or as already defined, is JD014's or JD034's: both rules here
///         must stay silent on it, since the walk ends where another rule begins. The vocabulary keeps every range
///         within the few dozen values the rule walks, so the rule's silence on a wider domain is never exercised.
///     </para>
///     <para>
///         Like every property in this suite, it fails on an analyzer that throws rather than reading <c>AD0001</c> as
///         silence.
///     </para>
/// </remarks>
public sealed class Jd023DifferentialProperties {

    /// <summary>Each case compiles a snippet, as in <see cref="Jd015DifferentialProperties" />.</summary>
    private const int Cases = 300;

    private const string Jd023 = "JD023";
    private const string Jd038 = "JD038";

    [Fact(DisplayName = "JD023 reports an exact-type chain exactly when the run time refuses it as admitting no value, and JD038 a built chain exactly when the generator counts one value.")]
    public void Jd023AndJd038AgreeWithTheRunTime() {
        ScalarChainAdmitsNoValueAnalyzer analyzer = new();

        Prop.ForAll(ScalarChains.Arbitrary(),
                    chain => {
                        ScalarOutcome outcome  = chain.RunTime();
                        ISet<string>  reported = chain.ReportedBy(analyzer);
                        bool          empty    = reported.Contains(Jd023);
                        bool          single   = reported.Contains(Jd038);

                        return outcome.Verdict switch {
                            ScalarVerdict.AdmitsNoValue when !empty      => Fails($"{chain}: the run time refuses it as admitting no value and JD023 is silent."),
                            ScalarVerdict.AdmitsNoValue when single      => Fails($"{chain}: JD038 reports a chain the run time refuses."),
                            ScalarVerdict.OwnedByAnotherRule when empty  => Fails($"{chain}: JD023 reports a chain the run time refuses on an argument or as already defined."),
                            ScalarVerdict.OwnedByAnotherRule when single => Fails($"{chain}: JD038 reports a chain the run time refuses on an argument or as already defined."),
                            ScalarVerdict.Built when empty               => Fails($"{chain}: JD023 reports it and the run time builds it."),
                            ScalarVerdict.Built when single != (outcome.Cardinality == 1) => Fails($"{chain}: the generator counts {outcome.Cardinality?.ToString() ?? "an uncounted number of"} value(s) and JD038 {(single ? "reports it" : "is silent")}."),
                            _                                            => true.ToProperty()
                        };
                    })
            .Check(Config.QuickThrowOnFailure.WithMaxTest(Cases));
    }

    private static Property Fails(string label) {
        return false.Label(label);
    }

}
