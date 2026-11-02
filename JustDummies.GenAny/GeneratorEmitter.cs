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
            WriteRecipeConstructor(file, plan, member);
            WriteCopyConstructor(file, plan, member);
            WritePairs(file, plan, member);
            WriteWithMethods(file, plan, member);
        }

        WriteGenerate(file, plan, member);

        if (!plan.IsDegenerate && !plan.PinsThroughAnyFixed) { WriteFixedValue(file, member); }

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

    /// <summary>
    ///     Two fields per resolved parameter (§4.2): what is drawn, and the current recipe it was drawn from —
    ///     the valid base at first, then what each refinement made of it. One per unresolved parameter, which
    ///     has no recipe to keep.
    /// </summary>
    /// <remarks>
    ///     The recipe is kept because a pin does not consume it: a fixed value takes the place of the draw and
    ///     leaves the recipe as it was, so a refinement written after the pin starts from the last recipe. A
    ///     refinement that started again from the base, as the first shape of ADR-0109 did, dropped every
    ///     earlier refinement — a rule an outer base had set on a composed value included (issue #406).
    ///     <para>
    ///         <b>Not <c>readonly</c>, by decision.</b> A <c>With…</c> method copies the generator through the
    ///         private copy constructor and writes one parameter of the copy before handing it out, so the
    ///         instance a caller holds never changes — ADR-0057's immutability, kept by the file's own shape
    ///         rather than by the compiler. The alternative, an all-arguments constructor every <c>With…</c>
    ///         passes every field to, grew with the parameter count twice over once the recipes doubled the
    ///         fields: twelve arguments per method on the worked example of §4.1. The maintainer took the
    ///         readability and accepted what <c>readonly</c> would have refused — a developer's own part
    ///         writing a field of <c>this</c> — on condition that the convention is written down (§4.2).
    ///     </para>
    /// </remarks>
    private static void WriteFields(StringBuilder file, ScaffoldPlan plan, string indent) {
        int width = GeneratorTypeWidth(plan);

        foreach (ScaffoldedParameter parameter in plan.Parameters) {
            if (plan.PairingOf(parameter) is not { } pairing) {
                Line(file, $"{indent}private {GeneratorTypeOf(parameter).PadRight(width)} {parameter.FieldName};");
            } else if (OpensPair(plan, parameter, pairing)) {
                Line(file, $"{indent}private {PairTypeOf(plan, pairing).PadRight(width)} {PairFieldName(plan, pairing)};");
            }
        }

        Line(file, string.Empty);

        List<ScaffoldedParameter> resolved = Resolved(plan);

        if (resolved.Count == 0) { return; }

        int recipeWidth = resolved.Max(parameter => parameter.GeneratorType!.Length);

        foreach (ScaffoldedParameter parameter in resolved) {
            Line(file, $"{indent}private {parameter.GeneratorType!.PadRight(recipeWidth)} {parameter.RecipeFieldName};");
        }

        Line(file, string.Empty);

        List<ScaffoldedParameter> paired = Paired(plan);

        if (paired.Count == 0) { return; }

        int pinWidth = paired.Max(parameter => PinTypeOf(parameter).Length);

        foreach (ScaffoldedParameter parameter in paired) {
            Line(file, $"{indent}private {PinTypeOf(parameter).PadRight(pinWidth)} {PinFieldName(parameter)};");
        }

        Line(file, string.Empty);
    }

    /// <summary>
    ///     Named arguments, aligned, so a reader maps each call to its parameter without counting (§4.2). Each is
    ///     a call to the parameter's factory — never the chain itself, which is written once, in that factory's
    ///     body — and never its conversion either: the recipe constructor this chains into applies that, so the
    ///     recipe it keeps is the generator the constraints were written on.
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
            string              argument  = $"{(parameter.Identifier + ":").PadRight(width)} {parameter.FactoryMethodName}()";

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

        // `any generate` refuses an abstract type, so the line names the generator the developer writes instead —
        // the one a composing scaffold would have named — rather than a command that sends them nowhere (§5.5).
        if (parameter.Unscaffoldable is { } abstractType) {
            Line(file, $"{indent}// TODO(any): '{parameter.TypeDisplay} {parameter.Name}' reaches '{abstractType.Type}', which is abstract, so `any generate` cannot scaffold it.");
            Line(file, $"{indent}//   Write {abstractType.Generator} : IAny<{abstractType.Type}> by hand, choosing among the concrete types,");
            Line(file, $"{indent}//   or write one here, or replace it and always pass .With{parameter.PascalCasedName}(value) instead.");

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

    /// <summary>
    ///     The constructor the public one chains into: one recipe per parameter, each drawn from as it is —
    ///     through its conversion where it has one — and kept as the current recipe.
    /// </summary>
    private static void WriteRecipeConstructor(StringBuilder file, ScaffoldPlan plan, string indent) {
        int    width      = plan.Parameters.Max(parameter => RecipeTypeOf(parameter).Length);
        string opening    = $"{indent}private {plan.GeneratorName}(";
        string continuing = new(' ', opening.Length);

        Line(file, $"{indent}/// <summary>Draws every parameter from the recipe given for it.</summary>");

        for (int index = 0; index < plan.Parameters.Count; index++) {
            ScaffoldedParameter parameter = plan.Parameters[index];
            bool                last      = index == plan.Parameters.Count - 1;
            string              lead      = index == 0 ? opening : continuing;

            Line(file, $"{lead}{RecipeTypeOf(parameter).PadRight(width)} {parameter.Identifier}{(last ? ") {" : ",")}");
        }

        int assignment = AssignmentWidth(plan);

        foreach (ScaffoldedParameter parameter in plan.Parameters) {
            if (plan.PairingOf(parameter) is null) {
                Line(file, $"{indent}{Indent}{parameter.FieldName.PadRight(assignment)} = {parameter.Identifier}{parameter.DefaultConversion};");
            }

            if (!parameter.IsUnresolved) {
                Line(file, $"{indent}{Indent}{parameter.RecipeFieldName.PadRight(assignment)} = {parameter.Identifier};");
            }
        }

        // Last, once both recipes are in place: the pair is drawn from them.
        foreach (OrderedPairing pairing in plan.Pairings) {
            Line(file, $"{indent}{Indent}{PairFieldName(plan, pairing).PadRight(assignment)} = {PairMethodName(plan, pairing)}();");
        }

        Line(file, $"{indent}}}");
        Line(file, string.Empty);
    }

    /// <summary>
    ///     The copy constructor every <c>With…</c> method goes through: the whole state, field for field, for
    ///     the method to change one parameter of before the copy is handed out.
    /// </summary>
    /// <remarks>
    ///     One place that knows every field, instead of one argument list per <c>With…</c> that repeats them
    ///     all: a parameter added to the target adds a line here and nowhere else, and a <c>With…</c> written
    ///     in the developer's own part is the same three statements whatever the parameter count.
    /// </remarks>
    private static void WriteCopyConstructor(StringBuilder file, ScaffoldPlan plan, string indent) {
        int assignment = AssignmentWidth(plan);

        Line(file, $"{indent}/// <summary>Copies <paramref name=\"from\" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>");
        Line(file, $"{indent}private {plan.GeneratorName}({plan.GeneratorName} from) {{");

        foreach (ScaffoldedParameter parameter in plan.Parameters) {
            OrderedPairing? pairing = plan.PairingOf(parameter);

            if (pairing is null) {
                Line(file, $"{indent}{Indent}{parameter.FieldName.PadRight(assignment)} = from.{parameter.FieldName};");
            } else if (OpensPair(plan, parameter, pairing)) {
                Line(file, $"{indent}{Indent}{PairFieldName(plan, pairing).PadRight(assignment)} = from.{PairFieldName(plan, pairing)};");
            }

            if (!parameter.IsUnresolved) {
                Line(file, $"{indent}{Indent}{parameter.RecipeFieldName.PadRight(assignment)} = from.{parameter.RecipeFieldName};");
            }

            if (pairing is not null) {
                Line(file, $"{indent}{Indent}{PinFieldName(parameter).PadRight(assignment)} = from.{PinFieldName(parameter)};");
            }
        }

        Line(file, $"{indent}}}");
        Line(file, string.Empty);
    }

    /// <summary>The width the assignments of both constructors align on: the longest field name, recipes included.</summary>
    private static int AssignmentWidth(ScaffoldPlan plan) {
        int parameters = plan.Parameters.Max(parameter => parameter.IsUnresolved ? parameter.FieldName.Length : parameter.RecipeFieldName.Length);

        return plan.Pairings.Select(pairing => PairFieldName(plan, pairing).Length).Aggregate(parameters, Math.Max);
    }

    /// <summary>
    ///     One private method per pair (§4.2): the two parameters drawn together from their current recipes,
    ///     through <c>Any.OrderedPair</c>, so the first never follows the second, with any pin either carries. Every
    ///     <c>With…</c> of either parameter rebuilds the pair through it, so a pin on one side survives a
    ///     refinement of the other, and two pins the guard refuses are refused at the second call (#405).
    /// </summary>
    private static void WritePairs(StringBuilder file, ScaffoldPlan plan, string indent) {
        string body = indent + Indent;

        foreach (OrderedPairing pairing in plan.Pairings) {
            ScaffoldedParameter first  = Named(plan, pairing.First);
            ScaffoldedParameter second = Named(plan, pairing.Second);
            string              type   = PairTypeOf(plan, pairing);
            string              order  = pairing.Strict ? "always before" : "never after";
            string              strict = pairing.Strict ? ", strict: true" : string.Empty;

            Line(file, $"{indent}/// <summary>Draws <c>{first.Name}</c> and <c>{second.Name}</c> together, <c>{first.Name}</c> {order} <c>{second.Name}</c> as the constructor requires, each from its current recipe and pin.</summary>");
            Line(file, $"{indent}private {type} {PairMethodName(plan, pairing)}() {{");
            Line(file, $"{body}{type} pair = Any.OrderedPair({first.RecipeFieldName}, {second.RecipeFieldName}{strict});");
            Line(file, string.Empty);
            Line(file, $"{body}if ({PinFieldName(first)} is {{ }} {first.Identifier}) {{ pair = pair.WithFirst({first.Identifier}); }}");
            Line(file, $"{body}if ({PinFieldName(second)} is {{ }} {second.Identifier}) {{ pair = pair.WithSecond({second.Identifier}); }}");
            Line(file, string.Empty);
            Line(file, $"{body}return pair;");
            Line(file, $"{indent}}}");
            Line(file, string.Empty);
        }
    }

    /// <summary>
    ///     Two overloads per parameter, one per intent (ADR-0109). The value one pins the whole value, for a test
    ///     that cares about all of it, and keeps the recipe. The refining one hands the test's lambda the
    ///     parameter's <b>current</b> recipe — the valid base at first, then what each earlier refinement made of
    ///     it — and draws from what the lambda returns, for a test that cares about one part of it: so the test
    ///     states that part and nothing else, never restates a rule the base already keeps, and refinements
    ///     accumulate as the library's constraints do.
    /// </summary>
    /// <remarks>
    ///     No overload takes an <c>IAny&lt;T&gt;</c>: replacing the base is how a test lost a rule without a
    ///     word, compiling and then failing on the draws that broke it. A developer who needs one for a
    ///     particular parameter adds it in a neighbouring part of this partial class, where it is their choice
    ///     rather than the default. An unresolved parameter has no base to refine, and keeps the value overload
    ///     alone.
    ///     <para>
    ///         A parameter whose default draws <c>null</c> one draw in ten gets a third method, <c>Without…()</c>,
    ///         which pins it to <c>null</c>: the refining lambda describes a value and never yields one, and
    ///         <c>With…(null)</c> is ambiguous between the two overloads (#411).
    ///     </para>
    /// </remarks>
    private static void WriteWithMethods(StringBuilder file, ScaffoldPlan plan, string indent) {
        string body = indent + Indent;

        foreach (ScaffoldedParameter parameter in plan.Parameters) {
            if (plan.PairingOf(parameter) is { } pairing) {
                WritePairedWithMethods(file, plan, parameter, pairing, indent);

                continue;
            }

            Line(file, parameter.IsUnresolved
                           ? $"{indent}/// <summary>Pins <c>{parameter.Name}</c> to <paramref name=\"value\" />, which every draw hands over as it is — one shared instance for a reference type.</summary>"
                           : $"{indent}/// <summary>Pins <c>{parameter.Name}</c> to <paramref name=\"value\" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>");

            Line(file, $"{indent}public {plan.GeneratorName} With{parameter.PascalCasedName}({parameter.TypeDisplay} value) {{");
            WriteCopy(file, plan, body);
            Line(file, plan.PinsThroughAnyFixed
                           ? $"{body}copy.{parameter.FieldName} = Any.Fixed(value);"
                           : $"{body}copy.{parameter.FieldName} = new FixedValue<{parameter.TypeDisplay}>(value);");
            WriteReturnCopy(file, body);
            Line(file, $"{indent}}}");
            Line(file, string.Empty);

            if (parameter.IsUnresolved) { continue; }

            int assignment = parameter.RecipeFieldName.Length;

            Line(file, $"{indent}/// <summary>Refines the recipe <c>{parameter.Name}</c> is drawn from — <c>{parameter.FactoryMethodName}()</c>, then each earlier refinement — through <paramref name=\"configure\" />.</summary>");
            WriteReuseRemark(file, parameter, indent);
            Line(file, $"{indent}public {plan.GeneratorName} With{parameter.PascalCasedName}(Func<{parameter.GeneratorType}, {parameter.GeneratorType}> configure) {{");
            WriteCopy(file, plan, body);
            Line(file, $"{body}copy.{parameter.RecipeFieldName} = configure({parameter.RecipeFieldName});");
            Line(file, $"{body}copy.{parameter.FieldName.PadRight(assignment)} = copy.{parameter.RecipeFieldName}{parameter.Conversion};");
            WriteReturnCopy(file, body);
            Line(file, $"{indent}}}");
            Line(file, string.Empty);

            if (parameter.DrawsNull) { WriteWithout(file, plan, parameter, indent); }
        }
    }

    /// <summary>
    ///     The two <c>With…</c> overloads of a paired parameter, which keep their meaning: the pin is kept beside the
    ///     recipe, the refinement takes the pin's place, and either rebuilds the pair, so the other side is drawn
    ///     in order with what this one now is.
    /// </summary>
    private static void WritePairedWithMethods(StringBuilder file, ScaffoldPlan plan, ScaffoldedParameter parameter, OrderedPairing pairing, string indent) {
        string body   = indent + Indent;
        string pair   = PairFieldName(plan, pairing);
        string pin    = PinFieldName(parameter);
        string other  = Named(plan, pairing.First == parameter.Name ? pairing.Second : pairing.First).Name;
        string redraw = $"copy.{PairMethodName(plan, pairing)}()";
        int    width  = Math.Max(parameter.RecipeFieldName.Length, Math.Max(pin.Length, pair.Length));

        Line(file, $"{indent}/// <summary>Pins <c>{parameter.Name}</c> to a fixed value, which bounds <c>{other}</c>; its recipe is kept for a later refinement.</summary>");
        Line(file, $"{indent}public {plan.GeneratorName} With{parameter.PascalCasedName}({parameter.TypeDisplay} value) {{");
        WriteCopy(file, plan, body);
        Line(file, $"{body}copy.{pin.PadRight(width)} = value;");
        Line(file, $"{body}copy.{pair.PadRight(width)} = {redraw};");
        WriteReturnCopy(file, body);
        Line(file, $"{indent}}}");
        Line(file, string.Empty);

        Line(file, $"{indent}/// <summary>Refines the recipe <c>{parameter.Name}</c> is drawn from — <c>{parameter.FactoryMethodName}()</c>, then each earlier refinement — through <paramref name=\"configure\" />, in order with <c>{other}</c>.</summary>");
        WriteReuseRemark(file, parameter, indent);
        Line(file, $"{indent}public {plan.GeneratorName} With{parameter.PascalCasedName}(Func<{parameter.GeneratorType}, {parameter.GeneratorType}> configure) {{");
        WriteCopy(file, plan, body);
        Line(file, $"{body}copy.{parameter.RecipeFieldName.PadRight(width)} = configure({parameter.RecipeFieldName});");
        Line(file, $"{body}copy.{pin.PadRight(width)} = null;");
        Line(file, $"{body}copy.{pair.PadRight(width)} = {redraw};");
        WriteReturnCopy(file, body);
        Line(file, $"{indent}}}");
        Line(file, string.Empty);
    }

    /// <summary>
    ///     How to reuse a refinement, said where a developer meets the overload: a generator prepared elsewhere is
    ///     not accepted, and the lambda that would smuggle it in — <c>_ =&gt; prepared</c> — compiles and replaces
    ///     the base it was meant to keep (ADR-0109, #391).
    /// </summary>
    private static void WriteReuseRemark(StringBuilder file, ScaffoldedParameter parameter, string indent) {
        string generator = Escaped(parameter.GeneratorType!);

        Line(file, $"{indent}/// <remarks>Reuse a refinement as a <c>Func&lt;{generator}, {generator}&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>");
    }

    /// <summary>A type name as an XML documentation comment spells it.</summary>
    private static string Escaped(string type) {
        return type.Replace("<", "&lt;").Replace(">", "&gt;");
    }

    /// <summary>
    ///     <c>Without…()</c>: the parameter pinned to <c>null</c>, which its type declares valid, for the test that
    ///     is about its absence. The recipe is kept, as a pin keeps it.
    /// </summary>
    private static void WriteWithout(StringBuilder file, ScaffoldPlan plan, ScaffoldedParameter parameter, string indent) {
        string body = indent + Indent;

        Line(file, $"{indent}/// <summary>Pins <c>{parameter.Name}</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>");
        Line(file, $"{indent}public {plan.GeneratorName} Without{parameter.PascalCasedName}() {{");
        WriteCopy(file, plan, body);
        Line(file, plan.PinsThroughAnyFixed
                       ? $"{body}copy.{parameter.FieldName} = Any.Fixed<{parameter.TypeDisplay}>(null);"
                       : $"{body}copy.{parameter.FieldName} = new FixedValue<{parameter.TypeDisplay}>(null);");
        WriteReturnCopy(file, body);
        Line(file, $"{indent}}}");
        Line(file, string.Empty);
    }

    /// <summary>The first statement of every <c>With…</c>: a fresh copy, which is the only instance the method writes to.</summary>
    private static void WriteCopy(StringBuilder file, ScaffoldPlan plan, string indent) {
        Line(file, $"{indent}{plan.GeneratorName} copy = new {plan.GeneratorName}(this);");
        Line(file, string.Empty);
    }

    private static void WriteReturnCopy(StringBuilder file, string indent) {
        Line(file, string.Empty);
        Line(file, $"{indent}return copy;");
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
            WritePairDraws(file, plan, indent + Indent);

            string continuing = new(' ', opening.Length);

            for (int index = 0; index < plan.Parameters.Count; index++) {
                ScaffoldedParameter parameter = plan.Parameters[index];
                bool                last      = index == plan.Parameters.Count - 1;
                string              lead      = index == 0 ? opening : continuing;
                string              argument  = plan.PairingOf(parameter) is null ? $"{parameter.FieldName}.Generate()" : parameter.Identifier;

                Line(file, $"{lead}{argument}{(last ? ");" : ",")}");
            }
        }

        Line(file, $"{indent}}}");
        Line(file, string.Empty);
    }

    /// <summary>A pair is drawn as one, ahead of the construction, and each side handed over by name.</summary>
    private static void WritePairDraws(StringBuilder file, ScaffoldPlan plan, string indent) {
        foreach (OrderedPairing pairing in plan.Pairings) {
            ScaffoldedParameter first  = Named(plan, pairing.First);
            ScaffoldedParameter second = Named(plan, pairing.Second);

            Line(file, $"{indent}({first.TypeDisplay} {first.Identifier}, {second.TypeDisplay} {second.Identifier}) = {PairFieldName(plan, pairing)}.Generate();");
        }

        if (plan.Pairings.Count > 0) { Line(file, string.Empty); }
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

    /// <summary>What the recipe constructor takes for a parameter: its recipe's type, or the draw's where it has no recipe.</summary>
    private static string RecipeTypeOf(ScaffoldedParameter parameter) {
        return parameter.GeneratorType ?? GeneratorTypeOf(parameter);
    }

    /// <summary>The parameters that carry a recipe — every one the engine inferred a generator for.</summary>
    private static List<ScaffoldedParameter> Resolved(ScaffoldPlan plan) {
        return [.. plan.Parameters.Where(parameter => !parameter.IsUnresolved)];
    }

    private static int GeneratorTypeWidth(ScaffoldPlan plan) {
        int parameters = plan.Parameters.Where(parameter => plan.PairingOf(parameter) is null)
                             .Select(parameter => GeneratorTypeOf(parameter).Length)
                             .DefaultIfEmpty(0)
                             .Max();

        return plan.Pairings.Select(pairing => PairTypeOf(plan, pairing).Length).Aggregate(parameters, Math.Max);
    }

    /// <summary>The paired parameters, in declaration order.</summary>
    private static List<ScaffoldedParameter> Paired(ScaffoldPlan plan) {
        return [.. plan.Parameters.Where(parameter => plan.PairingOf(parameter) is not null)];
    }

    private static ScaffoldedParameter Named(ScaffoldPlan plan, string name) {
        return plan.Parameters.First(parameter => parameter.Name == name);
    }

    /// <summary>Whether <paramref name="parameter" /> is the one of its pair declared first, where the pair's field sits.</summary>
    private static bool OpensPair(ScaffoldPlan plan, ScaffoldedParameter parameter, OrderedPairing pairing) {
        return plan.Parameters.First(candidate => pairing.Ties(candidate.Name)) == parameter;
    }

    /// <summary>The pair's own type — <c>AnyOrderedPair&lt;DateTime&gt;</c>.</summary>
    private static string PairTypeOf(ScaffoldPlan plan, OrderedPairing pairing) {
        return $"AnyOrderedPair<{Named(plan, pairing.First).TypeDisplay}>";
    }

    /// <summary>The field the pair is drawn from — <c>_startAndEnd</c>.</summary>
    private static string PairFieldName(ScaffoldPlan plan, OrderedPairing pairing) {
        return Named(plan, pairing.First).FieldName + "And" + Named(plan, pairing.Second).PascalCasedName;
    }

    /// <summary>The private method that rebuilds the pair — <c>StartAndEnd()</c>.</summary>
    private static string PairMethodName(ScaffoldPlan plan, OrderedPairing pairing) {
        return Named(plan, pairing.First).PascalCasedName + "And" + Named(plan, pairing.Second).PascalCasedName;
    }

    /// <summary>The field a paired parameter's pin is kept in, beside its recipe — <c>_startPin</c>.</summary>
    private static string PinFieldName(ScaffoldedParameter parameter) {
        return parameter.FieldName + "Pin";
    }

    private static string PinTypeOf(ScaffoldedParameter parameter) {
        return parameter.TypeDisplay + "?";
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
