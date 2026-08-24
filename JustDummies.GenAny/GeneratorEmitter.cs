using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JustDummies.GenAny;

/// <summary>
///     Renders a <see cref="ScaffoldPlan" /> as the file of §4.
/// </summary>
/// <remarks>
///     A string builder over an ordered model, not <c>SyntaxFactory</c> (§11.2). The output has to read like a
///     file a person wrote — aligned declarations, explicit types, the repository's brace style — and a
///     syntax-API rewrite normalises exactly that away. Golden files are what make the choice safe.
///     <para>
///         Everything here is fixed text or text taken from the plan. Nothing reads the clock, the machine, the
///         culture or a hash: the same plan produces the same bytes anywhere, which is what §8.1 promises and
///         what makes a re-scaffold reviewable as a diff.
///     </para>
/// </remarks>
public static class GeneratorEmitter {

    /// <summary>
    ///     The line ending, fixed rather than <see cref="Environment" />'s.
    /// </summary>
    /// <remarks>
    ///     §8.1 promises the same bytes on any machine. <c>Environment.NewLine</c> would break that between a
    ///     Windows developer and a Linux one on the same type and the same compilation — the diff nobody could
    ///     explain. Git's own <c>core.autocrlf</c> is the layer that adapts this to a platform, if a repository
    ///     wants it adapted.
    /// </remarks>
    private const string Newline = "\n";

    private const string Indent = "    ";

    /// <summary>
    ///     Emits the file for <paramref name="plan" />.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="plan" /> is null.</exception>
    public static ScaffoldedFile Emit(ScaffoldPlan plan) {
        if (plan is null) { throw new ArgumentNullException(nameof(plan)); }

        StringBuilder file = new();

        WriteHeader(file, plan);
        WriteUsings(file, plan);

        string bodyIndent = plan.Target.Style == NamespaceStyle.Block ? Indent : string.Empty;

        WriteNamespaceOpening(file, plan);
        WriteGenerator(file, plan, bodyIndent);
        WriteNamespaceClosing(file, plan);

        return new ScaffoldedFile(plan.GeneratorName + ".cs", file.ToString(), plan.ContainsTodo);
    }

    /// <summary>
    ///     Exactly three comment lines, and no timestamp or tool version among them (§4.3): either would make
    ///     the bytes depend on something other than the analyzed type, so every scaffold after an upgrade would
    ///     produce a diff that means nothing.
    /// </summary>
    private static void WriteHeader(StringBuilder file, ScaffoldPlan plan) {
        Line(file, "// Scaffolded by dum (JustDummies). This file is yours: read it, edit it, commit it.");
        Line(file, $"// `dum generate {plan.Target.Name} --force` overwrites it. This type is partial, so members you add in a");
        Line(file, "// neighbouring file survive.");
        Line(file, string.Empty);
    }

    /// <summary>
    ///     The <c>System</c> namespaces first, then everything else, each group ordered and separated by a blank
    ///     line — the layout the repository's own files use.
    /// </summary>
    private static void WriteUsings(StringBuilder file, ScaffoldPlan plan) {
        if (plan.Usings.Count == 0) { return; }

        string[] system = Ordered(plan.Usings.Where(IsSystem));
        string[] rest   = Ordered(plan.Usings.Where(@using => !IsSystem(@using)));

        foreach (string @using in system) { Line(file, $"using {@using};"); }

        if (system.Length > 0 && rest.Length > 0) { Line(file, string.Empty); }

        foreach (string @using in rest) { Line(file, $"using {@using};"); }

        Line(file, string.Empty);
    }

    private static void WriteNamespaceOpening(StringBuilder file, ScaffoldPlan plan) {
        if (plan.Target.Style == NamespaceStyle.None || plan.Target.Namespace is null) { return; }

        if (plan.Target.Style == NamespaceStyle.FileScoped) {
            Line(file, $"namespace {plan.Target.Namespace};");
            Line(file, string.Empty);

            return;
        }

        Line(file, $"namespace {plan.Target.Namespace} {{");
        Line(file, string.Empty);
    }

    private static void WriteNamespaceClosing(StringBuilder file, ScaffoldPlan plan) {
        if (plan.Target.Style != NamespaceStyle.Block || plan.Target.Namespace is null) { return; }

        Line(file, string.Empty);
        Line(file, "}");
    }

    private static void WriteGenerator(StringBuilder file, ScaffoldPlan plan, string indent) {
        WriteTypeDocumentation(file, plan, indent);

        Line(file, $"{indent}public sealed partial class {plan.GeneratorName} : IAny<{plan.Target.Name}> {{");
        Line(file, string.Empty);

        string member = indent + Indent;

        if (plan.IsDegenerate) {
            WriteDegenerateConstructor(file, plan, member);
        } else {
            WriteFields(file, plan, member);
            WritePublicConstructor(file, plan, member);
            WriteFactories(file, plan, member);
            WritePrivateConstructor(file, plan, member);
            WriteWithMethods(file, plan, member);
        }

        WriteGenerate(file, plan, member);

        if (!plan.IsDegenerate) { WriteFixedValue(file, member); }

        Line(file, $"{indent}}}");
    }

    /// <summary>
    ///     The one sentence a developer on <c>Any.WithSeed(…)</c> needs, in the place they will read it. An
    ///     <c>AnyContext</c> carries its own random source and the emitted expressions all come from the static
    ///     façade, so that developer passes their context's generators in through the <c>With</c> overloads
    ///     (ADR-0061).
    /// </summary>
    private static void WriteTypeDocumentation(StringBuilder file, ScaffoldPlan plan, string indent) {
        Line(file, $"{indent}/// <summary>");
        Line(file, $"{indent}///     A generator of arbitrary <see cref=\"{plan.Target.Name}\" /> values. It draws from the ambient random");
        Line(file, $"{indent}///     context, so a reproducibility scope pins it; to draw from an isolated");
        Line(file, $"{indent}///     <c>Any.WithSeed(...)</c> context, pass that context's generators through the");
        Line(file, $"{indent}///     <c>With…</c> overloads.");
        Line(file, $"{indent}/// </summary>");
    }

    private static void WriteFields(StringBuilder file, ScaffoldPlan plan, string indent) {
        int width = GeneratorTypeWidth(plan);

        foreach (ScaffoldedParameter parameter in plan.Parameters) {
            Line(file, $"{indent}private readonly {GeneratorTypeOf(parameter).PadRight(width)} {parameter.FieldName};");
        }

        Line(file, string.Empty);
    }

    /// <summary>
    ///     Named arguments, aligned, so a reader maps each call to its parameter without counting (§4.2). Each
    ///     one is a call to that parameter's own factory — never the chain itself, which is written once, in
    ///     that factory's body — except a composed parameter, whose whole recipe is the one call to the
    ///     generator its type owns, and which is therefore written here (ADR-0089).
    /// </summary>
    private static void WritePublicConstructor(StringBuilder file, ScaffoldPlan plan, string indent) {
        Line(file, $"{indent}/// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>");
        Line(file, $"{indent}public {plan.GeneratorName}()");

        int    width       = plan.Parameters.Max(parameter => parameter.Identifier.Length) + 1;
        string opening     = $"{indent}{Indent}: this(";
        string continuing  = new(' ', opening.Length);

        for (int index = 0; index < plan.Parameters.Count; index++) {
            ScaffoldedParameter parameter = plan.Parameters[index];
            bool                last      = index == plan.Parameters.Count - 1;
            string              lead      = index == 0 ? opening : continuing;
            string              drawn     = parameter.DrawnInline ? parameter.Expression! : parameter.FactoryMethodName + "()";
            string              argument  = $"{(parameter.Identifier + ":").PadRight(width)} {drawn}";

            Line(file, lead + argument + (last ? ") { }" : ","));
        }

        Line(file, string.Empty);
    }

    /// <summary>
    ///     One factory per parameter that has one (§4.2): the method the public constructor calls, and the place
    ///     a parameter with something to say for itself says it, right beside the chain it is about. A parameter
    ///     drawn inline has nothing to put in a body, so it gets no method (ADR-0089).
    /// </summary>
    private static void WriteFactories(StringBuilder file, ScaffoldPlan plan, string indent) {
        string body = indent + Indent;

        foreach (ScaffoldedParameter parameter in plan.Parameters) {
            if (parameter.DrawnInline) { continue; }

            Line(file, $"{indent}private static IAny<{parameter.TypeDisplay}> {parameter.FactoryMethodName}() {{");

            if (parameter.IsUnresolved) {
                WriteTodo(file, parameter, body);
                Line(file, $"{body}return {parameter.TodoIdentifier};");
            } else if (parameter.RequiresVerification) {
                WriteVerify(file, parameter, body);
                Line(file, $"{body}_ = {parameter.VerifyIdentifier};");
                Line(file, string.Empty);
                Line(file, $"{body}return {parameter.Expression};");
            } else {
                Line(file, $"{body}return {parameter.Expression};");
            }

            Line(file, $"{indent}}}");
            Line(file, string.Empty);
        }
    }

    /// <summary>
    ///     The identifier below does not exist, and that is the whole mechanism (ADR-0060): the developer's own
    ///     build reports it at this line, in the IDE and in CI, the minute the file is written.
    /// </summary>
    private static void WriteTodo(StringBuilder file, ScaffoldedParameter parameter, string indent) {
        Line(file, $"{indent}// TODO(dum): no generator inferred for '{parameter.TypeDisplay} {parameter.Name}'.");
        Line(file, $"{indent}//   Scaffold one:  dum generate {parameter.TypeDisplay}");
        Line(file, $"{indent}//   or write one here, or replace it and always pass .With{parameter.PascalCasedName}(...) instead.");
    }

    /// <summary>
    ///     The identifier below does not exist either, and blocks compilation the same way — but the return
    ///     beneath it is a working recipe, not a placeholder: this parameter's own generator, kept as the base
    ///     to check rather than thrown away over a doubt the engine cannot resolve on its own (ADR-0082).
    /// </summary>
    private static void WriteVerify(StringBuilder file, ScaffoldedParameter parameter, string indent) {
        Line(file, $"{indent}// TODO(dum): '{parameter.TypeDisplay} {parameter.Name}' may be guarded by something dum could not read (§9).");
        Line(file, $"{indent}//   This is dum's best generator for the type; verify it honours the real invariant,");
        Line(file, $"{indent}//   or replace it, then delete the line below.");
    }

    private static void WritePrivateConstructor(StringBuilder file, ScaffoldPlan plan, string indent) {
        int    width      = GeneratorTypeWidth(plan);
        string opening    = $"{indent}private {plan.GeneratorName}(";
        string continuing = new(' ', opening.Length);

        for (int index = 0; index < plan.Parameters.Count; index++) {
            ScaffoldedParameter parameter = plan.Parameters[index];
            bool                last      = index == plan.Parameters.Count - 1;
            string              lead      = index == 0 ? opening : continuing;

            Line(file, $"{lead}{GeneratorTypeOf(parameter).PadRight(width)} {parameter.Identifier}{(last ? ") {" : ",")}");
        }

        int assignment = plan.Parameters.Max(parameter => parameter.FieldName.Length);

        foreach (ScaffoldedParameter parameter in plan.Parameters) {
            Line(file, $"{indent}{Indent}{parameter.FieldName.PadRight(assignment)} = {parameter.Identifier};");
        }

        Line(file, $"{indent}}}");
        Line(file, string.Empty);
    }

    /// <summary>
    ///     Two overloads per parameter. The value one is the ergonomic one; the generator one is what keeps
    ///     composition possible, and is why passing a constrained chain is not the JD011/JD012 mistake of
    ///     generating a value where a recipe was wanted.
    /// </summary>
    private static void WriteWithMethods(StringBuilder file, ScaffoldPlan plan, string indent) {
        foreach (ScaffoldedParameter parameter in plan.Parameters) {
            Line(file, $"{indent}/// <summary>Pins <c>{parameter.Name}</c> to a fixed value.</summary>");
            Line(file, $"{indent}public {plan.GeneratorName} With{parameter.PascalCasedName}({parameter.TypeDisplay} value) {{");
            Line(file, $"{indent}{Indent}return With{parameter.PascalCasedName}(new FixedValue<{parameter.TypeDisplay}>(value));");
            Line(file, $"{indent}}}");
            Line(file, string.Empty);

            string arguments = string.Join(", ", plan.Parameters.Select(other =>
                                                     ReferenceEquals(other, parameter) ? "generator" : other.FieldName));

            Line(file, $"{indent}/// <summary>Draws <c>{parameter.Name}</c> from <paramref name=\"generator\" />.</summary>");
            Line(file, $"{indent}public {plan.GeneratorName} With{parameter.PascalCasedName}({GeneratorTypeOf(parameter)} generator) {{");
            Line(file, $"{indent}{Indent}return new {plan.GeneratorName}({arguments});");
            Line(file, $"{indent}}}");
            Line(file, string.Empty);
        }
    }

    private static void WriteDegenerateConstructor(StringBuilder file, ScaffoldPlan plan, string indent) {
        // "with a default recipe for every constructor parameter" would describe nothing here, and the two
        // constructors of the ordinary shape would collide on one signature (CS0111).
        Line(file, $"{indent}/// <summary>Creates the generator.</summary>");
        Line(file, $"{indent}public {plan.GeneratorName}() {{ }}");
        Line(file, string.Empty);
    }

    private static void WriteGenerate(StringBuilder file, ScaffoldPlan plan, string indent) {
        Line(file, $"{indent}/// <summary>Produces one arbitrary <see cref=\"{plan.Target.Name}\" />.</summary>");
        Line(file, $"{indent}public {plan.Target.Name} Generate() {{");

        string construction = plan.Factory is null ? $"new {plan.Target.Name}(" : $"{plan.Factory}(";
        string opening      = $"{indent}{Indent}return {construction}";

        if (plan.IsDegenerate) {
            Line(file, $"{opening});");
        } else {
            string continuing = new(' ', opening.Length);

            for (int index = 0; index < plan.Parameters.Count; index++) {
                bool   last = index == plan.Parameters.Count - 1;
                string lead = index == 0 ? opening : continuing;

                Line(file, $"{lead}{plan.Parameters[index].FieldName}.Generate(){(last ? ");" : ",")}");
            }
        }

        Line(file, $"{indent}}}");
        Line(file, string.Empty);
    }

    /// <summary>
    ///     Nested and private, so any number of scaffolded files coexist in one project.
    /// </summary>
    /// <remarks>
    ///     It exists because <c>Any.OneOf(value)</c> cannot do this job: it rejects null and it consumes a draw,
    ///     so pinning one parameter with it would shift every value drawn for the others (§14.5). A pinned
    ///     parameter must cost nothing.
    /// </remarks>
    private static void WriteFixedValue(StringBuilder file, string indent) {
        Line(file, $"{indent}private sealed class FixedValue<TValue> : IAny<TValue> {{");
        Line(file, string.Empty);
        Line(file, $"{indent}{Indent}private readonly TValue _value;");
        Line(file, string.Empty);
        Line(file, $"{indent}{Indent}public FixedValue(TValue value) {{");
        Line(file, $"{indent}{Indent}{Indent}_value = value;");
        Line(file, $"{indent}{Indent}}}");
        Line(file, string.Empty);
        Line(file, $"{indent}{Indent}public TValue Generate() {{");
        Line(file, $"{indent}{Indent}{Indent}return _value;");
        Line(file, $"{indent}{Indent}}}");
        Line(file, string.Empty);
        Line(file, $"{indent}}}");
        Line(file, string.Empty);
    }

    private static string GeneratorTypeOf(ScaffoldedParameter parameter) {
        return $"IAny<{parameter.TypeDisplay}>";
    }

    private static int GeneratorTypeWidth(ScaffoldPlan plan) {
        return plan.Parameters.Max(parameter => GeneratorTypeOf(parameter).Length);
    }

    private static bool IsSystem(string @using) {
        return @using == "System" || @using.StartsWith("System.", StringComparison.Ordinal);
    }

    private static string[] Ordered(IEnumerable<string> usings) {
        return usings.Distinct(StringComparer.Ordinal)
                     .OrderBy(@using => @using, StringComparer.Ordinal)
                     .ToArray();
    }

    private static void Line(StringBuilder file, string text) {
        file.Append(text).Append(Newline);
    }

}
