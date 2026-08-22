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

/// <summary>
///     Opens a project on disk and hands its compilation to the engine (§11.1, steps 1 to 3).
/// </summary>
/// <remarks>
///     All of it belongs to the shell. The engine never touches MSBuild — that is one of the three constraints
///     that keep it loadable inside a Roslyn host (ADR-0065), and it is why this file has no counterpart there.
/// </remarks>
internal static class ProjectCompilation {

    private static readonly object Gate = new();

    /// <summary>
    ///     The compilation of the project at <paramref name="projectPath" />, or the diagnostics that stopped
    ///     it.
    /// </summary>
    internal static async Task<LoadedProject> OpenAsync(string projectPath, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(projectPath);

        RegisterMSBuild();

        return await LoadAsync(projectPath, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Finds MSBuild before anything can need it.
    /// </summary>
    /// <remarks>
    ///     <b>Before touching any workspace type</b>, which is the whole reason this is a method of its own and
    ///     the caller below is marked not to be inlined. Loading <c>MSBuildWorkspace</c> first is the classic
    ///     way this fails, and it fails with a <c>FileNotFoundException</c> on <c>Microsoft.Build</c> that
    ///     names nothing a developer could act on.
    /// </remarks>
    private static void RegisterMSBuild() {
        lock (Gate) {
            if (!MSBuildLocator.IsRegistered) { MSBuildLocator.RegisterDefaults(); }
        }
    }

    /// <summary>
    ///     Opens the project. Kept out of its caller so no workspace type is resolved before the registration
    ///     above has run — the JIT resolves a method's types when it compiles that method, not when it reaches
    ///     the line.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<LoadedProject> LoadAsync(string projectPath, CancellationToken cancellationToken) {
        List<string> diagnostics = [];
        object       reporting   = new();

        using MSBuildWorkspace workspace = MSBuildWorkspace.Create();

        // Surfaced, not swallowed (§11.1). A project that half-loaded produces a compilation missing the very
        // references the scaffold reads, and the developer would meet that as "nothing was inferred".
        //
        // Under a lock because the handler is documented as running off the caller's thread: this is a plain
        // List, and the alternative — a concurrent collection — would give up the order MSBuild reported them
        // in, which is the order they make sense in.
        using IDisposable reported = workspace.RegisterWorkspaceFailedHandler(
            failure => { lock (reporting) { diagnostics.Add(failure.Diagnostic.Message); } });

        try {
            Project project = await workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken)
                                             .ConfigureAwait(false);
            Compilation? compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            if (compilation is null) {
                return LoadedProject.Failed([.. Reported(), $"{projectPath} produced no C# compilation."]);
            }

            return LoadedProject.Opened(compilation, Reported());
        } catch (Exception failure) when (failure is InvalidOperationException or System.IO.IOException) {
            return LoadedProject.Failed([.. Reported(), failure.Message]);
        }

        List<string> Reported() {
            lock (reporting) { return [.. diagnostics]; }
        }
    }

}
