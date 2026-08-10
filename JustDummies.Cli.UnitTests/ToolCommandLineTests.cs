using System;
using System.IO;
using System.Reflection;

using NFluent;

namespace JustDummies.Cli.UnitTests;

/// <summary>
///     The surface around the one verb: what <c>dum</c> answers before any project is opened.
/// </summary>
/// <remarks>
///     What <c>generate</c> then does is <see cref="GenerateCommandTests" />' subject, over a compilation built
///     in memory. What is checked here is the wiring — that the command is reached at all, with the consoles it
///     needs — and the two answers that never reach it: a version, and a command line the tool could not read.
/// </remarks>
public sealed class ToolCommandLineTests {

    /// <summary>A project that is not there, so no run reaches MSBuild or the directory the suite runs from.</summary>
    private static readonly string Absent = Path.Combine(Path.GetTempPath(), "dum-absent", "Nothing.csproj");

    [Fact(DisplayName = "--version reports the version, on stdout, and succeeds.")]
    public void VersionIsReportedOnStandardOutput() {
        Invocation invocation = Invocation.Of("--version");

        Check.That(invocation.ExitCode).IsEqualTo(0);
        Check.That(invocation.Output.Trim()).IsEqualTo(ToolCommandLine.Version);
        Check.That(invocation.Error).IsEmpty();
    }

    // A deterministic build appends `+<commit sha>` to the informational version. A developer comparing their
    // tool against a release note wants the number that is on nuget.org, not the build it came from.
    [Fact(DisplayName = "The reported version carries no build metadata.")]
    public void TheReportedVersionCarriesNoBuildMetadata() {
        string informational = typeof(ToolCommandLine).Assembly
                                                      .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                                                      .InformationalVersion;

        Check.That(ToolCommandLine.Version).Not.Contains("+");
        Check.That(ToolCommandLine.Version).IsNotEmpty();
        Check.That(informational).StartsWith(ToolCommandLine.Version);
    }

    [Fact(DisplayName = "--help lists the one verb, on stdout, and succeeds.")]
    public void HelpListsTheOneVerbOnStandardOutput() {
        Invocation invocation = Invocation.Of("--help");

        Check.That(invocation.ExitCode).IsEqualTo(0);
        Check.That(invocation.Output).Contains("generate");
        Check.That(invocation.Error).IsEmpty();
    }

    /// <summary>
    ///     Every option of §3 parses, reaches the command, and comes back as §7's exit <c>1</c>.
    /// </summary>
    /// <remarks>
    ///     Named <c>--project</c> at a path that does not exist, so each of these gets as far as the first step
    ///     of §11.1 and no further: nothing is opened, nothing is written, and the answer is the tool's own
    ///     refusal rather than whatever happens to sit in the directory the suite runs from.
    /// </remarks>
    [Theory(DisplayName = "Every option of §3 parses, reaches generate, and fails on the named project.")]
    [InlineData("generate Order")]
    [InlineData("generate Order Customer")]
    [InlineData("generate Order --force --dry-run")]
    [InlineData("generate Shop.Domain.Order --output gen --namespace Shop.Tests")]
    public void EveryOptionParsesAndReachesGenerate(string commandLine) {
        Invocation invocation = Invocation.Of(commandLine + " --project " + Absent);

        // 1, not the 2 above: the command line was read, so this is a scaffolding run that failed (§7).
        Check.That(invocation.ExitCode).IsEqualTo(1);
        Check.That(invocation.Error).Contains(Absent);
        Check.That(invocation.Output).IsEmpty();
    }

    // Strict parsing, so a mistyped --forse is refused rather than read as "no --force was given"; and the
    // framework's own -1 never reaches the caller.
    [Theory(DisplayName = "A command line that cannot be parsed is refused with the tool's own exit code.")]
    [InlineData("generate")]
    [InlineData("--nonsense")]
    [InlineData("generate Order --forse")]
    [InlineData("scaffold Order")]
    public void AnUnparsableCommandLineIsRefused(string commandLine) {
        Invocation invocation = Invocation.Of(commandLine);

        Check.That(invocation.ExitCode).IsEqualTo(2);
        Check.That(invocation.Error).IsNotEmpty();
    }

    private sealed record Invocation(int ExitCode, string Output, string Error) {

        internal static Invocation Of(string commandLine) {
            string[]     args   = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            StringWriter output = new();
            StringWriter error  = new();

            int exitCode = ToolCommandLine.Run(args, ToolConsole.On(output), ToolConsole.On(error));

            return new Invocation(exitCode, output.ToString(), error.ToString());
        }

    }

}
