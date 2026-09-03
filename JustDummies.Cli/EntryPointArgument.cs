using System;

using JustDummies.GenDummy;

namespace JustDummies.Cli;

/// <summary>
///     Reads <c>--entry-point</c> and <c>--entry-point-namespace</c>, and says why it could not.
/// </summary>
/// <remarks>
///     A type of its own because the same two strings are read twice: once by
///     <see cref="GenerateSettings.Validate" />, which turns a refusal into exit <c>2</c> before anything runs,
///     and once by the command, which needs the options themselves. Parsing is pure and cheap, so reading them
///     twice costs nothing and keeps the settings free of state Spectre would have to carry.
///     <para>
///         Every refusal here is a <b>command line</b> that could not be read, never a scaffold that failed:
///         the tool has not opened a project yet, and §7 keeps those two apart.
///     </para>
/// </remarks>
internal sealed class EntryPointArgument {

    /// <summary>The value that asks for the library's own façade.</summary>
    private const string DummyValue = "dummy";

    /// <summary>The value that asks for nothing, which is also what omitting the option means.</summary>
    private const string NoneValue = "none";

    /// <summary>What a static root is spelled with: <c>static:Dummies</c>.</summary>
    private const string StaticPrefix = "static:";

    private EntryPointArgument(EntryPointOptions options, string? refusal) {
        Options = options;
        Refusal = refusal;
    }

    /// <summary>What was asked for, or <see cref="EntryPointOptions.None" /> when it could not be read.</summary>
    internal EntryPointOptions Options { get; }

    /// <summary>Why the command line could not be read, or null when it could.</summary>
    internal string? Refusal { get; }

    /// <summary>Whether the two options were understood.</summary>
    internal bool Understood => Refusal is null;

    /// <summary>
    ///     Reads the pair.
    /// </summary>
    /// <param name="entryPoint">The <c>--entry-point</c> value, or null when it was omitted.</param>
    /// <param name="entryPointNamespace">The <c>--entry-point-namespace</c> value, or null when omitted.</param>
    internal static EntryPointArgument Parse(string? entryPoint, string? entryPointNamespace) {
        EntryPointArgument read = Read(entryPoint);

        if (!read.Understood) { return read; }

        if (entryPointNamespace is null) { return read; }

        // Not silently ignored: a namespace given with nothing to place is a command line whose author expected
        // a file somewhere, and would go looking for it.
        if (read.Options.Kind == EntryPointKind.None) {
            return Refused("--entry-point-namespace places the entry-point file, and no entry point was asked for. "
                         + $"Add --entry-point static:<Name> or --entry-point {DummyValue}.");
        }

        return new EntryPointArgument(read.Options.InNamespace(entryPointNamespace), refusal: null);
    }

    private static EntryPointArgument Read(string? entryPoint) {
        if (entryPoint is null || entryPoint == NoneValue) { return Understandable(EntryPointOptions.None); }

        if (entryPoint == DummyValue) { return Understandable(EntryPointOptions.OnDummy); }

        if (!entryPoint.StartsWith(StaticPrefix, StringComparison.Ordinal)) {
            return Refused($"--entry-point does not take '{entryPoint}'. It takes {NoneValue}, "
                         + $"{StaticPrefix}<Name>, or {DummyValue}.");
        }

        string root = entryPoint.Substring(StaticPrefix.Length);

        if (root.Length == 0) {
            return Refused($"--entry-point {StaticPrefix}<Name> names the static class the entry points hang off, "
                         + $"as in {StaticPrefix}Dummies.");
        }

        // The engine owns both rules — an identifier is an identifier, and 'Dummy' is the one name that hides the
        // library instead of extending it — so they are asked rather than restated, and the sentence a developer
        // reads is written here where the option is.
        try {
            return Understandable(EntryPointOptions.OnStaticRoot(root));
        } catch (ArgumentException) {
            return Refused(root == EntryPointOptions.ReservedRootName
                               ? $"--entry-point {StaticPrefix}{EntryPointOptions.ReservedRootName} would declare a "
                               + "static class that hides JustDummies.Dummy for its whole namespace, and Dummy.Int32() "
                               + $"would stop compiling. Use --entry-point {DummyValue} to hang the entry point off "
                               + "the library's own Dummy instead."
                               : $"--entry-point {StaticPrefix}{root} does not name a class: '{root}' is not a C# identifier.");
        }
    }

    private static EntryPointArgument Understandable(EntryPointOptions options) {
        return new EntryPointArgument(options, refusal: null);
    }

    private static EntryPointArgument Refused(string refusal) {
        return new EntryPointArgument(EntryPointOptions.None, refusal);
    }

}
