using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace JustDummies.Cli;

/// <summary>
///     The optional <c>dum.json</c> beside the project, and the defaults it sets (§3.3).
/// </summary>
/// <remarks>
///     §3 said "there is no config file", and meant it for as long as every option was a per-invocation
///     decision. What changed is that some of them stopped being one: a team that wants its generators in
///     <c>./Dummies</c>, reached through one root, is stating a property of the project, and re-typing it on
///     every invocation is how it comes to be typed differently on one of them.
///     <para>
///         Read by the shell and folded into the settings before the engine is called, so the engine keeps
///         knowing nothing of MSBuild, of the disk, or of this file (ADR-0065).
///     </para>
/// </remarks>
internal sealed class ProjectDefaults {

    /// <summary>The file's name, fixed by §16 when it reserved it.</summary>
    internal const string FileName = "dum.json";

    /// <summary>
    ///     The keys this version reads.
    /// </summary>
    /// <remarks>
    ///     One per option that exists. <c>naming</c> is deliberately <b>not</b> among them: §16 reserves it for
    ///     <c>--name</c> and <c>--pattern</c>, which are not implemented, and a key that configured nothing
    ///     would be worse than a key that is refused.
    /// </remarks>
    private static readonly string[] Known = [
        "output", "namespace", "entryPoint", "entryPointNamespace", "format"
    ];

    private ProjectDefaults(IReadOnlyDictionary<string, string> values, string? refusal) {
        Values  = values;
        Refusal = refusal;
    }

    /// <summary>What the file set, by key.</summary>
    internal IReadOnlyDictionary<string, string> Values { get; }

    /// <summary>Why the file could not be read, or null when it could — or when there was none.</summary>
    internal string? Refusal { get; }

    /// <summary>Whether the file, if any, was understood.</summary>
    internal bool Understood => Refusal is null;

    /// <summary>
    ///     Reads the <c>dum.json</c> beside <paramref name="projectPath" />, if there is one.
    /// </summary>
    /// <remarks>
    ///     Beside the project file rather than in the current directory, because that is what makes it a
    ///     property of the project: it is committed next to the <c>.csproj</c>, and it applies wherever the
    ///     developer happens to have run the tool from.
    /// </remarks>
    internal static ProjectDefaults Beside(string projectPath) {
        ArgumentNullException.ThrowIfNull(projectPath);

        string path = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(projectPath)) ?? string.Empty, FileName);

        if (!File.Exists(path)) { return new ProjectDefaults(new Dictionary<string, string>(StringComparer.Ordinal), refusal: null); }

        try {
            return Read(File.ReadAllText(path), path);
        } catch (JsonException malformed) {
            return Refused($"{path} is not readable as JSON: {malformed.Message}");
        } catch (IOException unreadable) {
            return Refused($"{path} could not be read: {unreadable.Message}");
        }
    }

    /// <summary>
    ///     Every default that has not been overridden on the command line, folded into
    ///     <paramref name="settings" />.
    /// </summary>
    /// <remarks>
    ///     The command line always wins, and it wins by simply already being there: a value the developer typed
    ///     is non-null, and nothing here overwrites one. That is the whole precedence rule, and it is one
    ///     sentence on purpose — a config file whose interaction with the flags needs a table is a config file
    ///     nobody will trust.
    ///     <para>
    ///         A relative <c>output</c> is resolved against the project's own directory, not the current one.
    ///         A path typed on the command line is relative to where it was typed; a path committed in this
    ///         file has to mean the same thing wherever the tool is run from, or the default is not one.
    ///     </para>
    /// </remarks>
    internal void ApplyTo(GenerateSettings settings, string projectPath) {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(projectPath);

        string root = Path.GetDirectoryName(Path.GetFullPath(projectPath)) ?? string.Empty;

        settings.Output              ??= Rooted(Value("output"), root);
        settings.Namespace           ??= Value("namespace");
        settings.EntryPoint          ??= Value("entryPoint");
        settings.EntryPointNamespace ??= Value("entryPointNamespace");
        settings.Format              ??= Value("format");
    }

    private static ProjectDefaults Read(string json, string path) {
        Dictionary<string, string> values = new(StringComparer.Ordinal);

        using JsonDocument document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind != JsonValueKind.Object) {
            return Refused($"{path} has to be a JSON object: {{ \"output\": \"./Dummies\" }}.");
        }

        foreach (JsonProperty property in document.RootElement.EnumerateObject()) {
            // Refused rather than ignored, and this is the reason the file is worth having at all: a key
            // silently skipped is a default the developer believes is in force and is not, which is a worse
            // state than having no file. §16's own 'naming' key lands here until it configures something.
            if (!Known.Contains(property.Name, StringComparer.Ordinal)) {
                return Refused($"{path} sets '{property.Name}', which {FileName} does not read. "
                             + $"It reads: {string.Join(", ", Known)}.");
            }

            if (property.Value.ValueKind != JsonValueKind.String) {
                return Refused($"{path} sets '{property.Name}' to something other than a string.");
            }

            values[property.Name] = property.Value.GetString()!;
        }

        return new ProjectDefaults(values, refusal: null);
    }

    private static ProjectDefaults Refused(string refusal) {
        return new ProjectDefaults(new Dictionary<string, string>(StringComparer.Ordinal), refusal);
    }

    private string? Value(string key) {
        return Values.TryGetValue(key, out string? value) ? value : null;
    }

    private static string? Rooted(string? output, string root) {
        return output is null ? null : Path.GetFullPath(Path.Combine(root, output));
    }

}
