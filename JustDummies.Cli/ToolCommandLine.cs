using System;
using System.Reflection;

using Spectre.Console;
using Spectre.Console.Cli;

namespace JustDummies.Cli;

/// <summary>
///     Builds the tool's command line and runs one invocation.
/// </summary>
/// <remarks>
///     The surface is complete and the behaviour is not: <c>generate</c> parses exactly what §3 specifies — every
///     option, every default described in its help — and then refuses, because no part of §4–§7 is implemented.
///     A tool that accepted the command and printed nothing would read, to a script, as a scaffolding run that
///     produced no file.
///     <para>
///         Two consoles rather than one, from the start: §6 has <c>--dry-run</c> print the file to stdout and the
///         recap to stderr, so the split is part of the specification and not a detail of this build. They are
///         parameters so the suite reads what the tool says without capturing a process's console.
///     </para>
/// </remarks>
internal static class ToolCommandLine {

    /// <summary>
    ///     Runs one invocation and returns the process exit code.
    /// </summary>
    /// <param name="args">The command-line arguments, as the runtime handed them over.</param>
    /// <param name="output">Where an answer the caller asked for is written.</param>
    /// <param name="error">Where a refusal is written.</param>
    /// <returns>One of <see cref="ExitCode" />.</returns>
    internal static int Run(string[] args, IAnsiConsole output, IAnsiConsole error) {
        if (args is null) { throw new ArgumentNullException(nameof(args)); }
        if (output is null) { throw new ArgumentNullException(nameof(output)); }
        if (error is null) { throw new ArgumentNullException(nameof(error)); }

        CommandApp app = new();

        app.Configure(config => {
            config.SetApplicationName("dum");
            config.SetApplicationVersion(Version);
            config.ConfigureConsole(output);

            // Refuse an unknown option rather than carry it into the run: a mistyped --forse must not read as
            // "no --force was given".
            config.UseStrictParsing();

            // Spectre's default is to render the exception and return -1. The exit codes are the tool's, not
            // the framework's, so a command line that could not be parsed comes back as Usage.
            config.SetExceptionHandler((exception, _) => {
                error.MarkupLineInterpolated($"[red]{exception.Message}[/]");

                return ExitCode.Usage;
            });

            // A delegate rather than a Command<T> class, and deliberately so: with no body to write, a class
            // would exist only to hold one refusal, and reaching the error console from it would mean standing
            // up a type registrar for a constructor argument nothing else needs yet. The settings — the part
            // the specification fixes — are a real type either way. When §4-§7 arrive, this becomes
            // GenerateCommand and the registrar comes with it.
            config.AddDelegate<GenerateSettings>("generate", (_, _, _) => {
                       error.MarkupLine("[red]`generate` is specified but not implemented yet.[/] "
                                      + "This build of dum answers --version and --help, and nothing else.");

                       return ExitCode.Usage;
                   })
                  .WithDescription("Write the dummy generator for a type, as code you then own.");
        });

        return app.Run(args);
    }

    /// <summary>
    ///     What <c>dum --version</c> answers: the informational version, without the commit hash a deterministic
    ///     build appends to it. A developer comparing their tool against a release note wants the number they can
    ///     find on nuget.org, not the build it came from.
    /// </summary>
    internal static string Version {
        get {
            string informational = typeof(ToolCommandLine).Assembly
                                                          .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                                        ?.InformationalVersion
                                ?? typeof(ToolCommandLine).Assembly.GetName().Version?.ToString()
                                ?? string.Empty;

            int buildMetadata = informational.IndexOf('+');

            return buildMetadata < 0 ? informational : informational.Substring(0, buildMetadata);
        }
    }

}
