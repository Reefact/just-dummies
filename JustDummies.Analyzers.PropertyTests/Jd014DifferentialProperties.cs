using FsCheck;
using FsCheck.Fluent;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     The differential property for <c>JD014</c> on the guards it reads beyond a range or a non-empty check: one call
///     with a generated argument is compiled for the analyzer and applied by the run time, and the rule must report it
///     exactly when the run time rejects the argument.
/// </summary>
/// <remarks>
///     <para>
///         Where the call meets a guard the rule does not claim — a host <c>System.Uri</c> judges, a user-info part
///         whose case mapping depends on the runtime — only half of that holds: what the rule reports must still be
///         rejected, but a rejection it leaves silent is the one it disclaims, since this runtime is not every runtime
///         (see <see cref="GuardedCall" />). Its named cases pin that silence.
///     </para>
///     <para>
///         Like every property in this suite, it fails on an analyzer that throws rather than reading <c>AD0001</c> as
///         silence.
///     </para>
/// </remarks>
public sealed class Jd014DifferentialProperties {

    /// <summary>Each case compiles a snippet, as in <see cref="Jd015DifferentialProperties" />.</summary>
    private const int Cases = 300;

    private const string Jd014 = "JD014";

    private const string Jd017 = "JD017";

    // An enumeration value the type does not define is JD017's to report (issue #321): the call is analyzed by both
    // rules, the one owning it must agree with the run time, and the other must stay silent, so one mistake draws one
    // diagnostic.
    [Fact(DisplayName = "JD014 reports a call exactly when the run time rejects its argument, wherever it claims the guard, and leaves an undefined enumeration value to JD017.")]
    public void Jd014AgreesWithTheRunTime() {
        RejectedConstantArgumentAnalyzer jd014 = new();
        EnumUniverseViolationAnalyzer    jd017 = new();

        Prop.ForAll(GuardedCalls.Arbitrary(),
                    call => {
                        ISet<string> ids      = call.ReportedBy(jd014, jd017);
                        bool         reported = ids.Contains(call.Rule);
                        bool         rejected = call.RuntimeRejectsTheArgument();
                        string?      other    = ids.FirstOrDefault(id => id != call.Rule && id is Jd014 or Jd017);
                        if (other is not null) { return false.Label($"{call}: {other} reports it, which is {call.Rule}'s to report."); }
                        if (rejected == reported || (rejected && !call.Claimed)) { return true.ToProperty(); }

                        string disagreement = rejected ? $"the run time rejects its argument and {call.Rule} is silent" : $"{call.Rule} reports it and the run time accepts its argument";

                        return false.Label($"{call}: {disagreement}.");
                    })
            .Check(Config.QuickThrowOnFailure.WithMaxTest(Cases));
    }

}
