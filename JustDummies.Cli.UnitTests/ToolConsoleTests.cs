using System.IO;

using NFluent;

using Spectre.Console;

namespace JustDummies.Cli.UnitTests;

/// <summary>
///     The console the tool writes through, when nothing is attached to read it.
/// </summary>
/// <remarks>
///     A regression, not a preference. Built from the defaults, a console asks the terminal for its width and is
///     told there is none — a pipe, a file, a CI log — and then wraps every line to that. `dum --help` printed
///     `……` and six bytes, with a zero exit code: a run that looks successful and says nothing. Every output the
///     specification describes, the §6 recap included, travels this way.
/// </remarks>
public sealed class ToolConsoleTests {

    [Fact(DisplayName = "A console over a redirected output is still wide enough to read.")]
    public void ARedirectedConsoleIsStillWideEnoughToRead() {
        IAnsiConsole console = ToolConsole.On(new StringWriter());

        Check.That(console.Profile.Width).IsEqualTo(ToolConsole.WidthWhenRedirected);
    }

    [Fact(DisplayName = "A redirected console writes what it was given, in full.")]
    public void ARedirectedConsoleWritesWhatItWasGiven() {
        StringWriter writer  = new();
        IAnsiConsole console = ToolConsole.On(writer);

        console.WriteLine("the same sentence, forty-two characters");

        Check.That(writer.ToString().Trim()).IsEqualTo("the same sentence, forty-two characters");
    }

}
