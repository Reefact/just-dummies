using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using Spectre.Console;

namespace JustDummies.Cli;

/// <summary>
///     Puts a <see cref="RunReport" /> on stdout, as the one document <c>--format json</c> promises (§6.1).
/// </summary>
/// <remarks>
///     Indented rather than compact, and with a fixed key order — the declaration order of the record — because
///     the reader is as often a person as a script: two runs of the same command produce two texts a diff can
///     line up. Nothing here reads the clock, the culture or the machine, for the same reason nothing in the
///     emitter does (§8.1).
/// </remarks>
internal static class JsonReport {

    /// <summary>
    ///     Serialization settings, fixed once.
    /// </summary>
    /// <remarks>
    ///     <see cref="JavaScriptEncoder.UnsafeRelaxedJsonEscaping" /> is the load-bearing one, and it is not
    ///     unsafe here: the default encoder escapes <c>&lt;</c>, <c>&gt;</c>, <c>&amp;</c> and every non-ASCII
    ///     character, so a generic parameter list would come back as <c>IDummy<string></c> and the
    ///     <c>—</c> of a recap as an escape. That output is valid JSON and unreadable, and the escaping buys
    ///     nothing the tool needs: this text goes to a pipe, never into an HTML document. Nulls are kept rather
    ///     than dropped so that every key a consumer reads is always present.
    /// </remarks>
    private static readonly JsonSerializerOptions Settings = new() {
        WriteIndented          = true,
        PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder                = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>Writes <paramref name="report" /> to <paramref name="console" />, and a newline after it.</summary>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    internal static void Write(RunReport report, IAnsiConsole console) {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(console);

        Unwrapped.Line(console, JsonSerializer.Serialize(report, Settings));
    }

}
