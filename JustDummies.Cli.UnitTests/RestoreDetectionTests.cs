using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using NFluent;

using Spectre.Console;

namespace JustDummies.Cli.UnitTests;

/// <summary>
///     Whether a real project on disk reads as restored, through the opener the tool itself uses (§7).
/// </summary>
/// <remarks>
///     The other suites stand in for the opener, which is right for everything after it and says nothing about
///     the one fact only a real project carries: where NuGet wrote its assets file. A project that does not
///     reference JustDummies has to keep hearing "add the package", restored or not, and only a project that
///     was never restored may be told to restore.
/// </remarks>
public sealed class RestoreDetectionTests : IDisposable {

    private const string Money = """
                                 namespace Pilot.Domain;

                                 public sealed class Money {
                                     public Money(decimal amount) { }
                                 }
                                 """;

    private readonly string directory = Directory.CreateTempSubdirectory("any-restore-").FullName;

    public void Dispose() {
        Directory.Delete(directory, recursive: true);
    }

    // The case a review found worth proving rather than arguing: restored, and simply without the library.
    [Fact(DisplayName = "A restored project without the library is told to add the package, not to restore.")]
    public async Task ARestoredProjectWithoutTheLibraryIsToldToAddThePackage() {
        string project = Project(packageReference: null);

        await Restore(project);

        LoadedProject loaded = await ProjectCompilation.OpenAsync(project, TestContext.Current.CancellationToken);
        Run           run    = await Generate(project);

        Check.That(loaded.Restored).IsTrue();
        Check.That(run.Error).Contains("dotnet add package JustDummies");
        Check.That(run.Error).Not.Contains("dotnet restore");
    }

    [Fact(DisplayName = "A project that was never restored is told to restore, not to add the package.")]
    public async Task AnUnrestoredProjectIsToldToRestore() {
        string project = Project(packageReference: """<PackageReference Include="JustDummies" Version="1.0.0" />""");

        LoadedProject loaded = await ProjectCompilation.OpenAsync(project, TestContext.Current.CancellationToken);
        Run           run    = await Generate(project);

        Check.That(loaded.Restored).IsFalse();
        Check.That(run.Error).Contains("dotnet restore");
        Check.That(run.Error).Not.Contains("dotnet add package");
    }

    /// <summary>A project on the running runtime's framework, so the SDK restores it without a feed.</summary>
    private string Project(string? packageReference) {
        string framework = "net" + Environment.Version.Major + ".0";
        string path      = Path.Combine(directory, "Pilot.csproj");

        File.WriteAllText(path, $"""
                                 <Project Sdk="Microsoft.NET.Sdk">
                                   <PropertyGroup>
                                     <TargetFramework>{framework}</TargetFramework>
                                     <Nullable>enable</Nullable>
                                   </PropertyGroup>
                                   <ItemGroup>{packageReference}</ItemGroup>
                                 </Project>
                                 """);
        File.WriteAllText(Path.Combine(directory, "Money.cs"), Money);

        return path;
    }

    private static async Task Restore(string project) {
        using Process restore = Process.Start(new ProcessStartInfo("dotnet", $"restore \"{project}\" -v q") {
            RedirectStandardOutput = true,
            RedirectStandardError  = true
        })!;

        await restore.WaitForExitAsync(TestContext.Current.CancellationToken);

        Check.WithCustomMessage("dotnet restore failed: " + await restore.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken))
             .That(restore.ExitCode).IsEqualTo(0);
    }

    private async Task<Run> Generate(string project) {
        StringWriter output = new();
        StringWriter error  = new();

        IAnsiConsole outputConsole = ToolConsole.On(output);
        IAnsiConsole errorConsole  = ToolConsole.On(error);

        GenerateCommand  command  = new(new ToolConsoles(outputConsole, errorConsole), ProjectCompilation.OpenAsync);
        GenerateSettings settings = new() { Types = ["Money"], Output = directory, Project = project, DryRun = true };

        int exitCode = await command.RunAsync(settings, TestContext.Current.CancellationToken);

        return new Run(exitCode, output.ToString(), error.ToString());
    }

    private sealed record Run(int ExitCode, string Output, string Error);

}
