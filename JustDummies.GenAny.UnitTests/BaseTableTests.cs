using System.Diagnostics.CodeAnalysis;

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
    [InlineData("string", "Any.String().NonEmpty()")]
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
    [InlineData("Guid", "Any.Guid().NonEmpty()")]
    [InlineData("DateTime", "Any.DateTime()")]
    [InlineData("DateTimeOffset", "Any.DateTimeOffset()")]
    [InlineData("TimeSpan", "Any.TimeSpan()")]
    [InlineData("Uri", "Any.Uri().Web()")]
    [InlineData("OrderStatus", "Any.Enum<OrderStatus>()")]
    public void TheBaseTableDrawsEveryScalar(string parameterType, string expected) {
        Check.That(Subject.ExpressionFor(parameterType)).IsEqualTo(expected);
    }

    // Unconstrained, Any.String() draws zero to sixteen letters and digits — it can return the empty string
    // (§14.5) — and a string parameter of a domain type is overwhelmingly required non-empty. A default that
    // fails about one call in seventeen is the flakiness the library exists to remove. Same for Guid.Empty.
    [Theory(DisplayName = "The two rows that refuse an empty draw say so.")]
    [InlineData("string")]
    [InlineData("Guid")]
    public void TheTwoRowsThatRefuseAnEmptyDrawSaySo(string parameterType) {
        Check.That(Subject.ExpressionFor(parameterType)).Contains(".NonEmpty()");
    }

    [Theory(DisplayName = "The base table draws every collection through its element.")]
    [InlineData("int[]", "Any.ArrayOf(Any.Int32())")]
    [InlineData("List<string>", "Any.ListOf(Any.String().NonEmpty())")]
    [InlineData("IList<int>", "Any.ListOf(Any.Int32())")]
    [InlineData("IReadOnlyList<int>", "Any.ListOf(Any.Int32())")]
    [InlineData("ICollection<int>", "Any.ListOf(Any.Int32())")]
    [InlineData("IReadOnlyCollection<int>", "Any.ListOf(Any.Int32())")]
    [InlineData("IEnumerable<int>", "Any.SequenceOf(Any.Int32())")]
    [InlineData("HashSet<int>", "Any.SetOf(Any.Int32())")]
    [InlineData("ISet<int>", "Any.SetOf(Any.Int32())")]
    [InlineData("Dictionary<string, int>", "Any.DictionaryOf(Any.String().NonEmpty(), Any.Int32())")]
    [InlineData("IDictionary<string, int>", "Any.DictionaryOf(Any.String().NonEmpty(), Any.Int32())")]
    [InlineData("IReadOnlyDictionary<string, int>", "Any.DictionaryOf(Any.String().NonEmpty(), Any.Int32())")]
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
    /// </remarks>
    [Theory(DisplayName = "A nullable reference type needs no hop; a nullable value type does.")]
    [InlineData("string?", "Any.String().NonEmpty()")]
    [InlineData("Customer?", null)]
    [InlineData("int?", "Any.Int32().As(value => (int?)value)")]
    [InlineData("DateTime?", "Any.DateTime().As(value => (DateTime?)value)")]
    [InlineData("OrderStatus?", "Any.Enum<OrderStatus>().As(value => (OrderStatus?)value)")]
    public void ANullableValueTypeNeedsTheExplicitHop(string parameterType, string? expected) {
        Check.That(Subject.ExpressionFor(parameterType)).IsEqualTo(expected);
    }

    [Theory(DisplayName = "Element generators recurse, three hops deep and no further.")]
    [InlineData("IReadOnlyList<int[]>", "Any.ListOf(Any.ArrayOf(Any.Int32()))")]
    [InlineData("IReadOnlyList<Dictionary<string, int[]>>",
                "Any.ListOf(Any.DictionaryOf(Any.String().NonEmpty(), Any.ArrayOf(Any.Int32())))")]
    [InlineData("IReadOnlyList<IReadOnlyList<IReadOnlyList<int>>>", "Any.ListOf(Any.ListOf(Any.ListOf(Any.Int32())))")]
    [InlineData("IReadOnlyList<IReadOnlyList<IReadOnlyList<IReadOnlyList<int>>>>", null)]
    public void ElementGeneratorsRecurseToTheDepthBound(string parameterType, string? expected) {
        Check.That(Subject.ExpressionFor(parameterType)).IsEqualTo(expected);
    }

    // Until §5.4 composes through a scaffolded generator or a static factory, a domain type has no row. The
    // parameter comes back open, and §5.5 turns that into a TODO the developer's own build reports.
    [SuppressMessage(SonarRule.S1135.Category, SonarRule.S1135.Id,
                     Justification = "Names the marker the tool emits by design (§5.5), not unfinished work here.")]
    [Theory(DisplayName = "A type the table has no row for comes back open.")]
    [InlineData("Customer")]
    [InlineData("IReadOnlyList<Customer>")]
    public void ATypeTheTableHasNoRowForComesBackOpen(string parameterType) {
        Check.That(Subject.ExpressionFor(parameterType)).IsNull();
    }

}
