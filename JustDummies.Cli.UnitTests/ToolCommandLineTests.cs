using System;
using System.IO;
using System.Reflection;

using NFluent;

namespace JustDummies.Cli.UnitTests;

/// <summary>
///     What the tool answers today. The command line of §3 is parsed in full — every option, with its help —
///     and `generate` then refuses, because no part of §4–§7 is implemented.
/// </summary>
/// <remarks>
///     The refusal is what keeps this build honest: a tool that accepted `generate` and printed nothing would
///     read, to a script, as a scaffolding run that produced no file.
/// </remarks>
public sealed class ToolCommandLineTests {

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

    // The whole point of shaping the surface before the behaviour: `generate` parses, and then says so.
    [Theory(DisplayName = "generate parses and refuses, on stderr, without claiming a scaffolding failure.")]
    [InlineData("generate Order")]
    [InlineData("generate Order Customer")]
    [InlineData("generate Order --force --dry-run")]
    [InlineData("generate Shop.Domain.Order --project a.csproj --output gen --namespace Shop.Tests")]
    public void GenerateParsesAndRefuses(string commandLine) {
        Invocation invocation = Invocation.Of(commandLine);

        // 2, not the 1 of §7: that code says a scaffolding run failed, and no run was started here.
        Check.That(invocation.ExitCode).IsEqualTo(2);
        Check.That(invocation.Error).Contains("not implemented yet");
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
