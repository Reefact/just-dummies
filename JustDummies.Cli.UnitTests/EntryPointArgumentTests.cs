using JustDummies.GenDummy;

using NFluent;

using Spectre.Console;

namespace JustDummies.Cli.UnitTests;

/// <summary>
///     <c>--entry-point</c> and <c>--entry-point-namespace</c>, read before anything is opened (§3, §4.5).
/// </summary>
/// <remarks>
///     Every case here is a <b>command line</b> that could not be read, which is exit <c>2</c> and not one of
///     §7's scaffolding failures — the tool has not looked at a project yet. What each refusal has to carry is
///     the same thing every other refusal carries: what could not be done, and then what to do instead.
/// </remarks>
public sealed class EntryPointArgumentTests {

    [Fact(DisplayName = "Omitted, the option asks for nothing — which is what a scaffold does by default.")]
    public void OmittedAsksForNothing() {
        EntryPointArgument read = EntryPointArgument.Parse(entryPoint: null, entryPointNamespace: null);

        Check.That(read.Understood).IsTrue();
        Check.That(read.Options.Kind).IsEqualTo(EntryPointKind.None);
    }

    [Fact(DisplayName = "'none' says out loud what omitting it means.")]
    public void NoneSaysItOutLoud() {
        Check.That(Parsed("none").Kind).IsEqualTo(EntryPointKind.None);
    }

    [Fact(DisplayName = "'any' hangs the entry point off the library's own façade.")]
    public void DummyHangsItOffTheLibrary() {
        EntryPointOptions options = Parsed("any");

        Check.That(options.Kind).IsEqualTo(EntryPointKind.Dummy);
        Check.That(options.Root).IsEqualTo("Dummy");
    }

    [Fact(DisplayName = "'static:<Name>' names a root the developer owns.")]
    public void StaticNamesARootTheDeveloperOwns() {
        EntryPointOptions options = Parsed("static:Dummies");

        Check.That(options.Kind).IsEqualTo(EntryPointKind.StaticRoot);
        Check.That(options.Root).IsEqualTo("Dummies");
    }

    /// <summary>
    ///     The one refusal this option exists to make: a static class named <c>Dummy</c> hides the library's
    ///     façade for its whole namespace rather than extending it, and <c>Dummy.Int32()</c> stops compiling.
    /// </summary>
    /// <remarks>
    ///     Refused rather than warned, unlike the shadowing row of §7: that one compiles and is wrong later,
    ///     this one does not compile at all, and the developer asking for it is asking for
    ///     <c>--entry-point any</c> by another name.
    /// </remarks>
    [Fact(DisplayName = "'static:Dummy' is refused, and the refusal names what would stop compiling.")]
    public void StaticAnyIsRefused() {
        EntryPointArgument read = EntryPointArgument.Parse("static:Dummy", entryPointNamespace: null);

        Check.That(read.Understood).IsFalse();
        Check.That(read.Refusal).Contains("Dummy.Int32()");
        Check.That(read.Refusal).Contains("--entry-point any");
    }

    [Theory(DisplayName = "A root that is not an identifier is refused, by name.")]
    [InlineData("static:2Dummies")]
    [InlineData("static:my dummies")]
    [InlineData("static:class")]
    [InlineData("static:")]
    public void ARootThatIsNotAnIdentifierIsRefused(string value) {
        EntryPointArgument read = EntryPointArgument.Parse(value, entryPointNamespace: null);

        Check.That(read.Understood).IsFalse();
        Check.That(read.Options.Kind).IsEqualTo(EntryPointKind.None);
    }

    [Fact(DisplayName = "An unknown value is refused, listing the three that are not.")]
    public void AnUnknownValueIsRefused() {
        EntryPointArgument read = EntryPointArgument.Parse("dummies", entryPointNamespace: null);

        Check.That(read.Understood).IsFalse();
        Check.That(read.Refusal).Contains("none");
        Check.That(read.Refusal).Contains("static:<Name>");
        Check.That(read.Refusal).Contains("any");
    }

    // Not ignored: a namespace given with nothing to place is a command line whose author expects a file
    // somewhere, and would go looking for it.
    [Theory(DisplayName = "A namespace with no entry point to place is refused, not ignored.")]
    [InlineData(null)]
    [InlineData("none")]
    public void ANamespaceWithNothingToPlaceIsRefused(string? entryPoint) {
        EntryPointArgument read = EntryPointArgument.Parse(entryPoint, "Shop.Tests.Dummies");

        Check.That(read.Understood).IsFalse();
        Check.That(read.Refusal).Contains("--entry-point");
    }

    [Fact(DisplayName = "A namespace is carried through to the options.")]
    public void ANamespaceIsCarriedThrough() {
        EntryPointArgument read = EntryPointArgument.Parse("any", "Shop.Tests.Dummies");

        Check.That(read.Understood).IsTrue();
        Check.That(read.Options.NamespaceOverride).IsEqualTo("Shop.Tests.Dummies");
    }

    [Fact(DisplayName = "The settings refuse an unreadable pair before the command runs.")]
    public void TheSettingsRefuseAnUnreadablePair() {
        GenerateSettings settings = new() { Types = ["Order"], EntryPoint = "static:Dummy" };

        Check.That(settings.Validate().Successful).IsFalse();
    }

    [Theory(DisplayName = "Either option given without a value is refused, like every other option (§3).")]
    [InlineData("--entry-point")]
    [InlineData("--entry-point-namespace")]
    public void EitherOptionGivenWithoutAValueIsRefused(string option) {
        GenerateSettings settings = new() { Types = ["Order"] };

        if (option == "--entry-point") { settings.EntryPoint = "  "; } else { settings.EntryPointNamespace = "  "; }

        ValidationResult result = settings.Validate();

        Check.That(result.Successful).IsFalse();
        Check.That(result.Message).Contains(option);
    }

    [Fact(DisplayName = "A readable pair passes validation.")]
    public void AReadablePairPassesValidation() {
        GenerateSettings settings = new() {
            Types               = ["Order"],
            EntryPoint          = "static:Dummies",
            EntryPointNamespace = "Shop.Tests.Dummies"
        };

        Check.That(settings.Validate().Successful).IsTrue();
    }

    private static EntryPointOptions Parsed(string entryPoint) {
        EntryPointArgument read = EntryPointArgument.Parse(entryPoint, entryPointNamespace: null);

        Check.That(read.Understood).IsTrue();

        return read.Options;
    }

}
