#region Usings declarations

using System.Reflection;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     <c>Except(IEnumerable&lt;T&gt;)</c>, the sequence form of every exclusion: a set the test keeps passes as it is,
///     with the array form's semantics — an empty sequence included, which is the no-op of ADR-0122.
/// </summary>
public sealed class ExceptSequenceTests {

    [Fact(DisplayName = "Every builder carrying Except(params T[]) carries Except(IEnumerable<T>) too.")]
    public void EveryBuilderCarriesTheSequenceForm() {
        List<string> missing = [];
        foreach (Type type in typeof(Any).Assembly.GetExportedTypes()) {
            MethodInfo? array = type.GetMethods().FirstOrDefault(method => method.Name == "Except" && method.GetParameters() is { Length: 1 } parameters && parameters[0].ParameterType.IsArray);
            if (array is null) { continue; }

            Type element  = array.GetParameters()[0].ParameterType.GetElementType()!;
            Type sequence = typeof(IEnumerable<>).MakeGenericType(element);
            if (type.GetMethod("Except", [sequence]) is null) { missing.Add(type.Name); }
        }

        Check.That(missing).IsEmpty();
    }

    [Fact(DisplayName = "A set kept by the test excludes its values.")]
    public void ASetKeptByTheTestExcludesItsValues() {
        AnyContext   any  = Any.WithSeed(20261101);
        HashSet<int> used = [];
        for (int i = 0; i < 6; i++) { used.Add(any.Int32().Between(1, 6).Except(used).Generate()); }

        Check.That(used).IsOnlyMadeOf(1, 2, 3, 4, 5, 6).And.HasSize(6);
    }

    [Fact(DisplayName = "An empty sequence is a no-op, as an empty array is.")]
    public void AnEmptySequenceIsANoOp() {
        int plain     = Any.WithSeed(7).Int32().Generate();
        int excepting = Any.WithSeed(7).Int32().Except(new List<int>()).Generate();

        Check.That(excepting).IsEqualTo(plain);
    }

    [Fact(DisplayName = "A lazy query is enumerated once, at declaration.")]
    public void ALazyQueryIsEnumeratedOnce() {
        int                enumerations = 0;
        IEnumerable<string> taken        = Enumerable.Range(0, 3).Select(index => { enumerations++; return "v" + index; });

        AnyString generator = Any.WithSeed(3).String().OneOf("v0", "v1", "v2", "v3").Except(taken);
        for (int i = 0; i < 20; i++) { Check.That(generator.Generate()).IsEqualTo("v3"); }

        Check.That(enumerations).IsEqualTo(3);
    }

    [Fact(DisplayName = "A character builder takes a string as the characters to exclude.")]
    public void ACharacterBuilderTakesAString() {
        AnyContext any = Any.WithSeed(11);
        for (int i = 0; i < 200; i++) { Check.That("aeiou").Not.Contains(any.Char().Alpha().InLowerCase().Except("aeiou").Generate()); }
    }

    [Fact(DisplayName = "A null sequence is refused as an argument.")]
    public void ANullSequenceIsRefused() {
        Check.ThatCode(() => Any.Int32().Except((IEnumerable<int>)null!)).Throws<ArgumentNullException>();
    }

}
