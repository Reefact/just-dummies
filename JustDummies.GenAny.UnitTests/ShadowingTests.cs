using System.Linq;

using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     The one warning a scaffold can carry (§7), and the eight false alarms it does not raise.
/// </summary>
/// <remarks>
///     A domain type named <c>Pattern</c> scaffolds to <c>AnyPattern</c>, which inside its own namespace
///     silently shadows the library's type for every file in that namespace — C# resolves the enclosing
///     namespace before any <c>using</c>. It compiles; it is just wrong later, which is exactly why it is said
///     out loud rather than fixed silently.
/// </remarks>
public sealed class ShadowingTests {

    [Fact(DisplayName = "A name the library already uses is reported, with both types named.")]
    public void ANameTheLibraryUsesIsReported() {
        ScaffoldOutcome outcome = Scaffold("Pattern");

        Check.That(outcome.Succeeded).IsTrue();
        Check.That(outcome.Warnings.Select(warning => warning.Kind)).ContainsExactly(ScaffoldWarningKind.ShadowsLibraryType);
        Check.That(outcome.Warnings[0].Subject).IsEqualTo("AnyPattern");
        Check.That(outcome.Warnings[0].Other).IsEqualTo("JustDummies.AnyPattern");
    }

    /// <summary>
    ///     Arity is part of a type's identity in C#, so the library's eight generic <c>Any*</c> names cannot be
    ///     shadowed by a scaffolded one.
    /// </summary>
    /// <remarks>
    ///     Which matters: <c>Set</c>, <c>List</c> and <c>Sequence</c> are ordinary domain nouns, and warning on
    ///     all forty <c>Any*</c> names would cry wolf on the eight that cannot collide. Verified against the
    ///     library the compilation references rather than against a list here.
    /// </remarks>
    [Theory(DisplayName = "A name that collides only with a generic library type is not reported.")]
    [InlineData("Set")]
    [InlineData("List")]
    [InlineData("Sequence")]
    [InlineData("Array")]
    public void ANameCollidingOnlyWithAGenericTypeIsNotReported(string typeName) {
        Check.That(Scaffold(typeName).Warnings).IsEmpty();
    }

    [Fact(DisplayName = "An ordinary name is reported as nothing at all.")]
    public void AnOrdinaryNameIsReportedAsNothing() {
        Check.That(Scaffold("Basket").Warnings).IsEmpty();
    }

    private static ScaffoldOutcome Scaffold(string typeName) {
        return Subject.Scaffold($$"""
                                 namespace Shop.Domain;

                                 public sealed class {{typeName}} {
                                     public {{typeName}}(string text) { }
                                 }
                                 """,
                                 metadataName: "Shop.Domain." + typeName);
    }

}
