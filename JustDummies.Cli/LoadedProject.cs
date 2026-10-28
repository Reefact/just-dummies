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

    private LoadedProject(Compilation? compilation, IReadOnlyList<string> diagnostics, bool restored) {
        Compilation = compilation;
        Diagnostics = diagnostics;
        Restored    = restored;
    }

    /// <summary>The compilation to scaffold against, or null when the project did not open.</summary>
    internal Compilation? Compilation { get; }

    /// <summary>
    ///     What MSBuild said on the way, verbatim (§7). Present even on success: a project can open with
    ///     warnings that explain why a reference is missing.
    /// </summary>
    internal IReadOnlyList<string> Diagnostics { get; }

    /// <summary>
    ///     False when the project was found never to have been restored: it opens, but without one package
    ///     reference, so a missing library says nothing about what the project file declares (§7).
    /// </summary>
    internal bool Restored { get; }

    internal bool Succeeded => Compilation is not null;

    internal static LoadedProject Opened(Compilation compilation, IReadOnlyList<string> diagnostics) {
        return new LoadedProject(compilation, diagnostics, restored: true);
    }

    /// <summary>A project that opened without its packages, because nothing has restored it yet.</summary>
    internal static LoadedProject Unrestored(Compilation compilation, IReadOnlyList<string> diagnostics) {
        return new LoadedProject(compilation, diagnostics, restored: false);
    }

    internal static LoadedProject Failed(IReadOnlyList<string> diagnostics) {
        return new LoadedProject(compilation: null, diagnostics, restored: true);
    }

}
