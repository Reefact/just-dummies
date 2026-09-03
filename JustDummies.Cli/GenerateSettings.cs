using System.ComponentModel;

using Spectre.Console;
using Spectre.Console.Cli;

namespace JustDummies.Cli;

/// <summary>
///     The command line of <c>dum generate</c>, as specified in §3 — and the whole of it: there is no
///     <c>init</c>, no <c>list</c>, no <c>--all</c> and no <c>check</c>. The one file read besides it,
///     <c>dum.json</c> (§3.3), supplies defaults for these same options and is validated through the same
///     <see cref="Validate" /> — it can never widen this surface.
/// </summary>
internal sealed class GenerateSettings : CommandSettings {

    /// <summary>The recap of §6, written for a reader — what a run reports unless told otherwise.</summary>
    internal const string HumanFormat = "human";

    /// <summary>One JSON document on stdout, written for a script (§6.1).</summary>
    internal const string JsonFormat = "json";

    /// <summary>
    ///     The types to scaffold, written as a developer would type them: <c>Order</c>, <c>Shop.Domain.Order</c>,
    ///     or <c>Order.Line</c> for a nested one (§3.2). Several are processed independently (§7).
    /// </summary>
    [CommandArgument(0, "<TYPE>")]
    [Description("The type to scaffold a generator for. Repeat it to scaffold several.")]
    public string[] Types { get; set; } = [];

    /// <summary>Defaults to the single <c>*.csproj</c> in the current directory (§3.1).</summary>
    [CommandOption("--project <PATH>")]
    [Description("Project whose compilation is analyzed. Defaults to the only .csproj here.")]
    public string? Project { get; set; }

    /// <summary>Defaults to the current directory, which is where the tests that use the file live (§3.1).</summary>
    [CommandOption("--output <DIR>")]
    [Description("Where the file is written. Defaults to the current directory.")]
    public string? Output { get; set; }

    /// <summary>Defaults to the target type's own namespace (ADR-0062).</summary>
    [CommandOption("--namespace <NAMESPACE>")]
    [Description("Namespace of the emitted type. Defaults to the target type's own.")]
    public string? Namespace { get; set; }

    /// <summary>
    ///     Off by default, so the generator file is all a scaffold writes and <c>new Dummy{Type}()</c> stays the
    ///     way in (§4.5).
    /// </summary>
    [CommandOption("--entry-point <VALUE>")]
    [Description("Also emit an entry point: none, static:<Name>, or any. Defaults to none.")]
    public string? EntryPoint { get; set; }

    /// <summary>Defaults to the emitted generator's own namespace, which costs the call site no import.</summary>
    [CommandOption("--entry-point-namespace <NAMESPACE>")]
    [Description("Namespace of the entry-point file. Defaults to the emitted type's.")]
    public string? EntryPointNamespace { get; set; }

    /// <summary>Off by default: an existing file is never overwritten silently (§7).</summary>
    [CommandOption("--force")]
    [Description("Overwrite an existing file. Your edits to it are lost.")]
    public bool Force { get; set; }

    /// <summary>Prints the file to stdout and the recap to stderr, writing nothing (§6).</summary>
    [CommandOption("--dry-run")]
    [Description("Print the file instead of writing it.")]
    public bool DryRun { get; set; }

    /// <summary>
    ///     Defaults to the recap of §6, which is written for a reader; <c>json</c> is written for a script
    ///     (§6.1).
    /// </summary>
    [CommandOption("--format <FORMAT>")]
    [Description("How the run reports itself: human or json. Defaults to human.")]
    public string? Format { get; set; }

    /// <summary>
    ///     Refuses an option that was given without a value.
    /// </summary>
    /// <remarks>
    ///     <c>--namespace ""</c> is not "no namespace override": it is an override to nothing, and the three
    ///     options below would each answer it differently and late — a path routine throwing, a namespace
    ///     declaration emitted empty. Refused here instead, where it is what it is: a command line the tool
    ///     could not read, which is exit <c>2</c> and not a scaffolding failure.
    /// </remarks>
    public override ValidationResult Validate() {
        foreach ((string option, string? value) in new[] {
                     ("--project", Project), ("--output", Output), ("--namespace", Namespace),
                     ("--entry-point", EntryPoint), ("--entry-point-namespace", EntryPointNamespace),
                     ("--format", Format)
                 }) {
            if (value is not null && value.Trim().Length == 0) {
                // "Omit it to take its default" stopped being true the day a dum.json could set the same
                // option: omitting it now takes that file's value where there is one (§3.3).
                string advice = option == "--project"
                                    ? "Omit it to take its default."
                                    : $"Omit it to take {ProjectDefaults.FileName}'s value, or the built-in "
                                    + "default where that file sets nothing.";

                return ValidationResult.Error($"{option} was given without a value. {advice}");
            }
        }

        if (Format is not null && Format != HumanFormat && Format != JsonFormat) {
            return ValidationResult.Error($"--format does not take '{Format}'. It takes {HumanFormat} or {JsonFormat}.");
        }

        EntryPointArgument entryPoint = ReadEntryPoint();

        return entryPoint.Understood ? ValidationResult.Success() : ValidationResult.Error(entryPoint.Refusal!);
    }

    /// <summary>Whether the run reports itself as one JSON document rather than as the recap of §6.</summary>
    internal bool ReportsAsJson() {
        return Format == JsonFormat;
    }

    /// <summary>
    ///     What <c>--entry-point</c> and <c>--entry-point-namespace</c> asked for, or why they could not be read.
    /// </summary>
    /// <remarks>
    ///     A method rather than a property: it parses two strings and allocates, and a caller reading a property
    ///     twice would expect the second read to cost what the first did.
    /// </remarks>
    internal EntryPointArgument ReadEntryPoint() {
        return EntryPointArgument.Parse(EntryPoint, EntryPointNamespace);
    }

}
