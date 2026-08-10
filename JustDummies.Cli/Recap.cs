using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

using JustDummies.GenAny;

using Spectre.Console;

namespace JustDummies.Cli;

/// <summary>
///     The console recap of §6.
/// </summary>
/// <remarks>
///     Not decoration: it is the mechanism that keeps the tool honest about what it inferred and what it
///     guessed. A developer who cannot tell "inferred, and here is why" from "gave up" has to re-derive the
///     whole file by hand, and the tool has bought them nothing.
///     <para>
///         Rendering only. Every fact it prints — the provenance of each expression, the count of open
///         parameters, the shadowed name — is decided by the engine and carried in its result model, which is
///         what makes the recap testable without a console and what an IDE consumer would ignore entirely.
///     </para>
///     <para>
///         Written through the console's own writer rather than through Spectre's markup, and with no colour.
///         Spectre wraps what it prints to the terminal's width, which reads well for a sentence and destroys a
///         table: at eighty columns the parameter rows fold in half and the columns stop lining up. A terminal
///         soft-wraps a long line on its own, and a developer can widen it; the tool folding the table for them
///         is not a service. The line ending is fixed for the same reason the emitter's is — so the recap reads
///         the same, and is checkable the same, on every platform.
///     </para>
/// </remarks>
[SuppressMessage(SonarRule.S1135.Category, SonarRule.S1135.Id, Justification = "Names the marker the tool emits by design (§5.5) and prints in the recap's right-hand column (§6), not unfinished work here.")]
internal static class Recap {

    /// <summary>What an open parameter shows where an expression would be.</summary>
    private const string NoExpression = "—";

    /// <summary>The order the provenance column reads in, so two runs never word the same facts differently.</summary>
    private static readonly (GenAny.Provenance Flag, string Word)[] Words = [
        (GenAny.Provenance.Scaffolded, "AnyX"),
        (GenAny.Provenance.Factory, "factory"),
        (GenAny.Provenance.Guard, "guard"),
        (GenAny.Provenance.GuardsNotCombined, "guards not combined"),
        (GenAny.Provenance.UnreadGuards, "unread guards"),
        (GenAny.Provenance.NoSource, "no source"),
        (GenAny.Provenance.Unavailable, "unavailable")
    ];

    /// <summary>Renders one scaffold, as §6 writes it.</summary>
    internal static void Render(ScaffoldOutcome outcome, IAnsiConsole console) {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(console);
        if (!outcome.Succeeded) { return; }

        ScaffoldPlan plan = outcome.Plan!;

        Line(console, $"Analyzing {FullName(plan)}");
        Line(console, $"  constructor {plan.Target.Name}({string.Join(", ", plan.Parameters.Select(p => p.TypeDisplay))})");

        if (plan.Parameters.Count > 0) {
            Line(console, string.Empty);

            foreach (string row in Rows(plan)) { Line(console, row); }
        }

        Line(console, string.Empty);

        foreach (ScaffoldWarning warning in outcome.Warnings) { Warn(warning, console); }

        Closing(outcome, console);
    }

    /// <summary>
    ///     One line per parameter, in columns: name, type, expression, and where it came from.
    /// </summary>
    /// <remarks>
    ///     Written through a builder rather than a table widget so the layout is the specification's, to the
    ///     space — and so a trailing column that is empty leaves no trailing space behind it.
    /// </remarks>
    private static IEnumerable<string> Rows(ScaffoldPlan plan) {
        int name       = plan.Parameters.Max(parameter => parameter.Name.Length);
        int type       = plan.Parameters.Max(parameter => parameter.TypeDisplay.Length);
        int expression = plan.Parameters.Max(parameter => Expression(parameter).Length);

        foreach (ScaffoldedParameter parameter in plan.Parameters) {
            StringBuilder row = new();

            row.Append("  ")
               .Append(parameter.Name.PadRight(name))
               .Append("  ")
               .Append(parameter.TypeDisplay.PadRight(type))
               .Append("  ");

            string provenance = Column(parameter);

            row.Append(provenance.Length == 0 ? Expression(parameter) : Expression(parameter).PadRight(expression))
               .Append(provenance.Length == 0 ? string.Empty : "  " + provenance);

            yield return row.ToString();
        }
    }

    private static string Expression(ScaffoldedParameter parameter) {
        return parameter.Expression ?? NoExpression;
    }

    /// <summary>
    ///     The right-hand column: empty for the base table, and never silent about a guess.
    /// </summary>
    /// <remarks>
    ///     <c>TODO</c> comes first on an open parameter because it is what the reader is scanning for; the rest
    ///     follows in a fixed order, so the same facts always read the same way.
    /// </remarks>
    private static string Column(ScaffoldedParameter parameter) {
        List<string> words = [];

        if (parameter.IsUnresolved) { words.Add("TODO"); }

        words.AddRange(Words.Where(word => parameter.Provenance.HasFlag(word.Flag)).Select(word => word.Word));

        return string.Join(", ", words);
    }

    /// <summary>
    ///     It compiles; it is just wrong later — which is exactly why this is said out loud (§7).
    /// </summary>
    private static void Warn(ScaffoldWarning warning, IAnsiConsole console) {
        Line(console, $"! {warning.Subject} shadows {warning.Other} inside its own namespace.");
        Line(console, "  It compiles, and every file in that namespace will resolve yours instead. Rename either one.");
        Line(console, string.Empty);
    }

    /// <summary>
    ///     The closing line, and the one sentence that keeps an open parameter from reading as a failure.
    /// </summary>
    private static void Closing(ScaffoldOutcome outcome, IAnsiConsole console) {
        ScaffoldPlan plan  = outcome.Plan!;
        int          open  = plan.Parameters.Count(parameter => parameter.IsUnresolved);
        int          total = plan.Parameters.Count;

        if (total == 0) {
            Line(console, $"✓ {outcome.File!.FileName} — no constructor parameters to infer.");

            return;
        }

        string counted = $"{total - open} of {total} parameters inferred"
                       + (open == 0 ? "." : $", {open.ToString(CultureInfo.InvariantCulture)} TODO.");

        Line(console, $"✓ {outcome.File!.FileName} — {counted}");

        if (open == 0) { return; }

        Line(console, "  The file will not compile until you resolve it. That is deliberate.");

        foreach (ScaffoldedParameter parameter in plan.Parameters.Where(p => p.Candidates.Count > 0)) {
            Line(console, $"  {parameter.Name}: several factories qualify — {string.Join(", ", parameter.Candidates)}.");
        }
    }

    /// <summary>One line, unwrapped, ending in the newline every platform reads the same.</summary>
    private static void Line(IAnsiConsole console, string text) {
        Unwrapped.Line(console, text);
    }

    private static string FullName(ScaffoldPlan plan) {
        return plan.Target.Namespace is null ? plan.Target.Name : plan.Target.Namespace + "." + plan.Target.Name;
    }

}
