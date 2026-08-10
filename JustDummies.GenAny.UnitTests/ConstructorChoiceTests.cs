using System.Linq;

using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     Which constructor <c>Generate()</c> calls (§5.1).
/// </summary>
public sealed class ConstructorChoiceTests {

    // The widest constructor states the type's whole shape; a narrower overload usually defaults something the
    // developer would rather see varied.
    [Fact(DisplayName = "The widest public constructor wins.")]
    public void TheWidestPublicConstructorWins() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {
                                                       public Subject(int one) { }
                                                       public Subject(int one, string two) { }
                                                       public Subject(int one, string two, bool three) { }
                                                   }
                                                   """);

        Check.That(Names(outcome)).ContainsExactly("one", "two", "three");
    }

    [Fact(DisplayName = "Two constructors of the same width are settled by source order.")]
    public void TwoConstructorsOfTheSameWidthAreSettledBySourceOrder() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {
                                                       public Subject(int first) { }
                                                       public Subject(string second) { }
                                                   }
                                                   """);

        Check.That(Names(outcome)).ContainsExactly("first");
    }

    [Fact(DisplayName = "A non-public constructor never competes.")]
    public void ANonPublicConstructorNeverCompetes() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {
                                                       internal Subject(int one, string two, bool three) { }
                                                       public Subject(int one) { }
                                                   }
                                                   """);

        Check.That(Names(outcome)).ContainsExactly("one");
    }

    /// <summary>
    ///     A <c>ref</c> or <c>out</c> parameter makes a constructor ineligible rather than merely awkward:
    ///     <c>Generate()</c> passes value arguments, and such a call site does not compile (<c>CS1620</c>).
    /// </summary>
    [Fact(DisplayName = "A constructor taking ref or out is skipped for the next candidate.")]
    public void AConstructorTakingRefOrOutIsSkipped() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {
                                                       public Subject(int one, out string two) { two = ""; }
                                                       public Subject(int one) { }
                                                   }
                                                   """);

        Check.That(Names(outcome)).ContainsExactly("one");
    }

    // `in` is fine: a value argument binds to it.
    [Fact(DisplayName = "A constructor taking in is eligible.")]
    public void AConstructorTakingInIsEligible() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {
                                                       public Subject(in int one) { }
                                                   }
                                                   """);

        Check.That(Names(outcome)).ContainsExactly("one");
    }

    [Theory(DisplayName = "A type nothing can construct is refused, and says which.")]
    [InlineData("public sealed class Subject { private Subject(int one) { } }")]
    [InlineData("public sealed class Subject { public Subject(ref int one) { } }")]
    [InlineData("public static class Subject { }")]
    public void ATypeNothingCanConstructIsRefused(string declaration) {
        Check.That(Subject.Scaffold(declaration).Status).IsEqualTo(ScaffoldStatus.NoEligibleConstructor);
    }

    // A record needs no special handling: its primary constructor is an ordinary public one, and the copy
    // constructor the compiler adds is protected, so it never competes.
    [Fact(DisplayName = "A positional record scaffolds from its primary constructor.")]
    public void APositionalRecordScaffoldsFromItsPrimaryConstructor() {
        ScaffoldOutcome outcome = Subject.Scaffold("public sealed record Subject(string Street, string City);");

        Check.That(Names(outcome)).ContainsExactly("Street", "City");
    }

    /// <summary>
    ///     A parameterless constructor is not a failure: the generator it yields is still an
    ///     <c>IAny&lt;T&gt;</c>, so it composes into <c>Any.ListOf(…)</c> and <c>Any.Combine(…)</c>, which
    ///     <c>new Subject()</c> does not (§4.2).
    /// </summary>
    [Fact(DisplayName = "A parameterless constructor yields the degenerate generator.")]
    public void AParameterlessConstructorYieldsTheDegenerateGenerator() {
        ScaffoldOutcome outcome = Subject.Scaffold("public sealed class Subject { public Subject() { } }");

        Check.That(outcome.Succeeded).IsTrue();
        Check.That(outcome.Plan!.IsDegenerate).IsTrue();
        Check.That(outcome.File!.SourceText).Contains("return new Subject();");
    }

    private static string[] Names(ScaffoldOutcome outcome) {
        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        return outcome.Plan!.Parameters.Select(parameter => parameter.Name).ToArray();
    }

}
