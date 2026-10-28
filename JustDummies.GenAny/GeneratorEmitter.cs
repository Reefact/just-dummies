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
        Line(file, "// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.");
        Line(file, $"// `any generate {plan.Target.Argument} --force` overwrites it. This type is partial, so members you add in a");
        Line(file, "// neighbouring file survive.");
        Line(file, string.Empty);
    }

    /// <summary>
    ///     The <c>System</c> namespaces first, then everything else, each group ordered and separated by a blank
    ///     line — the layout the repository's own files use.
    /// </summary>
    /// <remarks>
    ///     <c>System</c> is opened whenever a parameter has a refining overload: its <c>Func&lt;,&gt;</c> is the
    ///     emitter's own text (ADR-0109), and nothing in the plan's types need have opened the namespace for it.
    /// </remarks>
    private static void WriteUsings(StringBuilder file, ScaffoldPlan plan) {
        IEnumerable<string> usings = plan.Parameters.Any(parameter => !parameter.IsUnresolved) ? [.. plan.Usings, "System"] : plan.Usings;

        string[] system = Ordered(usings.Where(IsSystem));
        string[] rest   = Ordered(usings.Where(@using => !IsSystem(@using)));

        if (system.Length == 0 && rest.Length == 0) { return; }

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
    ///     The one sentence a developer reaching for determinism needs, in the place they will read it. The
    ///     emitted expressions all come from the static façade, so an ambient scope pins every draw, composed
    ///     parameters included, and <c>Any.UseSeed(…)</c> is how to open one outside a test (ADR-0061, ADR-0109).
    /// </summary>
    private static void WriteTypeDocumentation(StringBuilder file, ScaffoldPlan plan, string indent) {
        Line(file, $"{indent}/// <summary>");
        Line(file, $"{indent}///     A generator of arbitrary <see cref=\"{plan.Target.Name}\" /> values. It draws from the ambient random");
        Line(file, $"{indent}///     context, so a reproducibility scope pins every draw it makes; outside a test,");
        Line(file, $"{indent}///     <c>Any.UseSeed(...)</c> opens one.");
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
    ///     Named arguments, aligned, so a reader maps each call to its parameter without counting (§4.2). Each is
    ///     a call to the parameter's factory — never the chain itself, which is written once, in that factory's
    ///     body — followed by the conversion the factory leaves out, if the parameter has one.
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
            string              drawn     = parameter.FactoryMethodName + "()" + parameter.Conversion;
            string              argument  = $"{(parameter.Identifier + ":").PadRight(width)} {drawn}";

            Line(file, lead + argument + (last ? ") { }" : ","));
        }

        Line(file, string.Empty);
    }

    /// <summary>
    ///     One factory per parameter (§4.2): the valid base the public constructor draws from and the refining
    ///     overload starts from, and the place a parameter with something to say for itself says it, right
    ///     beside the chain it is about (ADR-0109).
    /// </summary>
    /// <remarks>
    ///     A resolved factory returns the concrete generator its chain builds, so a refining lambda can call
    ///     that generator's constraints; an unresolved one has no generator to name, and returns the recipe
    ///     interface its sentinel stands in for.
    /// </remarks>
    private static void WriteFactories(StringBuilder file, ScaffoldPlan plan, string indent) {
        string body = indent + Indent;

        foreach (ScaffoldedParameter parameter in plan.Parameters) {
            string returned = parameter.IsUnresolved ? GeneratorTypeOf(parameter) : parameter.GeneratorType!;

            Line(file, $"{indent}private static {returned} {parameter.FactoryMethodName}() {{");

            if (parameter.IsUnresolved) {
                WriteTodo(file, parameter, body);
                Line(file, $"{body}return {parameter.TodoIdentifier};");
            } else if (parameter.RequiresVerification) {
                WriteVerify(file, parameter, body);
                Line(file, $"{body}_ = {parameter.VerifyIdentifier};");
                Line(file, string.Empty);
                Line(file, $"{body}return {parameter.BaseExpression};");
            } else {
                Line(file, $"{body}return {parameter.BaseExpression};");
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
        // Scaffolding the type named would be scaffolding this very file again, so the line says what is
        // actually missing: a bound on how deep the value goes, which only the developer can choose (§5.4).
        if (parameter.Provenance.HasFlag(Provenance.SelfReferencing)) {
            Line(file, $"{indent}// TODO(any): '{parameter.TypeDisplay} {parameter.Name}' reaches the type being scaffolded, so drawing it here would recurse forever.");
            Line(file, $"{indent}//   Write one here with a depth bound you choose (an empty collection or null ends it),");
            Line(file, $"{indent}//   or replace it and always pass .With{parameter.PascalCasedName}(value) instead.");

            return;
        }

        Line(file, $"{indent}// TODO(any): no generator inferred for '{parameter.TypeDisplay} {parameter.Name}'.");

        if (ScaffoldableByName(parameter.TypeDisplay)) {
            // The type is scaffolded, not its annotation: `any generate Bic?` names nothing.
            Line(file, $"{indent}//   Scaffold one:  any generate {parameter.TypeDisplay.TrimEnd('?')}");
            Line(file, $"{indent}//   or write one here, or replace it and always pass .With{parameter.PascalCasedName}(value) instead.");

            return;
        }

        Line(file, $"{indent}//   Write one here, or replace it and always pass .With{parameter.PascalCasedName}(value) instead.");
    }

    /// <summary>
    ///     Whether <c>any generate</c> would accept this type's name (§3.2).
    /// </summary>
    /// <remarks>
    ///     It refuses a generic or constructed type, so naming one on the line above would send the developer
    ///     at a command the tool itself declines — the kind of instruction that is worse than none. The check
    ///     is on the spelling rather than on a symbol because a plan carries the display string alone, and the
    ///     two spellings that matter both show in it: an angle bracket, or a bracket pair.
    ///     <para>
    ///         Cheap before ADR-0089 and worth doing after it: composition now names a generator for every
    ///         plain type, so what is left in this branch is largely the constructed ones.
    ///     </para>
    /// </remarks>
    private static bool ScaffoldableByName(string typeDisplay) {
        return typeDisplay.IndexOf('<') < 0 && typeDisplay.IndexOf('[') < 0;
    }

    /// <summary>
    ///     The identifier below does not exist either, and blocks compilation the same way — but the return
    ///     beneath it is a working recipe, not a placeholder: this parameter's own generator, kept as the base
    ///     to check rather than thrown away over a doubt the engine cannot resolve on its own (ADR-0082).
    /// </summary>
    private static void WriteVerify(StringBuilder file, ScaffoldedParameter parameter, string indent) {
        // Which guard, where: on a parameter with three of them, a mark alone sends the developer to read the
        // whole constructor. The place is the one the developer opens, not a section of a page they never see.
        if (parameter.UnreadGuard is { } unread) {
            Line(file, $"{indent}// TODO(any): '{parameter.TypeDisplay} {parameter.Name}' may be guarded by something the scaffolder could not read,");
            Line(file, $"{indent}//   first at {unread.Place()}: {unread.Excerpt}");
        } else {
            Line(file, $"{indent}// TODO(any): '{parameter.TypeDisplay} {parameter.Name}' may be guarded by something the scaffolder could not read.");
        }

        Line(file, $"{indent}//   This is the scaffolder's best generator for the type; verify it honours the real invariant,");
        Line(file, $"{indent}//   or replace it, then delete the line below. What the scaffolder reads, and what it cannot:");
        Line(file, $"{indent}//   {VerifyGuide}");
    }

    /// <summary>The user page that says what <c>to verify</c> means, which a reader of the file can open.</summary>
    private const string VerifyGuide = "https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means";

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
    ///     Two overloads per parameter, one per intent (ADR-0109). The value one pins the whole value, for a test
    ///     that cares about all of it. The refining one hands the test's lambda the parameter's valid base and
    ///     draws from what the lambda returns, for a test that cares about one part of it — so the test states
    ///     that part and nothing else, and never restates a rule the base already keeps.
    /// </summary>
    /// <remarks>
    ///     No overload takes an <c>IAny&lt;T&gt;</c>: replacing the base is how a test lost a rule without a
    ///     word, compiling and then failing on the draws that broke it. A developer who needs one for a
    ///     particular parameter adds it in a neighbouring part of this partial class, where it is their choice
    ///     rather than the default. An unresolved parameter has no base to refine, and keeps the value overload
    ///     alone.
    /// </remarks>
    private static void WriteWithMethods(StringBuilder file, ScaffoldPlan plan, string indent) {
        foreach (ScaffoldedParameter parameter in plan.Parameters) {
            Line(file, $"{indent}/// <summary>Pins <c>{parameter.Name}</c> to a fixed value.</summary>");
            Line(file, $"{indent}public {plan.GeneratorName} With{parameter.PascalCasedName}({parameter.TypeDisplay} value) {{");
            Line(file, $"{indent}{Indent}return new {plan.GeneratorName}({Arguments(plan, parameter, $"new FixedValue<{parameter.TypeDisplay}>(value)")});");
            Line(file, $"{indent}}}");
            Line(file, string.Empty);

            if (parameter.IsUnresolved) { continue; }

            string refined = $"configure({parameter.FactoryMethodName}()){parameter.Conversion}";

            Line(file, $"{indent}/// <summary>Draws <c>{parameter.Name}</c> from <c>{parameter.FactoryMethodName}()</c>, refined by <paramref name=\"configure\" />.</summary>");
            Line(file, $"{indent}public {plan.GeneratorName} With{parameter.PascalCasedName}(Func<{parameter.GeneratorType}, {parameter.GeneratorType}> configure) {{");
            Line(file, $"{indent}{Indent}return new {plan.GeneratorName}({Arguments(plan, parameter, refined)});");
            Line(file, $"{indent}}}");
            Line(file, string.Empty);
        }
    }

    /// <summary>The private constructor's arguments, with <paramref name="drawn" /> in <paramref name="parameter" />'s place.</summary>
    private static string Arguments(ScaffoldPlan plan, ScaffoldedParameter parameter, string drawn) {
        return string.Join(", ", plan.Parameters.Select(other => ReferenceEquals(other, parameter) ? drawn : other.FieldName));
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
