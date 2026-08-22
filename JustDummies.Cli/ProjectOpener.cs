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
///     Opening the project named by <paramref name="projectPath" />.
/// </summary>
/// <remarks>
///     A delegate rather than a direct call, so the one step of the pipeline that needs MSBuild, a restored
///     project and a disk is the one step a test can stand in for. Everything after it — resolution, emission,
///     the recap, the exit codes of §7 — is then exercised against a compilation built in memory, which is the
///     only way those rows are checkable at all.
/// </remarks>
internal delegate Task<LoadedProject> ProjectOpener(string projectPath, CancellationToken cancellationToken);
