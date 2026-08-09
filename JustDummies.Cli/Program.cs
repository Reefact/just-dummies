using System;

namespace JustDummies.Cli;

/// <summary>
///     The process entry point, and nothing else: it binds the two real consoles to
///     <see cref="ToolCommandLine" /> and returns what that returns, so every decision the tool makes is
///     reachable from a test.
/// </summary>
internal static class Program {

    private static int Main(string[] args) {
        return ToolCommandLine.Run(args, ToolConsole.On(Console.Out), ToolConsole.On(Console.Error));
    }

}
