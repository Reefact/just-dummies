using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JustDummies.Cli;

/// <summary>What <see cref="ProjectLocator" /> concluded: one project, or the reason there is not one.</summary>
internal sealed class ProjectChoice {

    private ProjectChoice(string? path, string? refusal, IReadOnlyList<string> candidates) {
        Path       = path;
        Refusal    = refusal;
        Candidates = candidates;
    }

    /// <summary>The project to open, or null when there is none to open.</summary>
    internal string? Path { get; }

    /// <summary>Why there is none, in one sentence for the console.</summary>
    internal string? Refusal { get; }

    /// <summary>The projects that were found, when there were too many.</summary>
    internal IReadOnlyList<string> Candidates { get; }

    /// <summary>Whether exactly one project was settled on.</summary>
    internal bool Found => Path is not null;

    internal static ProjectChoice At(string path) {
        return new ProjectChoice(path, refusal: null, candidates: []);
    }

    internal static ProjectChoice None(string refusal) {
        return new ProjectChoice(path: null, refusal, candidates: []);
    }

    internal static ProjectChoice Between(IReadOnlyList<string> candidates) {
        return new ProjectChoice(path: null,
                                 "Several projects here; name the one to analyze with --project.",
                                 candidates);
    }

}
