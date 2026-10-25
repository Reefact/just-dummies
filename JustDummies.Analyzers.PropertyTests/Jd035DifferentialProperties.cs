using FsCheck;
using FsCheck.Fluent;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     The differential property for <c>JD035</c>: random chains are compiled for the analyzer and applied call by call
///     by the run time, and the rule must report a call exactly when the run time ignores it.
/// </summary>
/// <remarks>
///     <para>
///         The run time is the oracle, read through what a call changes: one it ignores leaves every draw, under every
///         seed, as it was before it. The vocabularies are built so that "ignored" can only mean "repeated" (see
///         <see cref="RepetitionChains" />), which makes this the rule's whole claim checked both ways: no repetition
///         left silent, no call reported that the run time applies (issue #279).
///     </para>
///     <para>
///         Like every property in this suite, it fails on an analyzer that throws rather than reading <c>AD0001</c> as
///         silence.
///     </para>
/// </remarks>
public sealed class Jd035DifferentialProperties {

    /// <summary>Each case compiles a snippet, as in <see cref="Jd015DifferentialProperties" />.</summary>
    private const int Cases = 300;

    [Fact(DisplayName = "JD035 reports a call exactly when the run time ignores it.")]
    public void Jd035AgreesWithTheRunTime() {
        ConstraintRepeatedAnalyzer analyzer = new();

        Prop.ForAll(RepetitionChains.Arbitrary(),
                    chain => {
                        ISet<int> ignored  = chain.IgnoredCalls();
                        ISet<int> reported = chain.ReportedCalls(analyzer);
                        if (ignored.SetEquals(reported)) { return true.ToProperty(); }

                        return false.Label($"{chain}: the run time ignores the calls at [{string.Join(", ", ignored.Order())}], JD035 reports [{string.Join(", ", reported.Order())}].");
                    })
            .Check(Config.QuickThrowOnFailure.WithMaxTest(Cases));
    }

}
