using System.ComponentModel;

using Spectre.Console.Cli;

namespace JustDummies.Cli;

/// <summary>
///     The command line of <c>dum generate</c>, as specified in §3 — and the whole of it: there is no config
///     file, no <c>init</c>, no <c>list</c>, no <c>--all</c> and no <c>check</c>.
/// </summary>
/// <remarks>
///     Declared in full although nothing reads it yet, because this is the surface the specification fixes and the
///     one a developer will meet first. What is missing is the behaviour behind it, not the shape.
/// </remarks>
internal sealed class GenerateSettings : CommandSettings {

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

    /// <summary>Off by default: an existing file is never overwritten silently (§7).</summary>
    [CommandOption("--force")]
    [Description("Overwrite an existing file. Your edits to it are lost.")]
    public bool Force { get; set; }

    /// <summary>Prints the file to stdout and the recap to stderr, writing nothing (§6).</summary>
    [CommandOption("--dry-run")]
    [Description("Print the file instead of writing it.")]
    public bool DryRun { get; set; }

}
