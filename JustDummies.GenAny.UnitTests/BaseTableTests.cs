using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     Every row of §5.2, read off a real compilation.
/// </summary>
/// <remarks>
///     One case per row rather than a few representative ones, because the rows are not variations of each
///     other: the collection rows lean on covariance, the nullable value-type row exists precisely because
///     covariance does not reach it, and the modern rows exist only on one of the library's two assets. A
///     sample would cover the easy half.
/// </remarks>
public sealed class BaseTableTests {

    [Theory(DisplayName = "The base table draws every scalar the library has a factory for.")]
    [InlineData("string", "Any.String()")]
    [InlineData("bool", "Any.Boolean()")]
    [InlineData("sbyte", "Any.SByte()")]
    [InlineData("byte", "Any.Byte()")]
    [InlineData("short", "Any.Int16()")]
    [InlineData("ushort", "Any.UInt16()")]
    [InlineData("int", "Any.Int32()")]
    [InlineData("uint", "Any.UInt32()")]
    [InlineData("long", "Any.Int64()")]
    [InlineData("ulong", "Any.UInt64()")]
    [InlineData("float", "Any.Single()")]
    [InlineData("double", "Any.Double()")]
    [InlineData("decimal", "Any.Decimal()")]
    [InlineData("char", "Any.Char()")]
    [InlineData("Guid", "Any.Guid()")]
    [InlineData("DateTime", "Any.DateTime()")]
    [InlineData("DateTimeOffset", "Any.DateTimeOffset()")]
    [InlineData("TimeSpan", "Any.TimeSpan()")]
    [InlineData("Uri", "Any.Uri().Web()")]
    [InlineData("OrderStatus", "Any.Enum<OrderStatus>()")]
    public void TheBaseTableDrawsEveryScalar(string parameterType, string expected) {
        Check.That(Subject.ExpressionFor(parameterType)).IsEqualTo(expected);
    }

    // A constructor that refuses the empty value says so in a guard the engine reads (§5.3) or marks to verify
    // (§5.6); one that accepts it accepts a valid dummy — the empty allele of an insertion, say. The base table
    // guesses no rule the constructor does not write (#409).
    [Theory(DisplayName = "No row refuses an empty draw the constructor does not refuse.")]
    [InlineData("string")]
    [InlineData("Guid")]
    public void NoRowRefusesAnEmptyDrawTheConstructorDoesNotRefuse(string parameterType) {
        Check.That(Subject.ExpressionFor(parameterType)).Not.Contains(".NonEmpty()");
    }

    [Theory(DisplayName = "The base table draws every collection through its element.")]
    [InlineData("int[]", "Any.ArrayOf(Any.Int32())")]
    [InlineData("List<string>", "Any.ListOf(Any.String())")]
    [InlineData("IList<int>", "Any.ListOf(Any.Int32())")]
    [InlineData("IReadOnlyList<int>", "Any.ListOf(Any.Int32())")]
    [InlineData("ICollection<int>", "Any.ListOf(Any.Int32())")]
    [InlineData("IReadOnlyCollection<int>", "Any.ListOf(Any.Int32())")]
    [InlineData("IEnumerable<int>", "Any.SequenceOf(Any.Int32())")]
    [InlineData("HashSet<int>", "Any.SetOf(Any.Int32())")]
    [InlineData("ISet<int>", "Any.SetOf(Any.Int32())")]
    [InlineData("Dictionary<string, int>", "Any.DictionaryOf(Any.String(), Any.Int32())")]
    [InlineData("IDictionary<string, int>", "Any.DictionaryOf(Any.String(), Any.Int32())")]
    [InlineData("IReadOnlyDictionary<string, int>", "Any.DictionaryOf(Any.String(), Any.Int32())")]
    public void TheBaseTableDrawsEveryCollection(string parameterType, string expected) {
        Check.That(Subject.ExpressionFor(parameterType)).IsEqualTo(expected);
    }

    /// <summary>
    ///     The two nullable rows differ, and the difference is not cosmetic.
    /// </summary>
    /// <remarks>
    ///     Variance in C# applies across reference conversions only. <c>IAny&lt;string&gt;</c> already is an
    ///     <c>IAny&lt;string?&gt;</c>; <c>IAny&lt;int&gt;</c> is <b>not</b> an <c>IAny&lt;int?&gt;</c>, so the
    ///     value-type row has to write the conversion. Getting this wrong is the likeliest way to produce a
    ///     table that does not compile — and neither row is ever <c>.OrNull()</c> (ADR-0064).
    ///     <para>
    ///         The conversion is <c>AsNullable()</c> and not the general <c>As</c> hop, which is what it used to
    ///         be. A derived generator advertises no cardinality, so a <b>distinct</b> collection over one drew a
    ///         count its element domain could not fill and refused a set of two <c>bool?</c>; the lift keeps the
    ///         count. The theory below pins the other half — an asset without the lift still gets the hop that
    ///         always worked (ADR-0059).
    ///     </para>
    /// </remarks>
    [Theory(DisplayName = "A nullable reference type needs no hop; a nullable value type does.")]
    [InlineData("string?", "Any.String()")]
    [InlineData("Customer?", "new AnyCustomer()")]
    [InlineData("int?", "Any.Int32().AsNullable()")]
    [InlineData("DateTime?", "Any.DateTime().AsNullable()")]
    [InlineData("OrderStatus?", "Any.Enum<OrderStatus>().AsNullable()")]
    public void ANullableValueTypeNeedsTheExplicitHop(string parameterType, string? expected) {
        Check.That(Subject.ExpressionFor(parameterType)).IsEqualTo(expected);
    }

    /// <summary>
    ///     A nullable reference parameter keeps its <c>?</c> where a test meets it: on the pin, so the opt-in
    ///     ADR-0064 and ADR-0109 promise compiles without a warning.
    /// </summary>
    /// <remarks>
    ///     The default still never draws null; what changes is that <c>WithNote((string?)null)</c> no longer
    ///     warns <c>CS8625</c> — a build break under <c>TreatWarningsAsErrors</c> — for passing the null the
    ///     constructor itself accepts. Covariance keeps the never-null base assignable to the <c>T?</c> field.
    /// </remarks>
    [Fact(DisplayName = "A nullable reference parameter is pinned as T?, so a test can pass null.")]
    public void ANullableReferenceParameterIsPinnedAsNullable() {
        const string declarations = """
                                    namespace Shop.Domain;

                                    public sealed class Subject {
                                        public Subject(string? note, Customer? agent) { }
                                    }
                                    """;
        const string caller = """
                              namespace Shop.Tests;

                              public static class Caller {
                                  public static Shop.Domain.Subject WithoutANote() {
                                      return new Shop.Domain.AnySubject().WithNote((string?)null).Generate();
                                  }
                              }
                              """;

        ScaffoldOutcome outcome = Subject.Scaffold(declarations);
        string          emitted = outcome.File!.SourceText;

        Check.That(outcome.Plan!.Parameters.Select(parameter => parameter.TypeDisplay)).ContainsExactly("string?", "Customer?");
        Check.That(emitted).Contains("public AnySubject WithNote(string? value) {");

        CSharpCompilation compilation = EmittedCodeCompiler.CompileAllWith([emitted, caller], declarations + AnyCustomer);

        compilation = compilation.WithOptions(compilation.Options.WithNullableContextOptions(NullableContextOptions.Enable));

        Check.That(compilation.GetDiagnostics(TestContext.Current.CancellationToken)
                              .Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning)
                              .Select(diagnostic => diagnostic.Id + ": " + diagnostic.GetMessage())).IsEmpty();
    }

    // Outside a nullable annotation context a `?` on a reference type is itself a warning (CS8632), and nothing
    // there warns about passing null either: the annotation is written only where it means something.
    [Fact(DisplayName = "Without nullable annotations, a reference parameter is pinned as it always was.")]
    public void WithoutNullableAnnotationsAReferenceParameterIsPinnedAsItWas() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   namespace Shop.Domain;

                                                   public sealed class Subject {
                                                       public Subject(string? note) { }
                                                   }
                                                   """, nullable: NullableContextOptions.Disable);

        Check.That(outcome.File!.SourceText).Contains("public AnySubject WithNote(string value) {");
    }

    // The file is right not to carry the `?`, and the recap describes the file; what it still owes the reader is that
    // the declaration had one (§6), so the parameter says so — and only where the annotation was really dropped.
    [Fact(DisplayName = "A nullable reference parameter spelled without its ? says so, for the recap to note once.")]
    public void ANullableReferenceSpelledWithoutItsAnnotationSaysSo() {
        const string declarations = """
                                    namespace Shop.Domain;

                                    public sealed class Subject {
                                        public Subject(string? note, string name, int? priority) { }
                                    }
                                    """;

        ScaffoldOutcome withoutAnnotations = Subject.Scaffold(declarations, nullable: NullableContextOptions.Disable);
        ScaffoldOutcome withAnnotations    = Subject.Scaffold(declarations);

        Check.That(withoutAnnotations.Plan!.Parameters.Select(parameter => parameter.NullableAnnotationDropped)).ContainsExactly(true, false, false);
        Check.That(withAnnotations.Plan!.Parameters.Select(parameter => parameter.NullableAnnotationDropped)).ContainsExactly(false, false, false);
    }

    /// <summary>The fixture's <c>Customer</c> and a stand-in for its own scaffolded generator.</summary>
    private const string AnyCustomer = """

                                       public sealed class Customer {
                                           public Customer(string name) { }
                                       }

                                       public sealed class AnyCustomer : JustDummies.IAny<Customer> {
                                           public Customer Generate() { return new Customer("name"); }
                                       }
                                       """;

    /// <summary>
    ///     A library without the lift gets the hop that always worked, rather than a member it cannot resolve.
    /// </summary>
    /// <remarks>
    ///     ADR-0059 in the one shape the real assets cannot exercise: both legs of this build carry
    ///     <c>AsNullable</c>, so the fallback is only reachable against a compilation that does not — a consumer
    ///     who upgraded the tool without upgrading the package. The surface is declared in source here for that
    ///     reason, with no JustDummies assembly referenced at all, which is exactly what the engine would meet
    ///     there.
    /// </remarks>
    [Fact(DisplayName = "A library carrying no AsNullable falls back to the general conversion hop.")]
    public void ALibraryWithoutTheLiftFallsBackToTheGeneralHop() {
        ScaffoldOutcome outcome = Subject.Scaffold($$"""
                                                   {{StubbedLibrary}}

                                                   namespace Shop.Domain {
                                                       public sealed class Subject {
                                                           public Subject(int? value) { }
                                                       }
                                                   }
                                                   """,
                                                   withLibrary: false);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);
        Check.That(outcome.Plan!.Parameters.Single().Expression).IsEqualTo("Any.Int32().As(value => (int?)value)");
    }

    /// <summary>
    ///     A member of the right name and arity, and of the wrong shape, is not the lift.
    /// </summary>
    /// <remarks>
    ///     The version-skew invariant read the other way round. The row asks the compilation whether
    ///     <c>AsNullable</c> resolves <b>for an <c>IAny&lt;T&gt;</c></b>, and a check reading only the name, the
    ///     visibility and the parameter count would answer yes to anything that happened to be spelled that way —
    ///     then emit a call the developer's own build rejects. ADR-0059 is not "a member of that name exists"; it
    ///     is "this expression resolves here".
    ///     <para>
    ///         The decoy below is deliberately plausible rather than absurd: static, public, one parameter, an
    ///         extension method, the exact name. Only its receiver is wrong.
    ///     </para>
    /// </remarks>
    [Fact(DisplayName = "A member named AsNullable that does not take an IAny is not the lift.")]
    public void AMemberOfTheRightNameAndTheWrongShapeIsNotTheLift() {
        ScaffoldOutcome outcome = Subject.Scaffold($$"""
                                                   {{StubbedLibrary}}

                                                   {{DecoyLift}}

                                                   namespace Shop.Domain {
                                                       public sealed class Subject {
                                                           public Subject(int? value) { }
                                                       }
                                                   }
                                                   """,
                                                   withLibrary: false);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);
        Check.That(outcome.Plan!.Parameters.Single().Expression).IsEqualTo("Any.Int32().As(value => (int?)value)");
    }

    /// <summary>Just enough of the library for §5.2 to answer: the façade, the recipe, and the general hop.</summary>
    private const string StubbedLibrary = """
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

    /// <summary>An <c>AsNullable</c> the emitted call could not bind to, in the place the real one lives.</summary>
    private const string DecoyLift = """
                                     namespace JustDummies {

                                         public static class NullableExtensions {
                                             public static string AsNullable(this string value) { return value; }
                                         }

                                     }
                                     """;

    /// <summary>
    ///     An element the outer type will not convert on its own carries the conversion; one it will, does not.
    /// </summary>
    /// <remarks>
    ///     <c>Any.SetOf(…)</c> is typed <c>IAny&lt;HashSet&lt;T&gt;&gt;</c> and <c>Any.ListOf(…)</c>
    ///     <c>IAny&lt;List&lt;T&gt;&gt;</c>, so a collection OF one of those carries the concrete type where the
    ///     parameter declared the interface. Covariance settles it at the top level and says nothing here:
    ///     <c>IAny&lt;out T&gt;</c> converts <c>IAny&lt;X&gt;</c> to <c>IAny&lt;Y&gt;</c> when <c>X</c> converts
    ///     to <c>Y</c>, and <c>List&lt;HashSet&lt;T&gt;&gt;</c> never converts to <c>List&lt;ISet&lt;T&gt;&gt;</c>.
    ///     Without the hop the emitted file failed on a plain <c>CS0029</c>, with no sentinel over it — the one
    ///     thing ADR-0083 says must not happen.
    ///     <para>
    ///         The covariant rows are here for the other half of the rule. Writing the hop everywhere would
    ///         compile too, and would make three nested read-only lists carry two casts that change nothing, in
    ///         a file the developer reads and owns.
    ///     </para>
    /// </remarks>
    [Theory(DisplayName = "A collection element is converted where the outer type will not convert it.")]
    [InlineData("List<ISet<int>>", "Any.ListOf(Any.SetOf(Any.Int32()).As(value => (ISet<int>)value))")]
    [InlineData("ICollection<IReadOnlyList<int>>", "Any.ListOf(Any.ListOf(Any.Int32()).As(value => (IReadOnlyList<int>)value))")]
    [InlineData("HashSet<IList<int>>", "Any.SetOf(Any.ListOf(Any.Int32()).As(value => (IList<int>)value))")]
    [InlineData("Dictionary<int, ISet<int>>", "Any.DictionaryOf(Any.Int32(), Any.SetOf(Any.Int32()).As(value => (ISet<int>)value))")]
    [InlineData("IReadOnlyList<ISet<int>>", "Any.ListOf(Any.SetOf(Any.Int32()))")]
    [InlineData("ISet<int>[]", "Any.ArrayOf(Any.SetOf(Any.Int32()))")]
    [InlineData("IEnumerable<IReadOnlyList<int>>", "Any.SequenceOf(Any.ListOf(Any.Int32()))")]
    public void AnElementIsConvertedWhereTheOuterTypeWillNot(string parameterType, string expected) {
        Check.That(Subject.ExpressionFor(parameterType)).IsEqualTo(expected);
    }

    [Theory(DisplayName = "Element generators recurse, three hops deep and no further.")]
    [InlineData("IReadOnlyList<int[]>", "Any.ListOf(Any.ArrayOf(Any.Int32()))")]
    [InlineData("IReadOnlyList<Dictionary<string, int[]>>",
                "Any.ListOf(Any.DictionaryOf(Any.String(), Any.ArrayOf(Any.Int32())))")]
    [InlineData("IReadOnlyList<IReadOnlyList<IReadOnlyList<int>>>", "Any.ListOf(Any.ListOf(Any.ListOf(Any.Int32())))")]
    [InlineData("IReadOnlyList<IReadOnlyList<IReadOnlyList<IReadOnlyList<int>>>>", null)]
    public void ElementGeneratorsRecurseToTheDepthBound(string parameterType, string? expected) {
        Check.That(Subject.ExpressionFor(parameterType)).IsEqualTo(expected);
    }

    // A domain type has no row here, and does not need one: §5.4 draws it through the generator that type
    // owns, named whether the compilation carries it yet or not (ADR-0089). Nested inside a collection or not
    // makes no difference — the element goes through the same door.
    [Theory(DisplayName = "A type the table has no row for is handed to composition, not left open.")]
    [InlineData("Customer", "new AnyCustomer()")]
    [InlineData("IReadOnlyList<Customer>", "Any.ListOf(new AnyCustomer())")]
    public void ATypeTheTableHasNoRowForIsHandedToComposition(string parameterType, string expected) {
        Check.That(Subject.ExpressionFor(parameterType)).IsEqualTo(expected);
    }

}
