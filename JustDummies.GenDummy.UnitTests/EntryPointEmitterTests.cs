using System;

using NFluent;

namespace JustDummies.GenDummy.UnitTests;

/// <summary>
///     The entry-point file of §4.5, pinned the same way the generator is: one approved file per shape it can
///     take, each compiled beside the generator it reaches by <see cref="EmittedCodeCompilesTests" />.
/// </summary>
/// <remarks>
///     Four shapes, because four things vary independently: the kind (a static root the developer owns, or an
///     extension member on the library's façade), the namespace form copied from the target, whether the file
///     was moved into a namespace of its own — which is the only case that opens a <c>using</c> — and the
///     global namespace, which has no declaration to copy at all.
/// </remarks>
public sealed class EntryPointEmitterTests {

    [Fact(DisplayName = "A static root, beside the generator it reaches.")]
    public void AStaticRoot() {
        ScaffoldedEntryPoint entry = Emitted(Shapes.Order(), EntryPointOptions.OnStaticRoot("Dummies"));

        GoldenFile.Approve("DummyOrder.Entry.Static", entry.File.SourceText);
        Check.That(entry.Call).IsEqualTo("Dummies.Order()");
    }

    [Fact(DisplayName = "An extension member on the library's own Dummy.")]
    public void AnExtensionMemberOnDummy() {
        ScaffoldedEntryPoint entry = Emitted(Shapes.Order(), EntryPointOptions.OnDummy);

        GoldenFile.Approve("DummyOrder.Entry.Dummy", entry.File.SourceText);
        Check.That(entry.Call).IsEqualTo("Dummy.Order()");
    }

    // The one case that opens a using: the generator stays where ADR-0062 puts it and the entry point does not,
    // so the entry point has to name a type it no longer shares a namespace with.
    [Fact(DisplayName = "A root moved into its own namespace opens the generator's.")]
    public void ARootMovedIntoItsOwnNamespace() {
        ScaffoldedEntryPoint entry = Emitted(Shapes.Pattern(),
                                             EntryPointOptions.OnStaticRoot("Dummies").InNamespace("Shop.Tests.Generators"));

        GoldenFile.Approve("DummyPattern.Entry.Moved", entry.File.SourceText);
        Check.That(entry.Call).IsEqualTo("Dummies.Pattern()");
    }

    [Fact(DisplayName = "The global namespace, which has no declaration to copy.")]
    public void TheGlobalNamespace() {
        ScaffoldedEntryPoint entry = Emitted(Shapes.Session(), EntryPointOptions.OnDummy);

        GoldenFile.Approve("DummySession.Entry.Dummy", entry.File.SourceText);
        Check.That(entry.Call).IsEqualTo("Dummy.Session()");
    }

    [Fact(DisplayName = "The file is named after the generator it reaches.")]
    public void TheFileIsNamedAfterTheGenerator() {
        Check.That(Emitted(Shapes.Order(), EntryPointOptions.OnDummy).File.FileName).IsEqualTo("DummyOrder.Entry.cs");
    }

    // An entry point is a call into a generator that already exists; it can carry no unresolved parameter of
    // its own, so it never blocks the developer's build the way the open parameter of §5.5 deliberately does.
    [Fact(DisplayName = "An entry point never carries a TODO, even for a generator that does.")]
    public void AnEntryPointNeverCarriesATodo() {
        Check.That(Emitted(Shapes.OrderWithTodo(), EntryPointOptions.OnDummy).File.ContainsTodo).IsFalse();
    }

    [Fact(DisplayName = "No entry point asked for, no file emitted.")]
    public void NoEntryPointAskedFor() {
        Check.That(EntryPointEmitter.Emit(Shapes.Order(), EntryPointOptions.None)).IsNull();
    }

    // TargetType.Name is the target as C# reads it from inside its namespace, so a nested type arrives as
    // 'Order.Line' — which is not a method name. The generator is already named after the nested type alone.
    [Fact(DisplayName = "A nested target is reached by its own name, not its container's.")]
    public void ANestedTargetIsReachedByItsOwnName() {
        ScaffoldPlan nested = new(new TargetType("Order.Line", "Shop.Domain", NamespaceStyle.FileScoped),
                                  "DummyLine",
                                  ["JustDummies"],
                                  [ScaffoldedParameter.DrawnFrom("sku", "string", "Dummy.String().NonEmpty()")]);

        ScaffoldedEntryPoint entry = Emitted(nested, EntryPointOptions.OnStaticRoot("Dummies"));

        Check.That(entry.Call).IsEqualTo("Dummies.Line()");
        Check.That(entry.File.SourceText).Contains("public static DummyLine Line() {");
    }

    [Fact(DisplayName = "Every line ends in a single newline, on every platform.")]
    public void EveryLineEndsInASingleNewline() {
        string emitted = Emitted(Shapes.Order(), EntryPointOptions.OnStaticRoot("Dummies")).File.SourceText;

        Check.That(emitted).Not.Contains("\r");
        Check.That(emitted.EndsWith('\n')).IsTrue();
    }

    [Fact(DisplayName = "A plan and a kind are both required.")]
    public void APlanAndAKindAreBothRequired() {
        Check.ThatCode(() => EntryPointEmitter.Emit(null!, EntryPointOptions.OnDummy)).Throws<ArgumentNullException>();
        Check.ThatCode(() => EntryPointEmitter.Emit(Shapes.Order(), null!)).Throws<ArgumentNullException>();
    }

    private static ScaffoldedEntryPoint Emitted(ScaffoldPlan plan, EntryPointOptions entryPoint) {
        return EntryPointEmitter.Emit(plan, entryPoint)!;
    }

}
