using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JustDummies.GenDummy;

/// <summary>
///     Renders the entry-point file of §4.5 — the one that lets a scaffolded generator be reached as
///     <c>Dummies.Order()</c> or <c>Dummy.Order()</c> rather than only as <c>new DummyOrder()</c>.
/// </summary>
/// <remarks>
///     A file of its own, never a member folded into the generator, and that is the whole design. One scaffold
///     writes one part; the parts never meet on disk, so nothing here reads a file to add a member to it and
///     §8.1's byte-identity survives a project with forty generators in it.
///     <para>
///         It is also what keeps §4.4 intact. <see cref="EntryPointKind.Dummy" /> needs C# 14, which the generator
///         file may not use; confining it here means the language floor is raised for the file that asked for
///         it and for no other.
///     </para>
/// </remarks>
public static class EntryPointEmitter {

    /// <summary>Fixed, not <see cref="Environment" />'s — §8.1, and the same reason as the generator's.</summary>
    private const string Newline = "\n";

    private const string Indent = "    ";

    /// <summary>
    ///     Emits the entry point for <paramref name="plan" />, or null when none was asked for.
    /// </summary>
    /// <param name="plan">The scaffold the entry point reaches.</param>
    /// <param name="entryPoint">Which entry point to emit, and where to declare it.</param>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static ScaffoldedEntryPoint? Emit(ScaffoldPlan plan, EntryPointOptions entryPoint) {
        if (plan is null) { throw new ArgumentNullException(nameof(plan)); }
        if (entryPoint is null) { throw new ArgumentNullException(nameof(entryPoint)); }

        if (entryPoint.Kind == EntryPointKind.None) { return null; }

        string  method          = MethodNameFor(plan);
        string? entryNamespace  = entryPoint.NamespaceOverride ?? plan.Target.Namespace;
        bool    block           = StyleOf(plan, entryNamespace) == NamespaceStyle.Block;

        StringBuilder file = new();

        WriteHeader(file, plan, entryPoint);
        WriteUsings(file, Usings(plan, entryPoint, entryNamespace));
        WriteNamespaceOpening(file, entryNamespace, block);
        WriteBody(file, plan, entryPoint, method, block ? Indent : string.Empty);
        WriteNamespaceClosing(file, entryNamespace, block);

        return new ScaffoldedEntryPoint(new ScaffoldedFile(plan.GeneratorName + ".Entry.cs", file.ToString(), containsTodo: false),
                                        $"{entryPoint.Root}.{method}()");
    }

    /// <summary>
    ///     The name the entry point takes: the target's own, never its container's.
    /// </summary>
    /// <remarks>
    ///     <see cref="TargetType.Name" /> is the target as C# reads it from inside its namespace, so a nested
    ///     type arrives as <c>Order.Line</c> — which is not a method name. Taking the last segment is the same
    ///     rule §3.2 already applies to the generator's own name, where <c>Order.Line</c> becomes
    ///     <c>DummyLine</c>: reading a name the plan supplies, not resolving anything of its own.
    /// </remarks>
    private static string MethodNameFor(ScaffoldPlan plan) {
        string name  = plan.Target.Name;
        int    nested = name.LastIndexOf('.');

        return nested < 0 ? name : name.Substring(nested + 1);
    }

    /// <summary>
    ///     Exactly three comment lines, like the generator's (§4.3), and naming the option that produced this
    ///     file — a developer who meets it in a diff should not have to work out which flag wrote it.
    /// </summary>
    private static void WriteHeader(StringBuilder file, ScaffoldPlan plan, EntryPointOptions entryPoint) {
        string option = entryPoint.Kind == EntryPointKind.Dummy
                            ? "any"
                            : "static:" + entryPoint.Root;

        Line(file, "// Scaffolded by dum (JustDummies). This file is yours: read it, edit it, commit it.");
        Line(file, $"// `dum generate {plan.Target.Name} --entry-point {option} --force` overwrites it.");

        Line(file, entryPoint.Kind == EntryPointKind.Dummy
                       ? "// It needs C# 14: a static extension member is what reaches this spelling without touching the library."
                       : "// The root is partial, so every other type's entry point lands in its own file beside this one.");

        Line(file, string.Empty);
    }

    /// <summary>
    ///     What this file has to open: the library for <see cref="EntryPointKind.Dummy" />, and the generator's
    ///     namespace whenever the entry point was moved away from it.
    /// </summary>
    private static IReadOnlyList<string> Usings(ScaffoldPlan plan, EntryPointOptions entryPoint, string? entryNamespace) {
        List<string> usings = [];

        if (entryPoint.Kind == EntryPointKind.Dummy) { usings.Add("JustDummies"); }

        if (plan.Target.Namespace is not null && plan.Target.Namespace != entryNamespace) {
            usings.Add(plan.Target.Namespace);
        }

        return usings;
    }

    private static void WriteUsings(StringBuilder file, IReadOnlyList<string> usings) {
        if (usings.Count == 0) { return; }

        foreach (string @using in usings.Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal)) {
            Line(file, $"using {@using};");
        }

        Line(file, string.Empty);
    }

    /// <summary>
    ///     The namespace form, copied from the target type's own file like the generator's (§4.4) — except
    ///     where the entry point was moved into a namespace the target has no declaration for, and the block
    ///     form is what compiles at every language version.
    /// </summary>
    private static NamespaceStyle StyleOf(ScaffoldPlan plan, string? entryNamespace) {
        if (entryNamespace is null) { return NamespaceStyle.None; }

        return plan.Target.Style == NamespaceStyle.None ? NamespaceStyle.Block : plan.Target.Style;
    }

    private static void WriteNamespaceOpening(StringBuilder file, string? entryNamespace, bool block) {
        if (entryNamespace is null) { return; }

        Line(file, block ? $"namespace {entryNamespace} {{" : $"namespace {entryNamespace};");
        Line(file, string.Empty);
    }

    private static void WriteNamespaceClosing(StringBuilder file, string? entryNamespace, bool block) {
        if (entryNamespace is null || !block) { return; }

        Line(file, string.Empty);
        Line(file, "}");
    }

    private static void WriteBody(StringBuilder file,
                                  ScaffoldPlan plan,
                                  EntryPointOptions entryPoint,
                                  string method,
                                  string indent) {
        if (entryPoint.Kind == EntryPointKind.Dummy) {
            WriteExtension(file, plan, method, indent);

            return;
        }

        WriteStaticRoot(file, plan, entryPoint, method, indent);
    }

    /// <summary>
    ///     The root is documented on every part, which is deliberate: each part is the whole declaration as far
    ///     as a reader of that file is concerned, and repeating one sentence costs less than a public type with
    ///     no documentation in a project that generates a documentation file. Duplicate summaries across
    ///     partial parts compile without a warning — verified.
    /// </summary>
    private static void WriteStaticRoot(StringBuilder file,
                                        ScaffoldPlan plan,
                                        EntryPointOptions entryPoint,
                                        string method,
                                        string indent) {
        Line(file, $"{indent}/// <summary>Reaches this project's scaffolded generators through one root.</summary>");
        Line(file, $"{indent}public static partial class {entryPoint.Root} {{");
        Line(file, string.Empty);

        WriteFactory(file, plan, method, indent + Indent);

        Line(file, $"{indent}}}");
    }

    /// <summary>
    ///     The <c>extension</c> block of C# 14, which is what puts <c>Order()</c> on a type declared in another
    ///     assembly. A <c>partial</c> part could not: partial declarations do not cross an assembly boundary,
    ///     and a second <c>Dummy</c> declared here would hide the library's rather than extend it.
    /// </summary>
    private static void WriteExtension(StringBuilder file, ScaffoldPlan plan, string method, string indent) {
        Line(file, $"{indent}/// <summary>Hangs <c>Dummy.{method}()</c> off the library's own entry point.</summary>");
        Line(file, $"{indent}public static class {plan.GeneratorName}Entry {{");
        Line(file, string.Empty);
        Line(file, $"{indent}{Indent}extension(Dummy) {{");
        Line(file, string.Empty);

        WriteFactory(file, plan, method, indent + Indent + Indent);

        Line(file, $"{indent}{Indent}}}");
        Line(file, string.Empty);
        Line(file, $"{indent}}}");
    }

    /// <summary>
    ///     The one member either kind carries: a fresh generator, in its default recipe, for the caller to
    ///     constrain. It returns the generator rather than a value on purpose — <c>Generate()</c> is the
    ///     developer's call to make, and a factory that drew one immediately would put a value where the
    ///     library's whole grammar puts a recipe.
    /// </summary>
    private static void WriteFactory(StringBuilder file, ScaffoldPlan plan, string method, string indent) {
        Line(file, $"{indent}/// <summary>Starts an arbitrary <c>{plan.Target.Name}</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>");
        Line(file, $"{indent}public static {plan.GeneratorName} {method}() {{");
        Line(file, $"{indent}{Indent}return new {plan.GeneratorName}();");
        Line(file, $"{indent}}}");
        Line(file, string.Empty);
    }

    private static void Line(StringBuilder file, string text) {
        file.Append(text).Append(Newline);
    }

}
