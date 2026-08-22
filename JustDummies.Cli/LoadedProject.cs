using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace JustDummies.Cli;

/// <summary>A project that opened, or the diagnostics that stopped it.</summary>
internal sealed class LoadedProject {

    private LoadedProject(Compilation? compilation, IReadOnlyList<string> diagnostics) {
        Compilation = compilation;
        Diagnostics = diagnostics;
    }

    /// <summary>The compilation to scaffold against, or null when the project did not open.</summary>
    internal Compilation? Compilation { get; }

    /// <summary>
    ///     What MSBuild said on the way, verbatim (§7). Present even on success: a project can open with
    ///     warnings that explain why a reference is missing.
    /// </summary>
    internal IReadOnlyList<string> Diagnostics { get; }

    internal bool Succeeded => Compilation is not null;

    internal static LoadedProject Opened(Compilation compilation, IReadOnlyList<string> diagnostics) {
        return new LoadedProject(compilation, diagnostics);
    }

    internal static LoadedProject Failed(IReadOnlyList<string> diagnostics) {
        return new LoadedProject(compilation: null, diagnostics);
    }

}
