using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using JustDummies.GenAny;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Spectre.Console;
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

            return Refused(settings, RunRefusal.NoProject);
        }

        // Before the project is opened, and before any option is read: what the file sets is what the rest of
        // this method sees, so nothing downstream has to know there was a file (§3.3).
        ProjectDefaults defaults = ProjectDefaults.Beside(project.Path!);

        if (!defaults.Understood) { return Unreadable(settings, defaults.Refusal!); }

        defaults.ApplyTo(settings, project.Path!);

        // Validated again over the merged state, through the same rules the command line answered to: a value
        // this file supplied is refused for exactly the reasons a typed one would be, and in the same words.
        ValidationResult merged = settings.Validate();

        if (!merged.Successful) { return Unreadable(settings, merged.Message ?? string.Empty); }

        LoadedProject loaded = await open(project.Path!, cancellationToken).ConfigureAwait(false);

        // Surfaced, not swallowed (§11.1) — and on the way through, not only on failure: a project that opened
        // with a warning about an unresolved reference is exactly the project whose scaffold will read as "the
        // tool inferred nothing", and the two facts belong on screen together.
        foreach (string diagnostic in loaded.Diagnostics) { Unwrapped.Line(consoles.Error, "! " + diagnostic); }

        if (!loaded.Succeeded) {
            Refusals.ProjectDidNotLoad(project.Path!, consoles.Error);

            return Refused(settings, RunRefusal.ProjectDidNotLoad);
        }

        EntryPointArgument entryPoint = settings.ReadEntryPoint();

        // Asked once for the run, not once per type: it is a fact about the project, so repeating it under
        // `dum generate Order Customer Invoice` would print the same two lines three times.
        if (!CanCompileEntryPoint(loaded.Compilation!, entryPoint, out string languageVersion)) {
            Refusals.LanguageVersionTooLow(project.Path!, languageVersion, consoles.Error);

            return Refused(settings, RunRefusal.LanguageVersionTooLow);
        }

        return ScaffoldEach(loaded.Compilation!, settings, entryPoint, here);
    }

    /// <summary>
    ///     A run that stopped before its first scaffold: the sentence is already on stderr, and this is what
    ///     stdout owes a script.
    /// </summary>
    /// <remarks>
    ///     Emitted even here, so that <c>--format json</c> means "stdout carries one JSON document" with no
    ///     exception to remember. Under the recap of §6 there is nothing to add — stderr has said it.
    /// </remarks>
    private int Refused(GenerateSettings settings, string refusal) {
        if (settings.ReportsAsJson()) { JsonReport.Write(RunReport.Refused(refusal), consoles.Output); }

        return ExitCode.Failed;
    }

    /// <summary>
    ///     A <c>dum.json</c> that could not be read, or whose values the merged command line refuses (§3.3).
    /// </summary>
    /// <remarks>
    ///     Exit <c>2</c> rather than <c>1</c>, on the same rule §7 already draws: the tool never got as far as
    ///     scaffolding anything, and what it could not read is an instruction rather than a project.
    /// </remarks>
    private int Unreadable(GenerateSettings settings, string refusal) {
        Refusals.UnreadableDefaults(refusal, consoles.Error);

        if (settings.ReportsAsJson()) { JsonReport.Write(RunReport.Refused(RunRefusal.UnreadableDefaults), consoles.Output); }

        return ExitCode.Usage;
    }

    /// <summary>
    ///     Whether the project could compile the entry point it asked for (§4.5, §7).
    /// </summary>
    /// <remarks>
    ///     The check belongs to the shell and not to the engine: the engine is compiled against the Roslyn floor
    ///     (§13.2), which has no name for C# 14, while the CLI hosts a current compiler and reads the version
    ///     the project actually resolved. A compilation that is not C# — none reaches here, but the model
    ///     admits one — is not asked a question it cannot answer.
    /// </remarks>
    private static bool CanCompileEntryPoint(Compilation compilation, EntryPointArgument entryPoint, out string languageVersion) {
        languageVersion = string.Empty;

        if (entryPoint.Options.Kind != EntryPointKind.Any) { return true; }
        if (compilation is not CSharpCompilation csharp) { return true; }

        languageVersion = csharp.LanguageVersion.ToDisplayString();

        return csharp.LanguageVersion >= LanguageVersion.CSharp14;
    }

    /// <summary>
    ///     One scaffold per type argument, independently, exiting with the worst of them (§7).
    /// </summary>
    private int ScaffoldEach(Compilation compilation, GenerateSettings settings, EntryPointArgument entryPoint, string here) {
        ScaffoldOptions options = ScaffoldOptions.Default.WithEntryPoint(entryPoint.Options);

        if (settings.Namespace is not null) { options = options.InNamespace(settings.Namespace); }

        string               directory = settings.Output is null ? here : Path.GetFullPath(settings.Output);
        List<int>            codes     = [];
        List<ScaffoldReport> reported  = [];

        foreach (string typeArgument in settings.Types) {
            ScaffoldOutcome  outcome  = Scaffolder.Scaffold(compilation, typeArgument, options);
            ReportedScaffold scaffold = Report(typeArgument, outcome, settings, directory);

            codes.Add(scaffold.Code);
            reported.Add(scaffold.Report);

            // The one refusal that is not about the type: without the package nothing in this project can be
            // resolved, so the remaining arguments would each print the same two lines. Said once, and the run
            // stops — the exit code is the same either way.
            if (outcome.Status == ScaffoldStatus.LibraryNotReferenced) { break; }
        }

        if (settings.ReportsAsJson()) { JsonReport.Write(RunReport.Of(reported), consoles.Output); }

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
    private ReportedScaffold Report(string typeArgument, ScaffoldOutcome outcome, GenerateSettings settings, string directory) {
        bool json = settings.ReportsAsJson();

        if (!outcome.Succeeded) {
            Refusals.Render(typeArgument, outcome, consoles.Error);

            return new ReportedScaffold(ExitCode.Failed, ScaffoldReport.Refused(typeArgument, outcome));
        }

        IReadOnlyList<ScaffoldedFile> files = Files(outcome);

        if (settings.DryRun) {
            Recap.Render(outcome, consoles.Error);
            Unwrapped.Line(consoles.Error, DryRunNotice(files.Count, json));
            Unwrapped.Line(consoles.Error, string.Empty);

            // Under the recap of §6 the files themselves go to stdout, with no separator invented between
            // them: each opens with the three header lines of §4.3, which name it and the option that wrote
            // it. Under --format json stdout is carrying the document, so each file's text travels inside it.
            if (!json) {
                foreach (ScaffoldedFile file in files) { Unwrapped.Text(consoles.Output, file.SourceText); }
            }

            return new ReportedScaffold(ExitCode.Success,
                                        ScaffoldReport.Of(typeArgument, outcome, [.. files.Select(FileReport.Printed)]));
        }

        WriteOutcome written = ScaffoldWriter.WriteAll(files, directory, settings.Force);

        if (!written.Succeeded) {
            Refusals.FileExists(written.Path, consoles.Error);

            return new ReportedScaffold(ExitCode.Failed, ScaffoldReport.Refused(typeArgument, outcome));
        }

        if (!json) {
            Recap.Render(outcome, consoles.Output);

            // The separator belongs to the run, not to the recap: §6 writes one scaffold out, and a blank line
            // under it is what keeps three of them from reading as one paragraph.
            Unwrapped.Line(consoles.Output, string.Empty);
        }

        IReadOnlyList<FileReport> landed = [.. files.Select(file => FileReport.WrittenTo(file.FileName,
                                                                                        Path.Combine(directory, file.FileName)))];

        return new ReportedScaffold(ExitCode.For(outcome), ScaffoldReport.Of(typeArgument, outcome, landed));
    }

    /// <summary>Where a <c>--dry-run</c>'s files went, which is not the same place under each format.</summary>
    private static string DryRunNotice(int files, bool json) {
        if (json) { return "  Nothing written: --dry-run. Each file's text is in the report on stdout."; }

        return files == 1
                   ? "  Nothing written: --dry-run. The file itself is on stdout."
                   : "  Nothing written: --dry-run. Both files are on stdout, in that order.";
    }

    /// <summary>One scaffold's two answers: what the process exits with, and what the report records.</summary>
    private sealed record ReportedScaffold(int Code, ScaffoldReport Report);

    /// <summary>
    ///     Everything one scaffold produced, generator first — the order it is written in, printed in and read
    ///     in, so a <c>--dry-run</c> and a write never disagree about which file came first.
    /// </summary>
    private static IReadOnlyList<ScaffoldedFile> Files(ScaffoldOutcome outcome) {
        return outcome.EntryPoint is null ? [outcome.File!] : [outcome.File!, outcome.EntryPoint.File];
    }

}
