using System;
using System.IO;
using System.Text;

using JustDummies.GenAny;

using NFluent;

namespace JustDummies.Cli.UnitTests;

/// <summary>
///     Putting the emitted file on disk — the one act of this tool that changes a working tree.
/// </summary>
public sealed class ScaffoldWriterTests : IDisposable {

    private readonly string directory = Directory.CreateTempSubdirectory("dum-write-").FullName;

    /// <inheritdoc />
    public void Dispose() {
        Directory.Delete(directory, recursive: true);
    }

    [Fact(DisplayName = "The file lands in the output directory, under the emitted name.")]
    public void TheFileLandsUnderItsEmittedName() {
        ScaffoldedFile file = Emitted();

        WriteOutcome written = ScaffoldWriter.Write(file, directory, force: false);

        Check.That(written.Succeeded).IsTrue();
        Check.That(written.Path).IsEqualTo(Path.Combine(directory, "AnySubject.cs"));
        Check.That(File.ReadAllText(written.Path)).IsEqualTo(file.SourceText);
    }

    /// <summary>
    ///     The bytes on disk are the emitter's, and §8.1's byte-identity is about the file, not the string.
    /// </summary>
    /// <remarks>
    ///     A byte-order mark would be added by the writer, not by the emitter, and it would make the same
    ///     scaffold produce two different files depending on which tool wrote it — which is exactly what a
    ///     re-scaffold reviewed as a diff must not do.
    /// </remarks>
    [Fact(DisplayName = "The file is written in UTF-8 with no byte-order mark, and no line ending is translated.")]
    public void TheFileIsWrittenByteForByte() {
        ScaffoldedFile file = Emitted();

        byte[] written = File.ReadAllBytes(ScaffoldWriter.Write(file, directory, force: false).Path);

        Check.That(written).ContainsExactly(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
                                               .GetBytes(file.SourceText));
    }

    // The developer's file is theirs. A scaffolder that silently replaced a hand-edited one would be used
    // once.
    [Fact(DisplayName = "An existing file is refused, and left exactly as it was.")]
    public void AnExistingFileIsRefused() {
        string path = Path.Combine(directory, "AnySubject.cs");

        File.WriteAllText(path, "// mine");

        WriteOutcome written = ScaffoldWriter.Write(Emitted(), directory, force: false);

        Check.That(written.Succeeded).IsFalse();
        Check.That(written.Path).IsEqualTo(path);
        Check.That(File.ReadAllText(path)).IsEqualTo("// mine");
    }

    [Fact(DisplayName = "--force overwrites it, which is the sentence that says the developer knows.")]
    public void ForceOverwritesIt() {
        ScaffoldedFile file = Emitted();

        File.WriteAllText(Path.Combine(directory, "AnySubject.cs"), "// mine");

        WriteOutcome written = ScaffoldWriter.Write(file, directory, force: true);

        Check.That(written.Succeeded).IsTrue();
        Check.That(File.ReadAllText(written.Path)).IsEqualTo(file.SourceText);
    }

    // --output names where the file belongs, not where it happens to be already: a test project laying its
    // generators under a folder it has not created yet is the ordinary case.
    [Fact(DisplayName = "An output directory that is not there yet is created.")]
    public void AnAbsentOutputDirectoryIsCreated() {
        string below = Path.Combine(directory, "Generators", "Dummies");

        WriteOutcome written = ScaffoldWriter.Write(Emitted(), below, force: false);

        Check.That(written.Succeeded).IsTrue();
        Check.That(File.Exists(Path.Combine(below, "AnySubject.cs"))).IsTrue();
    }

    private static ScaffoldedFile Emitted() {
        return GeneratorEmitter.Emit(new ScaffoldPlan(new TargetType("Subject", "Shop.Domain", NamespaceStyle.FileScoped),
                                                      "AnySubject",
                                                      ["JustDummies"],
                                                      [
                                                          ScaffoldedParameter.DrawnFrom("value", "string",
                                                                                        "Any.String().NonEmpty()",
                                                                                        Provenance.None)
                                                      ]));
    }

}
