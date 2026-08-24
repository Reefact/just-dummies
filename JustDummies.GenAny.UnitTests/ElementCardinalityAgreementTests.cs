using System;

using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     The engine holds a copy of how many distinct values each small element row can draw, and this is what
///     stops it drifting.
/// </summary>
/// <remarks>
///     The same shape of problem <c>ProducibleSizeAgreementTests</c> guards, one axis over. ADR-0063 keeps the
///     engine from referencing the library, so it cannot ask <c>Any.Char()</c> how many values it draws; it
///     carries the number instead. A copy nothing compares is a copy that goes stale — and this one started as
///     no copy at all, which is worse: every element row but <c>bool</c> and <c>enum</c> read as unbounded, so
///     a distinct floor of 200 over a set of <c>char</c> was written down with confidence and the generator
///     could not even be constructed.
///     <para>
///         So neither side names the number. The engine's edge is found by asking it what it will still
///         declare, the library's by calling it, and the assertion is that the two are the same edge. Move
///         either alone and this fails.
///     </para>
/// </remarks>
public sealed class ElementCardinalityAgreementTests {

    /// <summary>
    ///     A hint for the search, not the answer — well above any of the small domains, and well below the
    ///     producible cap, so the two searches cannot meet and stop measuring.
    /// </summary>
    private const int FarAboveEverySmallDomain = 4_096;

    [Theory(DisplayName = "The engine stops declaring a distinct floor exactly where the element row stops producing values.")]
    [InlineData("char")]
    [InlineData("byte")]
    [InlineData("sbyte")]
    [InlineData("bool")]
    public void TheEngineStopsExactlyWhereTheElementRowDoes(string element) {
        int largest = LargestFloorTheEngineWillDeclare(element);

        Check.WithCustomMessage($"The engine refused every floor over ISet<{element}>.").That(largest).IsStrictlyPositive();
        Check.That(largest).IsStrictlyLessThan(FarAboveEverySmallDomain);

        Check.ThatCode(() => Distinct(element, largest)).DoesNotThrow();
        Check.ThatCode(() => Distinct(element, largest + 1)).Throws<ConflictingAnyConstraintException>();
    }

    /// <summary>An enum is counted by its distinct VALUES, so an alias adds a name and not a value.</summary>
    /// <remarks>
    ///     Written against the library rather than against a hand-counted number: the aliased enum below
    ///     declares five members for three values, and the two sides have to agree on three.
    /// </remarks>
    [Fact(DisplayName = "An aliased enum is counted by its distinct values on both sides.")]
    public void AnAliasedEnumIsCountedByItsValues() {
        int largest = LargestFloorTheEngineWillDeclare("Grade", "public enum Grade { Low = 1, Medium = 2, High = 3, Min = 1, Max = 3 }");

        Check.ThatCode(() => Any.SetOf(Any.Enum<Grade>()).WithMinCount(largest).Generate()).DoesNotThrow();
        Check.ThatCode(() => Any.SetOf(Any.Enum<Grade>()).WithMinCount(largest + 1).Generate()).Throws<ConflictingAnyConstraintException>();
    }

    /// <summary>The mirror of the aliased enum above, declared here so the library can be called on it.</summary>
    public enum Grade {

        Low    = 1,
        Medium = 2,
        High   = 3,
        Min    = 1,
        Max    = 3

    }

    private static void Distinct(string element, int floor) {
        switch (element) {
            case "char":  Any.SetOf(Any.Char()).WithMinCount(floor).Generate();    break;
            case "byte":  Any.SetOf(Any.Byte()).WithMinCount(floor).Generate();    break;
            case "sbyte": Any.SetOf(Any.SByte()).WithMinCount(floor).Generate();   break;
            default:      Any.SetOf(Any.Boolean()).WithMinCount(floor).Generate(); break;
        }
    }

    /// <summary>
    ///     The largest distinct floor the engine is still willing to read out of a guard over
    ///     <c>ISet&lt;element&gt;</c>, found by bisection.
    /// </summary>
    /// <remarks>
    ///     Above the element row's own domain the engine leaves the parameter neutral and marks it
    ///     <c>unread guards</c>, which is the observable difference this searches on — no internals, no
    ///     constant, just what comes out.
    /// </remarks>
    private static int LargestFloorTheEngineWillDeclare(string element, string extra = "") {
        int declared = 0;
        int refused  = FarAboveEverySmallDomain;

        while (refused - declared > 1) {
            int probe = declared + ((refused - declared) / 2);

            if (Declares(element, probe, extra)) { declared = probe; } else { refused = probe; }
        }

        return declared;
    }

    private static bool Declares(string element, int floor, string extra) {
        ScaffoldOutcome outcome = Subject.Scaffold($$"""
                                                    {{extra}}

                                                    public sealed class Subject {

                                                        public Subject(ISet<{{element}}> values) {
                                                            if (values.Count < {{floor}}) { throw new ArgumentException(nameof(values)); }

                                                            Values = values;
                                                        }

                                                        public ISet<{{element}}> Values { get; }
                                                    }
                                                    """);

        return outcome.Plan!.Parameters.Single().Expression?.Contains($"WithMinCount({floor})") == true;
    }

}
