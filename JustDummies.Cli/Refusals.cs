using System;
using System.Collections.Generic;

using JustDummies.GenAny;

using Spectre.Console;

namespace JustDummies.Cli;

/// <summary>
///     What the console says when there is no file — the failure rows of §7, in one place.
/// </summary>
/// <remarks>
///     Together rather than at each call site, because they answer to the same rule and it is only visible when
///     they are read side by side: a refusal names what could not be done, and then what to do about it. A type
///     that matched nothing carries the closest names, an ambiguous one carries the full names, a project that
///     could not be chosen carries the candidates and <c>--project</c>, an existing file carries
///     <c>--force</c> and what it costs. Every one of them is one line of fact and one line of instruction,
///     which is the difference between a tool that refuses and a tool that stonewalls.
/// </remarks>
internal static class Refusals {

    /// <summary>What is printed where a scaffold failed, and it fits on one line before the advice.</summary>
    private const string Mark = "✗ ";

    /// <summary>A scaffold that produced nothing, and the reason the engine gave (§3.2, §5.1, §11.1).</summary>
    internal static void Render(string typeArgument, ScaffoldOutcome outcome, IAnsiConsole console) {
        if (typeArgument is null) { throw new ArgumentNullException(nameof(typeArgument)); }
        if (outcome is null) { throw new ArgumentNullException(nameof(outcome)); }

        Say(console, Sentences(typeArgument, outcome));
    }

    /// <summary>No project to analyze, or too many to choose from (§3.1).</summary>
    internal static void NoProject(ProjectChoice choice, IAnsiConsole console) {
        if (choice is null) { throw new ArgumentNullException(nameof(choice)); }

        List<string> sentences = [Mark + choice.Refusal];

        sentences.AddRange(Listed(choice.Candidates));

        Say(console, sentences);
    }

    /// <summary>
    ///     A project that would not open. Its diagnostics have already been printed verbatim, which is the part
    ///     that carries the information; this is the sentence that says the run stopped there.
    /// </summary>
    internal static void ProjectDidNotLoad(string projectPath, IAnsiConsole console) {
        if (projectPath is null) { throw new ArgumentNullException(nameof(projectPath)); }

        Say(console, [
            Mark + $"{projectPath} did not load, so nothing was scaffolded.",
            "  Build it first: a project that does not restore has no compilation to read."
        ]);
    }

    /// <summary>
    ///     Something is already there, and this tool does not silently replace a developer's file.
    /// </summary>
    internal static void FileExists(string path, IAnsiConsole console) {
        if (path is null) { throw new ArgumentNullException(nameof(path)); }

        Say(console, [
            Mark + $"{path} already exists.",
            "  Re-run with --force to overwrite it. Whatever you changed in it is lost."
        ]);
    }

    /// <summary>
    ///     The sentences for one refused scaffold.
    /// </summary>
    /// <remarks>
    ///     The default arm is not dead code and is not defensive padding: it is what a status added without a
    ///     sentence here would print, and it prints the status's own name — which is unreadable enough to be
    ///     caught by the test that asserts no refusal ever reads like an enum value.
    /// </remarks>
    private static IReadOnlyList<string> Sentences(string typeArgument, ScaffoldOutcome outcome) {
        return outcome.Status switch {
            ScaffoldStatus.TypeNotFound          => NotFound(typeArgument, outcome.Candidates),
            ScaffoldStatus.TypeAmbiguous         => Ambiguous(typeArgument, outcome.Candidates),
            ScaffoldStatus.LibraryNotReferenced  => NotReferenced(typeArgument),
            ScaffoldStatus.NoEligibleConstructor => NotConstructible(typeArgument),
            _                                    => [Mark + $"{typeArgument}: {outcome.Status}."]
        };
    }

    /// <summary>
    ///     Nothing matched. The closest names come from the engine, so the answer is a correction rather than a
    ///     denial (§3.2) — and when nothing is even close, the likely cause is the project, not the spelling.
    /// </summary>
    private static IReadOnlyList<string> NotFound(string typeArgument, IReadOnlyList<string> closest) {
        if (closest.Count == 0) {
            return [
                Mark + $"{typeArgument}: no type by that name in this compilation, and nothing close to it.",
                "  Check --project: the type has to be reachable from the project being analyzed."
            ];
        }

        return [
            Mark + $"{typeArgument}: no type by that name in this compilation.",
            $"  Did you mean {string.Join(", ", closest)}?"
        ];
    }

    /// <summary>
    ///     Several matched, and the tool does not pick: scaffolding the wrong one produces a file that compiles
    ///     and is quietly about another type.
    /// </summary>
    private static IReadOnlyList<string> Ambiguous(string typeArgument, IReadOnlyList<string> matches) {
        List<string> sentences = [Mark + $"{typeArgument}: several types answer to that name."];

        sentences.AddRange(Listed(matches));
        sentences.Add("  Name the one you mean in full.");

        return sentences;
    }

    /// <summary>
    ///     Not one expression could be resolved, because every candidate member is looked up in the
    ///     compilation before it is kept (ADR-0059). Without the package there is nothing to look up.
    /// </summary>
    private static IReadOnlyList<string> NotReferenced(string typeArgument) {
        return [
            Mark + $"{typeArgument}: this project does not reference JustDummies, so no expression could be resolved.",
            "  Add it to the project being analyzed: dotnet add package JustDummies"
        ];
    }

    /// <summary>
    ///     Nothing the emitted <c>Generate()</c> could call — which is a fact about the type, and one only its
    ///     author can change (§5.1).
    /// </summary>
    private static IReadOnlyList<string> NotConstructible(string typeArgument) {
        return [
            Mark + $"{typeArgument}: nothing here constructs it.",
            "  Generate() needs a public instance constructor whose parameters are all passed by value."
        ];
    }

    /// <summary>The candidates, indented under the sentence that introduced them.</summary>
    private static IEnumerable<string> Listed(IReadOnlyList<string> candidates) {
        foreach (string candidate in candidates) { yield return "  " + candidate; }
    }

    /// <summary>
    ///     The refusal, then a blank line — so a run over several types does not read as one long paragraph.
    /// </summary>
    private static void Say(IAnsiConsole console, IEnumerable<string> sentences) {
        if (console is null) { throw new ArgumentNullException(nameof(console)); }

        foreach (string sentence in sentences) { Unwrapped.Line(console, sentence); }

        Unwrapped.Line(console, string.Empty);
    }

}
