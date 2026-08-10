using System;
using System.Reflection;

using Spectre.Console;
using Spectre.Console.Cli;

namespace JustDummies.Cli;

/// <summary>
///     Builds the tool's command line and runs one invocation.
/// </summary>
/// <remarks>
///     <c>generate</c> is the only verb (§3), and everything it does happens in <see cref="GenerateCommand" />;
///     what is decided here is the surface around it — the application name a help screen carries, strict
///     parsing, and the fact that a command line the tool could not read exits with the tool's own code rather
///     than the framework's.
///     <para>
///         Two consoles rather than one: §6 has <c>--dry-run</c> print the file to stdout and the recap to
///         stderr, so the split is part of the specification. They are parameters so the suite reads what the
///         tool says without capturing a process's console.
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
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        // The one thing a command needs and cannot reach for itself. Spectre constructs the command, so the
        // consoles are handed to it the way any dependency is: registered, then taken as a constructor
        // argument.
        ToolTypeRegistrar registrar = new();

        registrar.RegisterInstance(typeof(ToolConsoles), new ToolConsoles(output, error));

        CommandApp app = new(registrar);

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

            config.AddCommand<GenerateCommand>("generate")
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
