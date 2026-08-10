using System;
using System.Globalization;
using System.Threading;

using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     The emitted file of §4, one approved file per shape §12 names.
/// </summary>
/// <remarks>
///     Golden files are what let the emitter be a string builder rather than a syntax API (§11.2): the output
///     has to read like a file a person wrote — aligned declarations, the repository's brace style — and there
///     is no way to assert that other than by reading it. Each approved file is also compiled, by
///     <see cref="EmittedCodeCompilesTests" />, so "it looks right" and "it is right" are two separate checks.
/// </remarks>
public sealed class EmitterGoldenTests {

    [Fact(DisplayName = "Six parameters, all inferred — the worked example of §4.1.")]
    public void SixParametersAllInferred() {
        GoldenFile.Approve("AnyOrder", GeneratorEmitter.Emit(Shapes.Order()).SourceText);
    }

    [Fact(DisplayName = "One parameter left open — the TODO of §5.5.")]
    public void OneParameterLeftOpen() {
        ScaffoldedFile file = GeneratorEmitter.Emit(Shapes.OrderWithTodo());

        GoldenFile.Approve("AnyOrderWithTodo", file.SourceText);
        Check.That(file.ContainsTodo).IsTrue();
    }

    [Fact(DisplayName = "One parameter, and no System using to separate.")]
    public void OneParameter() {
        GoldenFile.Approve("AnyMoney", GeneratorEmitter.Emit(Shapes.Money()).SourceText);
    }

    [Fact(DisplayName = "No parameters, in the global namespace — the degenerate shape of §4.2.")]
    public void NoParameters() {
        GoldenFile.Approve("AnySession", GeneratorEmitter.Emit(Shapes.Session()).SourceText);
    }

    [Fact(DisplayName = "A name that collides with the library's own, in a block namespace.")]
    public void ANameThatCollides() {
        GoldenFile.Approve("AnyPattern", GeneratorEmitter.Emit(Shapes.Pattern()).SourceText);
    }

    [Fact(DisplayName = "A positional record, which needs no special handling.")]
    public void APositionalRecord() {
        GoldenFile.Approve("AnyAddress", GeneratorEmitter.Emit(Shapes.Address()).SourceText);
    }

    [Fact(DisplayName = "A static-factory target, whose Generate() calls the factory.")]
    public void AStaticFactoryTarget() {
        GoldenFile.Approve("AnyEmail", GeneratorEmitter.Emit(Shapes.Email()).SourceText);
    }

    [Fact(DisplayName = "The file is named after the generator, not after the target.")]
    public void TheFileIsNamedAfterTheGenerator() {
        Check.That(GeneratorEmitter.Emit(Shapes.Order()).FileName).IsEqualTo("AnyOrder.cs");
    }

    // §8.1 promises the same bytes on any machine. A culture is the cheapest way to prove the emitter takes
    // nothing from the environment: Turkish is the one that breaks a careless upper-casing, turning WithId into
    // WithÄ°d, and an invariant-culture bug here would produce a file that differs by machine and by nothing else.
    [Theory(DisplayName = "The same plan emits the same bytes, whatever the machine's culture.")]
    [InlineData("tr-TR")]
    [InlineData("fr-FR")]
    [InlineData("en-US")]
    public void TheSamePlanEmitsTheSameBytesInEveryCulture(string culture) {
        CultureInfo original = Thread.CurrentThread.CurrentCulture;

        try {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);

            Check.That(GeneratorEmitter.Emit(Shapes.Order()).SourceText).IsEqualTo(GoldenFile.ApprovedTextOf("AnyOrder"));
        } finally {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    // Not Environment.NewLine: a Windows developer and a Linux one must scaffold the same bytes from the same
    // type, or the first re-scaffold shows a whole-file diff that means nothing.
    [Fact(DisplayName = "Every line ends in a single newline, on every platform.")]
    public void EveryLineEndsInASingleNewline() {
        string emitted = GeneratorEmitter.Emit(Shapes.Order()).SourceText;

        Check.That(emitted).Not.Contains("\r");
        Check.That(emitted.EndsWith("\n", StringComparison.Ordinal)).IsTrue();
    }

}
