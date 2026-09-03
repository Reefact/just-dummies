using NFluent;

namespace JustDummies.GenDummy.UnitTests;

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
    [InlineData("string", "Dummy.String().NonEmpty()")]
    [InlineData("bool", "Dummy.Boolean()")]
    [InlineData("sbyte", "Dummy.SByte()")]
    [InlineData("byte", "Dummy.Byte()")]
    [InlineData("short", "Dummy.Int16()")]
    [InlineData("ushort", "Dummy.UInt16()")]
    [InlineData("int", "Dummy.Int32()")]
    [InlineData("uint", "Dummy.UInt32()")]
    [InlineData("long", "Dummy.Int64()")]
    [InlineData("ulong", "Dummy.UInt64()")]
    [InlineData("float", "Dummy.Single()")]
    [InlineData("double", "Dummy.Double()")]
    [InlineData("decimal", "Dummy.Decimal()")]
    [InlineData("char", "Dummy.Char()")]
    [InlineData("Guid", "Dummy.Guid().NonEmpty()")]
    [InlineData("DateTime", "Dummy.DateTime()")]
    [InlineData("DateTimeOffset", "Dummy.DateTimeOffset()")]
    [InlineData("TimeSpan", "Dummy.TimeSpan()")]
    [InlineData("Uri", "Dummy.Uri().Web()")]
    [InlineData("OrderStatus", "Dummy.Enum<OrderStatus>()")]
    public void TheBaseTableDrawsEveryScalar(string parameterType, string expected) {
        Check.That(Subject.ExpressionFor(parameterType)).IsEqualTo(expected);
    }

    // Unconstrained, Dummy.String() draws zero to sixteen letters and digits — it can return the empty string
    // (§14.5) — and a string parameter of a domain type is overwhelmingly required non-empty. A default that
    // fails about one call in seventeen is the flakiness the library exists to remove. Same for Guid.Empty.
    [Theory(DisplayName = "The two rows that refuse an empty draw say so.")]
    [InlineData("string")]
    [InlineData("Guid")]
    public void TheTwoRowsThatRefuseAnEmptyDrawSaySo(string parameterType) {
        Check.That(Subject.ExpressionFor(parameterType)).Contains(".NonEmpty()");
    }

    [Theory(DisplayName = "The base table draws every collection through its element.")]
    [InlineData("int[]", "Dummy.ArrayOf(Dummy.Int32())")]
    [InlineData("List<string>", "Dummy.ListOf(Dummy.String().NonEmpty())")]
    [InlineData("IList<int>", "Dummy.ListOf(Dummy.Int32())")]
    [InlineData("IReadOnlyList<int>", "Dummy.ListOf(Dummy.Int32())")]
    [InlineData("ICollection<int>", "Dummy.ListOf(Dummy.Int32())")]
    [InlineData("IReadOnlyCollection<int>", "Dummy.ListOf(Dummy.Int32())")]
    [InlineData("IEnumerable<int>", "Dummy.SequenceOf(Dummy.Int32())")]
    [InlineData("HashSet<int>", "Dummy.SetOf(Dummy.Int32())")]
    [InlineData("ISet<int>", "Dummy.SetOf(Dummy.Int32())")]
    [InlineData("Dictionary<string, int>", "Dummy.DictionaryOf(Dummy.String().NonEmpty(), Dummy.Int32())")]
    [InlineData("IDictionary<string, int>", "Dummy.DictionaryOf(Dummy.String().NonEmpty(), Dummy.Int32())")]
    [InlineData("IReadOnlyDictionary<string, int>", "Dummy.DictionaryOf(Dummy.String().NonEmpty(), Dummy.Int32())")]
    public void TheBaseTableDrawsEveryCollection(string parameterType, string expected) {
        Check.That(Subject.ExpressionFor(parameterType)).IsEqualTo(expected);
    }

    /// <summary>
    ///     The two nullable rows differ, and the difference is not cosmetic.
    /// </summary>
    /// <remarks>
    ///     Variance in C# applies across reference conversions only. <c>IDummy&lt;string&gt;</c> already is an
    ///     <c>IDummy&lt;string?&gt;</c>; <c>IDummy&lt;int&gt;</c> is <b>not</b> an <c>IDummy&lt;int?&gt;</c>, so the
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
    [InlineData("string?", "Dummy.String().NonEmpty()")]
    [InlineData("Customer?", "new DummyCustomer()")]
    [InlineData("int?", "Dummy.Int32().AsNullable()")]
    [InlineData("DateTime?", "Dummy.DateTime().AsNullable()")]
    [InlineData("OrderStatus?", "Dummy.Enum<OrderStatus>().AsNullable()")]
    public void ANullableValueTypeNeedsTheExplicitHop(string parameterType, string? expected) {
        Check.That(Subject.ExpressionFor(parameterType)).IsEqualTo(expected);
    }

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
        Check.That(outcome.Plan!.Parameters.Single().Expression).IsEqualTo("Dummy.Int32().As(value => (int?)value)");
    }

    /// <summary>
    ///     A member of the right name and arity, and of the wrong shape, is not the lift.
    /// </summary>
    /// <remarks>
    ///     The version-skew invariant read the other way round. The row asks the compilation whether
    ///     <c>AsNullable</c> resolves <b>for an <c>IDummy&lt;T&gt;</c></b>, and a check reading only the name, the
    ///     visibility and the parameter count would answer yes to anything that happened to be spelled that way —
    ///     then emit a call the developer's own build rejects. ADR-0059 is not "a member of that name exists"; it
    ///     is "this expression resolves here".
    ///     <para>
    ///         The decoy below is deliberately plausible rather than absurd: static, public, one parameter, an
    ///         extension method, the exact name. Only its receiver is wrong.
    ///     </para>
    /// </remarks>
    [Fact(DisplayName = "A member named AsNullable that does not take an IDummy is not the lift.")]
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
        Check.That(outcome.Plan!.Parameters.Single().Expression).IsEqualTo("Dummy.Int32().As(value => (int?)value)");
    }

    /// <summary>Just enough of the library for §5.2 to answer: the façade, the recipe, and the general hop.</summary>
    private const string StubbedLibrary = """
                                          namespace JustDummies {

                                              public interface IDummy<out T> { T Generate(); }

                                              public sealed class DummyInt32 : IDummy<int> { public int Generate() { return 0; } }

                                              public static class Dummy {
                                                  public static DummyInt32 Int32() { return new DummyInt32(); }
                                              }

                                              public static class DummyExtensions {
                                                  public static IDummy<TResult> As<TSource, TResult>(this IDummy<TSource> generator,
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
    ///     <c>Dummy.SetOf(…)</c> is typed <c>IDummy&lt;HashSet&lt;T&gt;&gt;</c> and <c>Dummy.ListOf(…)</c>
    ///     <c>IDummy&lt;List&lt;T&gt;&gt;</c>, so a collection OF one of those carries the concrete type where the
    ///     parameter declared the interface. Covariance settles it at the top level and says nothing here:
    ///     <c>IDummy&lt;out T&gt;</c> converts <c>IDummy&lt;X&gt;</c> to <c>IDummy&lt;Y&gt;</c> when <c>X</c> converts
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
    [InlineData("List<ISet<int>>", "Dummy.ListOf(Dummy.SetOf(Dummy.Int32()).As(value => (ISet<int>)value))")]
    [InlineData("ICollection<IReadOnlyList<int>>", "Dummy.ListOf(Dummy.ListOf(Dummy.Int32()).As(value => (IReadOnlyList<int>)value))")]
    [InlineData("HashSet<IList<int>>", "Dummy.SetOf(Dummy.ListOf(Dummy.Int32()).As(value => (IList<int>)value))")]
    [InlineData("Dictionary<int, ISet<int>>", "Dummy.DictionaryOf(Dummy.Int32(), Dummy.SetOf(Dummy.Int32()).As(value => (ISet<int>)value))")]
    [InlineData("IReadOnlyList<ISet<int>>", "Dummy.ListOf(Dummy.SetOf(Dummy.Int32()))")]
    [InlineData("ISet<int>[]", "Dummy.ArrayOf(Dummy.SetOf(Dummy.Int32()))")]
    [InlineData("IEnumerable<IReadOnlyList<int>>", "Dummy.SequenceOf(Dummy.ListOf(Dummy.Int32()))")]
    public void AnElementIsConvertedWhereTheOuterTypeWillNot(string parameterType, string expected) {
        Check.That(Subject.ExpressionFor(parameterType)).IsEqualTo(expected);
    }

    [Theory(DisplayName = "Element generators recurse, three hops deep and no further.")]
    [InlineData("IReadOnlyList<int[]>", "Dummy.ListOf(Dummy.ArrayOf(Dummy.Int32()))")]
    [InlineData("IReadOnlyList<Dictionary<string, int[]>>",
                "Dummy.ListOf(Dummy.DictionaryOf(Dummy.String().NonEmpty(), Dummy.ArrayOf(Dummy.Int32())))")]
    [InlineData("IReadOnlyList<IReadOnlyList<IReadOnlyList<int>>>", "Dummy.ListOf(Dummy.ListOf(Dummy.ListOf(Dummy.Int32())))")]
    [InlineData("IReadOnlyList<IReadOnlyList<IReadOnlyList<IReadOnlyList<int>>>>", null)]
    public void ElementGeneratorsRecurseToTheDepthBound(string parameterType, string? expected) {
        Check.That(Subject.ExpressionFor(parameterType)).IsEqualTo(expected);
    }

    // A domain type has no row here, and does not need one: §5.4 draws it through the generator that type
    // owns, named whether the compilation carries it yet or not (ADR-0089). Nested inside a collection or not
    // makes no difference — the element goes through the same door.
    [Theory(DisplayName = "A type the table has no row for is handed to composition, not left open.")]
    [InlineData("Customer", "new DummyCustomer()")]
    [InlineData("IReadOnlyList<Customer>", "Dummy.ListOf(new DummyCustomer())")]
    public void ATypeTheTableHasNoRowForIsHandedToComposition(string parameterType, string expected) {
        Check.That(Subject.ExpressionFor(parameterType)).IsEqualTo(expected);
    }

}
