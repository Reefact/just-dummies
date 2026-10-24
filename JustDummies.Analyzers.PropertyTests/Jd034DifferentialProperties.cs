using FsCheck;
using FsCheck.Fluent;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     The differential property for <c>JD034</c> beyond <c>Any.String()</c>: random chains of declarations on the
///     other generators are compiled for the analyzer and executed by the run time, and the rule must report a chain
///     exactly when the run time refuses a declaration in it as already defined.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="StringRulesDifferentialProperties" /> holds the string chains, where <c>JD015</c> shares the
///         refusals. Here the chains declare only what <c>JD034</c> reads, chosen so their values never contradict one
///         another (issue #278), and the run time's first refusal decides: one refused as already defined must be
///         reported, and one refused on its own argument hides every declaration after it, which must not be.
///     </para>
///     <para>
///         Like every property in this suite, it fails on an analyzer that throws rather than reading <c>AD0001</c> as
///         silence.
///     </para>
/// </remarks>
public sealed class Jd034DifferentialProperties {

    /// <summary>Each case compiles a snippet, as in <see cref="Jd015DifferentialProperties" />.</summary>
    private const int Cases = 300;

    private const string Jd034 = "JD034";

    [Fact(DisplayName = "JD034 reports a chain on any other generator exactly when the run time refuses a declaration in it as already defined.")]
    public void Jd034AgreesWithTheRunTime() {
        ConstraintAlreadyDefinedAnalyzer analyzer = new();

        Prop.ForAll(DeclarationChains.Arbitrary(),
                    chain => {
                        bool refused  = chain.RuntimeRefusesAsAlreadyDefined();
                        bool reported = chain.ReportedBy(analyzer).Contains(Jd034);
                        if (refused == reported) { return true.ToProperty(); }

                        string disagreement = refused ? "the run time refuses it as already defined and JD034 is silent" : "JD034 reports it and the run time refuses no declaration in it as already defined";

                        return false.Label($"{chain}: {disagreement}.");
                    })
            .Check(Config.QuickThrowOnFailure.WithMaxTest(Cases));
    }

}
