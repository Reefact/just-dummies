using FsCheck;
using FsCheck.Fluent;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     The differential property for the string rules taken together: every chain the vocabulary can spell, the
///     conflicting declarations included, is refused by the run time exactly when <c>JD015</c> or <c>JD034</c> reports
///     it.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Jd015DifferentialProperties" /> holds <c>JD015</c> to its own scope, the constants that admit no
///         value. A second, different declaration of a constraint the generator takes once is <c>JD034</c>'s (issue
///         #274). Between them the two rules claim every refusal a chain of constants meets at a call or at the draw,
///         so this comparison runs with the scope filter off (ADR-0106).
///     </para>
///     <para>
///         That also makes it the property that reads every chain the vocabulary can spell, so an analyzer that throws
///         on one is found here: the harness fails on <c>AD0001</c> rather than reading it as silence.
///     </para>
/// </remarks>
public sealed class StringRulesDifferentialProperties {

    /// <summary>Each case compiles a snippet, as in <see cref="Jd015DifferentialProperties" />.</summary>
    private const int Cases = 300;

    private const string Jd015 = "JD015";
    private const string Jd034 = "JD034";

    [Fact(DisplayName = "JD015 or JD034 reports an Any.String() chain exactly when the run time refuses it, whatever it declares.")]
    public void TheStringRulesAgreeWithTheRunTime() {
        StringConstraintsAdmitNoValueAnalyzer admitsNoValue  = new();
        ConstraintAlreadyDefinedAnalyzer      alreadyDefined = new();

        Prop.ForAll(StringChains.Unscoped(),
                    chain => {
                        bool refused  = chain.RuntimeRefuses();
                        bool reported = chain.ReportedBy(admitsNoValue, alreadyDefined).Overlaps([Jd015, Jd034]);
                        if (refused == reported) { return true.ToProperty(); }

                        string disagreement = refused ? "the run time refuses it and JD015 and JD034 are silent" : "a rule reports it and the run time draws it";

                        return false.Label($"{chain}: {disagreement}.");
                    })
            .Check(Config.QuickThrowOnFailure.WithMaxTest(Cases));
    }

}
