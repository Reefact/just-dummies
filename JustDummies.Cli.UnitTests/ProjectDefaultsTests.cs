using System;
using System.IO;

using NFluent;

namespace JustDummies.Cli.UnitTests;

/// <summary>
///     The optional <c>dum.json</c> beside the project (§3.3).
/// </summary>
/// <remarks>
///     Two properties carry it, and every case below is one of them. The command line always wins, because a
///     value the developer typed is already there and nothing overwrites one. And a key the file does not read
///     is <b>refused</b>: a default someone believes is in force and is not would be worse than having no file
///     at all.
/// </remarks>
public sealed class ProjectDefaultsTests : IDisposable {

    private readonly string directory = Directory.CreateTempSubdirectory("dum-defaults-").FullName;

    /// <inheritdoc />
    public void Dispose() {
        Directory.Delete(directory, recursive: true);
    }

    [Fact(DisplayName = "No file at all is not a refusal: the tool has always run without one.")]
    public void NoFileIsNotARefusal() {
        ProjectDefaults defaults = ProjectDefaults.Beside(Project());

        Check.That(defaults.Understood).IsTrue();
        Check.That(defaults.Values).IsEmpty();
    }

    [Fact(DisplayName = "The file sets what the command line did not.")]
    public void TheFileSetsWhatTheCommandLineDidNot() {
        GenerateSettings settings = Settings("""
                                             { "namespace": "Shop.Tests.Dummies", "entryPoint": "static:Dummies" }
                                             """);

        Check.That(settings.Namespace).IsEqualTo("Shop.Tests.Dummies");
        Check.That(settings.EntryPoint).IsEqualTo("static:Dummies");
    }

    // The whole precedence rule, and it is one sentence: what was typed is already there, and nothing here
    // overwrites it.
    [Fact(DisplayName = "The command line wins over the file, option by option.")]
    public void TheCommandLineWinsOverTheFile() {
        GenerateSettings settings = new() { Types = ["Order"], Namespace = "Typed.On.The.Line" };

        Written("""
                { "namespace": "Shop.Tests.Dummies", "format": "json" }
                """);
        ProjectDefaults.Beside(Project()).ApplyTo(settings, Project());

        Check.That(settings.Namespace).IsEqualTo("Typed.On.The.Line");
        Check.That(settings.Format).IsEqualTo("json");
    }

    /// <summary>
    ///     A relative <c>output</c> is resolved against the project's directory, never the current one.
    /// </summary>
    /// <remarks>
    ///     A path typed on the command line is relative to where it was typed; a path committed in this file
    ///     has to mean the same thing wherever the tool is run from, or it is not a default.
    /// </remarks>
    [Fact(DisplayName = "A relative output is rooted at the project, not at the current directory.")]
    public void ARelativeOutputIsRootedAtTheProject() {
        GenerateSettings settings = Settings("""
                                             { "output": "./Dummies" }
                                             """);

        Check.That(settings.Output).IsEqualTo(Path.Combine(directory, "Dummies"));
    }

    [Fact(DisplayName = "A key the file does not read is refused, and named.")]
    public void AnUnknownKeyIsRefused() {
        Written("""
                { "outpout": "./Dummies" }
                """);

        ProjectDefaults defaults = ProjectDefaults.Beside(Project());

        Check.That(defaults.Understood).IsFalse();
        Check.That(defaults.Refusal).Contains("outpout");
        Check.That(defaults.Refusal).Contains("output");
    }

    // §16 reserves it for --name and --pattern, which do not exist yet. A key that configured nothing would be
    // worse than one that says so.
    [Fact(DisplayName = "The naming key §16 reserves is refused while it configures nothing.")]
    public void TheReservedNamingKeyIsRefused() {
        Written("""
                { "naming": "Any{Type}" }
                """);

        Check.That(ProjectDefaults.Beside(Project()).Understood).IsFalse();
    }

    [Fact(DisplayName = "A value that is not a string is refused, naming the key.")]
    public void ANonStringValueIsRefused() {
        Written("""
                { "output": 42 }
                """);

        ProjectDefaults defaults = ProjectDefaults.Beside(Project());

        Check.That(defaults.Understood).IsFalse();
        Check.That(defaults.Refusal).Contains("output");
    }

    [Fact(DisplayName = "A file that is not JSON is refused, with the parser's own reason.")]
    public void AFileThatIsNotJsonIsRefused() {
        Written("output: ./Dummies");

        ProjectDefaults defaults = ProjectDefaults.Beside(Project());

        Check.That(defaults.Understood).IsFalse();
        Check.That(defaults.Refusal).Contains(ProjectDefaults.FileName);
    }

    [Fact(DisplayName = "A file that is not an object is refused, and shows one that is.")]
    public void AFileThatIsNotAnObjectIsRefused() {
        Written("""
                [ "output" ]
                """);

        Check.That(ProjectDefaults.Beside(Project()).Understood).IsFalse();
    }

    [Fact(DisplayName = "Both arguments are required.")]
    public void BothArgumentsAreRequired() {
        Check.ThatCode(() => ProjectDefaults.Beside(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => ProjectDefaults.Beside(Project()).ApplyTo(null!, Project())).Throws<ArgumentNullException>();
    }

    private GenerateSettings Settings(string json) {
        GenerateSettings settings = new() { Types = ["Order"] };

        Written(json);
        ProjectDefaults.Beside(Project()).ApplyTo(settings, Project());

        return settings;
    }

    private void Written(string json) {
        File.WriteAllText(Path.Combine(directory, ProjectDefaults.FileName), json);
    }

    private string Project() {
        string path = Path.Combine(directory, "Shop.Tests.csproj");

        if (!File.Exists(path)) { File.WriteAllText(path, "<Project Sdk=\"Microsoft.NET.Sdk\" />"); }

        return path;
    }

}
