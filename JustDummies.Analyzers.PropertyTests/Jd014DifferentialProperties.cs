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

    [Fact(DisplayName = "JD014 reports a call exactly when the run time rejects its argument, wherever it claims the guard.")]
    public void Jd014AgreesWithTheRunTime() {
        RejectedConstantArgumentAnalyzer analyzer = new();

        Prop.ForAll(GuardedCalls.Arbitrary(),
                    call => {
                        bool reported = call.ReportedBy(analyzer).Contains(Jd014);
                        bool rejected = call.RuntimeRejectsTheArgument();
                        if (rejected == reported || (rejected && !call.Claimed)) { return true.ToProperty(); }

                        string disagreement = rejected ? "the run time rejects its argument and JD014 is silent" : "JD014 reports it and the run time accepts its argument";

                        return false.Label($"{call}: {disagreement}.");
                    })
            .Check(Config.QuickThrowOnFailure.WithMaxTest(Cases));
    }

}
