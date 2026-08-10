using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JustDummies.GenAny;

/// <summary>
///     The engine's entry point: a compilation and a type in, a file or a reason out.
/// </summary>
/// <remarks>
///     One entry point, shaped so the IDE consumer of §16 can call it unchanged (§10.3). It performs no IO,
///     writes to no console and never touches MSBuild — those three constraints are what keep the engine
///     loadable inside a Roslyn host (ADR-0065), and they are why this returns a model rather than writing a
///     file.
///     <para>
///         What it does <b>not</b> do yet is read the constructor's guards (§5.3) or compose through a
///         scaffolded generator or a static factory (§5.4). A parameter therefore gets the neutral generator
///         for its type, or a TODO.
///     </para>
/// </remarks>
[SuppressMessage(SonarRule.S1135.Category, SonarRule.S1135.Id,
                 Justification = SuppressionJustification.S1135.DocumentsTheMarkerTheToolEmits)]
public static class Scaffolder {

    /// <summary>
    ///     Scaffolds a generator for <paramref name="target" /> against <paramref name="compilation" />.
    /// </summary>
    /// <param name="compilation">The developer's compilation, which every symbol is resolved against.</param>
    /// <param name="target">The type to scaffold a generator for.</param>
    /// <param name="options">Where the generator lands and how it is named.</param>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static ScaffoldOutcome Scaffold(Compilation compilation, INamedTypeSymbol target, ScaffoldOptions options) {
        if (compilation is null) { throw new ArgumentNullException(nameof(compilation)); }
        if (target is null) { throw new ArgumentNullException(nameof(target)); }
        if (options is null) { throw new ArgumentNullException(nameof(options)); }

        LibrarySurface? library = LibrarySurface.In(compilation);

        if (library is null) { return ScaffoldOutcome.Refused(ScaffoldStatus.LibraryNotReferenced); }

        IMethodSymbol? constructor = ChosenConstructor(target);

        if (constructor is null) { return ScaffoldOutcome.Refused(ScaffoldStatus.NoEligibleConstructor); }

        string?      fileNamespace = options.NamespaceOverride ?? NamespaceOf(target);
        TypeNames    names         = new(fileNamespace);
        GeneratorFor generators    = new(library, names, compilation, options.Naming);
        GuardReading guards        = Guards.Read(constructor, compilation);

        string targetName = names.Of(target);

        List<ScaffoldedParameter> parameters = [];

        foreach (IParameterSymbol parameter in constructor.Parameters) {
            parameters.Add(Resolve(parameter, names, generators, guards));
        }

        ScaffoldPlan plan = new(new TargetType(targetName, fileNamespace, StyleOf(target, fileNamespace)),
                                TypeNaming.GeneratorNameFor(target, options.Naming),
                                names.Usings,
                                parameters);

        return ScaffoldOutcome.Scaffolded(plan, GeneratorEmitter.Emit(plan));
    }

    /// <summary>
    ///     One parameter: the table's answer for its type, tightened by whatever its guards said (§5.3).
    /// </summary>
    private static ScaffoldedParameter Resolve(IParameterSymbol parameter,
                                               TypeNames names,
                                               GeneratorFor generators,
                                               GuardReading guards) {
        string         typeDisplay = names.Of(parameter.Type);
        DrawnGenerator drawn       = generators.Draw(parameter.Type);

        Provenance provenance = drawn.Provenance
                              | (guards.SourceAvailable ? Provenance.None : Provenance.NoSource)
                              | (guards.Unread(parameter.Name) ? Provenance.UnreadGuards : Provenance.None);

        if (!drawn.Resolved) { return ScaffoldedParameter.Unresolved(parameter.Name, typeDisplay, provenance); }

        IReadOnlyList<GuardConstraint> tightening = guards.For(parameter.Name);
        string                         expression = GeneratorFor.Chain(drawn, tightening, out bool dropped);

        if (tightening.Count > 0) { provenance |= Provenance.Guard; }
        if (dropped) { provenance |= Provenance.GuardsNotCombined; }

        return ScaffoldedParameter.DrawnFrom(parameter.Name, typeDisplay, expression, provenance);
    }

    /// <summary>
    ///     The constructor <c>Generate()</c> will call: the public instance one taking the most parameters,
    ///     ties broken by source order (§5.1).
    /// </summary>
    /// <remarks>
    ///     Widest-first because the widest constructor is the one that states the type's whole shape; a narrower
    ///     overload usually defaults something the developer would rather see varied.
    ///     <para>
    ///         A constructor with a <c>ref</c> or <c>out</c> parameter is skipped rather than chosen and
    ///         patched: <c>Generate()</c> passes value arguments, and such a call site does not compile
    ///         (<c>CS1620</c>). <c>in</c> is fine — a value argument binds to it.
    ///     </para>
    ///     A positional record needs no special handling: its primary constructor is an ordinary public one, and
    ///     the copy constructor the compiler adds is protected, so it never competes.
    /// </remarks>
    private static IMethodSymbol? ChosenConstructor(INamedTypeSymbol target) {
        return target.InstanceConstructors
                     .Where(candidate => candidate.DeclaredAccessibility == Accessibility.Public)
                     .Where(candidate => candidate.Parameters.All(parameter => parameter.RefKind is RefKind.None or RefKind.In))
                     .OrderByDescending(candidate => candidate.Parameters.Length)
                     .FirstOrDefault();
    }

    private static string? NamespaceOf(INamedTypeSymbol target) {
        INamespaceSymbol? @namespace = target.ContainingNamespace;

        return @namespace is null || @namespace.IsGlobalNamespace ? null : @namespace.ToDisplayString();
    }

    /// <summary>
    ///     The namespace form the emitted file copies from the target type's own declaration (§4.4).
    /// </summary>
    /// <remarks>
    ///     A type read from metadata has no declaration to copy, and the emitted file falls back to the block
    ///     form — the one that compiles at every language version. Guessing file-scoped would be a guess at the
    ///     developer's <c>LangVersion</c>, and §4.4 exempts the namespace form from the C# 7.3 floor precisely
    ///     because it is copied rather than chosen.
    /// </remarks>
    private static NamespaceStyle StyleOf(INamedTypeSymbol target, string? fileNamespace) {
        if (fileNamespace is null) { return NamespaceStyle.None; }

        foreach (SyntaxReference reference in target.DeclaringSyntaxReferences) {
            for (SyntaxNode? node = reference.GetSyntax(); node is not null; node = node.Parent) {
                if (node is FileScopedNamespaceDeclarationSyntax) { return NamespaceStyle.FileScoped; }
                if (node is NamespaceDeclarationSyntax) { return NamespaceStyle.Block; }
            }
        }

        return NamespaceStyle.Block;
    }

}
