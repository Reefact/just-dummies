using Microsoft.CodeAnalysis.CSharp;

using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     How a pin is written: <c>Any.Fixed(value)</c> where the library the file compiles against carries it, and the
///     nested <c>FixedValue</c> helper where it predates it — ADR-0059 asked of one more member (ADR-0118).
/// </summary>
/// <remarks>
///     The goldens pin the first shape, since the library this suite compiles against carries <c>Any.Fixed</c>. These
///     cases pin the question itself, and the shape a developer on an older library still receives.
/// </remarks>
public sealed class PinTests {

    /// <summary>Just enough of a library that predates <c>Any.Fixed</c> for §5.2 to resolve an <c>int</c>.</summary>
    private const string LibraryWithoutFixed = """
                                               namespace JustDummies {

                                                   public interface IAny<out T> { T Generate(); }

                                                   public sealed class AnyInt32 : IAny<int> { public int Generate() { return 0; } }

                                                   public static class Any {
                                                       public static AnyInt32 Int32() { return new AnyInt32(); }
                                                   }

                                                   public static class AnyExtensions {
                                                       public static IAny<TResult> As<TSource, TResult>(this IAny<TSource> generator,
                                                                                                       System.Func<TSource, TResult> factory) {
                                                           return null!;
                                                       }
                                                   }

                                               }
                                               """;

    private const string Target = """
                                  namespace Shop.Domain {
                                      public sealed class Subject {
                                          public Subject(int value) { }
                                      }
                                  }
                                  """;

    [Fact(DisplayName = "A pin is written Any.Fixed where the library carries it.")]
    public void APinIsWrittenAnyFixedWhereTheLibraryCarriesIt() {
        ScaffoldOutcome outcome = Subject.Scaffold(Target);

        Check.That(outcome.Plan!.PinsThroughAnyFixed).IsTrue();
        Check.That(outcome.File!.SourceText).Contains("= Any.Fixed(value);").And.Not.Contains("FixedValue");
    }

    [Fact(DisplayName = "A library that predates Any.Fixed keeps the nested helper.")]
    public void ALibraryThatPredatesAnyFixedKeepsTheHelper() {
        ScaffoldOutcome outcome = Subject.Scaffold(LibraryWithoutFixed + Target, withLibrary: false);

        Check.That(outcome.Plan!.PinsThroughAnyFixed).IsFalse();
        Check.That(outcome.File!.SourceText).Contains("new FixedValue<int>(value);", "private sealed class FixedValue<TValue> : IAny<TValue>")
             .And.Not.Contains("Any.Fixed(");
    }

    [Fact(DisplayName = "A member named Fixed of the wrong shape is not the pin.")]
    public void AMemberOfTheRightNameAndTheWrongShapeIsNotThePin() {
        // The decoy is static, public and named Fixed, but returns the value rather than a generator: the emitted
        // assignment to an IAny field would not compile, so ADR-0059 must not take it for the factory.
        string decoy = LibraryWithoutFixed.Replace("public static AnyInt32 Int32() { return new AnyInt32(); }",
                                                   "public static AnyInt32 Int32() { return new AnyInt32(); }\n public static T Fixed<T>(T value) { return value; }");

        ScaffoldOutcome outcome = Subject.Scaffold(decoy + Target, withLibrary: false);

        Check.That(outcome.Plan!.PinsThroughAnyFixed).IsFalse();
    }

    [Fact(DisplayName = "The helper shape still compiles against the library, with no error.")]
    public void TheHelperShapeStillCompiles() {
        ScaffoldPlan money = Shapes.Money();
        ScaffoldPlan plan  = new(money.Target, money.GeneratorName, money.Usings, money.Parameters, money.Factory, pinsThroughAnyFixed: false);

        CSharpCompilation compilation = EmittedCodeCompiler.Compile(GeneratorEmitter.Emit(plan).SourceText);

        Check.That(EmittedCodeCompiler.ErrorsIn(compilation)).IsEmpty();
    }

}
