using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using JustDummies.GenAny;

using Microsoft.CodeAnalysis;

using Spectre.Console.Cli;

namespace JustDummies.Cli;

/// <summary>
///     <c>dum generate</c>: the pipeline of §11.1, from a directory to a file the developer then owns.
/// </summary>
/// <remarks>
///     The shell's half of it, and only that half. Steps 1 to 3 are here because they need MSBuild, a disk and
///     a console; steps 4 to 7 are the engine's and this command cannot see inside them; step 8 — write the
///     file, render the recap — is here again. What that split buys is visible in this file: it decides where
///     things go and what the process exits with, and it decides nothing about what a parameter resolves to.
/// </remarks>
internal sealed class GenerateCommand : AsyncCommand<GenerateSettings> {

    private readonly ToolConsoles consoles;

    private readonly ProjectOpener open;

    /// <summary>The constructor Spectre uses, through the registrar.</summary>
    public GenerateCommand(ToolConsoles consoles) : this(consoles, ProjectCompilation.OpenAsync) { }

    /// <summary>The same command, reading a project the caller opens — see <see cref="ProjectOpener" />.</summary>
    internal GenerateCommand(ToolConsoles consoles, ProjectOpener open) {
        this.consoles = consoles ?? throw new ArgumentNullException(nameof(consoles));
        this.open     = open ?? throw new ArgumentNullException(nameof(open));
    }

    /// <inheritdoc />
    protected override Task<int> ExecuteAsync(CommandContext context,
                                              GenerateSettings settings,
                                              CancellationToken cancellationToken) {
        return RunAsync(settings, cancellationToken);
    }

    /// <summary>Runs one <c>generate</c> and returns what the process exits with (§7).</summary>
    internal async Task<int> RunAsync(GenerateSettings settings, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(settings);

        string        here    = Directory.GetCurrentDirectory();
        ProjectChoice project = ProjectLocator.Locate(here, settings.Project);

        if (!project.Found) {
            Refusals.NoProject(project, consoles.Error);

            return ExitCode.Failed;
        }

        LoadedProject loaded = await open(project.Path!, cancellationToken).ConfigureAwait(false);

        // Surfaced, not swallowed (§11.1) — and on the way through, not only on failure: a project that opened
        // with a warning about an unresolved reference is exactly the project whose scaffold will read as "the
        // tool inferred nothing", and the two facts belong on screen together.
        foreach (string diagnostic in loaded.Diagnostics) { Unwrapped.Line(consoles.Error, "! " + diagnostic); }

        if (!loaded.Succeeded) {
            Refusals.ProjectDidNotLoad(project.Path!, consoles.Error);

            return ExitCode.Failed;
        }

        return ScaffoldEach(loaded.Compilation!, settings, here);
    }

    /// <summary>
    ///     One scaffold per type argument, independently, exiting with the worst of them (§7).
    /// </summary>
    private int ScaffoldEach(Compilation compilation, GenerateSettings settings, string here) {
        ScaffoldOptions options = settings.Namespace is null
                                      ? ScaffoldOptions.Default
                                      : ScaffoldOptions.Default.InNamespace(settings.Namespace);

        string    directory = settings.Output is null ? here : Path.GetFullPath(settings.Output);
        List<int> codes     = [];

        foreach (string typeArgument in settings.Types) {
            ScaffoldOutcome outcome = Scaffolder.Scaffold(compilation, typeArgument, options);

            codes.Add(Report(typeArgument, outcome, settings, directory));

            // The one refusal that is not about the type: without the package nothing in this project can be
            // resolved, so the remaining arguments would each print the same two lines. Said once, and the run
            // stops — the exit code is the same either way.
            if (outcome.Status == ScaffoldStatus.LibraryNotReferenced) { break; }
        }

        return ExitCode.Worst(codes);
    }

    /// <summary>
    ///     Puts one outcome where it belongs: on disk and in the recap, or on stderr with the reason.
    /// </summary>
    /// <remarks>
    ///     The recap comes after the write rather than before it, because its closing line says the file was
    ///     produced. Under <c>--dry-run</c> nothing is written, so the two swap streams instead: the file goes
    ///     to stdout for a developer to pipe, and the recap to stderr for them to read while it does (§6).
    /// </remarks>
    private int Report(string typeArgument, ScaffoldOutcome outcome, GenerateSettings settings, string directory) {
        if (!outcome.Succeeded) {
            Refusals.Render(typeArgument, outcome, consoles.Error);

            return ExitCode.Failed;
        }

        if (settings.DryRun) {
            Recap.Render(outcome, consoles.Error);
            Unwrapped.Line(consoles.Error, "  Nothing written: --dry-run. The file itself is on stdout.");
            Unwrapped.Line(consoles.Error, string.Empty);
            Unwrapped.Text(consoles.Output, outcome.File!.SourceText);

            return ExitCode.Success;
        }

        WriteOutcome written = ScaffoldWriter.Write(outcome.File!, directory, settings.Force);

        if (!written.Succeeded) {
            Refusals.FileExists(written.Path, consoles.Error);

            return ExitCode.Failed;
        }

        Recap.Render(outcome, consoles.Output);

        // The separator belongs to the run, not to the recap: §6 writes one scaffold out, and a blank line
        // under it is what keeps three of them from reading as one paragraph.
        Unwrapped.Line(consoles.Output, string.Empty);

        return ExitCode.For(outcome);
    }

}
