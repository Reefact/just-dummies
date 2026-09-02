using System;
using System.Collections.Generic;
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
///         What it assembles, in order: the constructor §5.1 chooses — or the static factory §5.1.2
///         recognises, where the type declares none to call — the base table's generator for each parameter
///         (§5.2), composing through the generator a type owns where the table has no row (§5.4), and the
///         constraints §5.3 reads from that constructor's guards. The provenance
///         each parameter carries is computed from the constraints actually <b>applied</b>, never from those
///         read, which is what §6's recap reports and why a guard the generator has no member for tightens
///         nothing however well it was understood.
///     </para>
/// </remarks>
public static class Scaffolder {

    /// <summary>
    ///     Scaffolds a generator for the type <paramref name="typeArgument" /> names (§3.2).
    /// </summary>
    /// <remarks>
    ///     The overload §11.1 describes: the shell hands over the compilation and the name the developer typed,
    ///     and gets back a file or a reason — including "that name matched nothing" and "it matched several",
    ///     as data rather than as an exception. The symbol overload below stays for the caller that has already
    ///     resolved one, which is how §10.3 words the same boundary.
    /// </remarks>
    /// <param name="compilation">The developer's compilation, which every symbol is resolved against.</param>
    /// <param name="typeArgument">The type, spelled as a developer would type it: <c>Order</c>,
    ///     <c>Shop.Domain.Order</c>, or <c>Order.Line</c> for a nested one.</param>
    /// <param name="options">Where the generator lands and how it is named.</param>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="typeArgument" /> is blank.</exception>
    public static ScaffoldOutcome Scaffold(Compilation compilation, string typeArgument, ScaffoldOptions options) {
        if (compilation is null) { throw new ArgumentNullException(nameof(compilation)); }
        if (typeArgument is null) { throw new ArgumentNullException(nameof(typeArgument)); }
        if (options is null) { throw new ArgumentNullException(nameof(options)); }
        if (typeArgument.Trim().Length == 0) {
            throw new ArgumentException("A scaffold names a type.", nameof(typeArgument));
        }

        IReadOnlyList<INamedTypeSymbol> found = TypeLookup.Find(compilation, typeArgument.Trim());

        if (found.Count == 0) {
            return ScaffoldOutcome.Refused(ScaffoldStatus.TypeNotFound,
                                           TypeLookup.Closest(compilation, typeArgument.Trim()));
        }

        if (found.Count > 1) {
            return ScaffoldOutcome.Refused(ScaffoldStatus.TypeAmbiguous,
                                           [.. found.Select(type => type.ToDisplayString()).OrderBy(name => name, StringComparer.Ordinal)]);
        }

        return Scaffold(compilation, found[0], options);
    }

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

        IMethodSymbol?               constructor = ChosenConstructor(target);
        IReadOnlyList<IMethodSymbol> factories   = EligibleFactories(target);
        IMethodSymbol?               factory     = factories.Count == 1 ? factories[0] : null;

        // Choosing a construction is not the same question as whether the emitted file can name the type and
        // make that call. Each of these finds a public construction and then fails the developer's own build,
        // so the refusal has to come first — a file nobody can compile, written under a recap that says every
        // parameter was inferred, is the one outcome §7 has no row for.
        //
        // And it comes before `NoEligibleConstructor` too, which is a fact about §5.1.1's search rather than
        // about the type: an abstract class declares its constructors `protected`, so the search finds none
        // and the coarser refusal used to win — sending the developer to add a public constructor, which
        // changes nothing about instantiating an abstract type. Measured over seven repositories, 12 of 55
        // such refusals were really one of these two (`audit/2026-09-02-dum-first-field-measurement.md`).
        if (IsGeneric(target)) { return ScaffoldOutcome.Refused(ScaffoldStatus.TypeIsGeneric); }

        // Only the generic refusal reaches the factory path: `CS0144` and `CS9035` are both about `new`,
        // which a factory call site never writes, so an abstract type behind a recognised factory is
        // scaffolded through it — the very design §5.1.2 exists to serve.
        // Before abstractness, because a tie is the only one of the two the developer can act on: bring
        // the qualifying set down to exactly one — from two or from five — and the same abstract type
        // scaffolds through it, so `TypeIsAbstract` here would send them to write a derived type they do
        // not need. Unreachable while a public constructor
        // exists — the route is shut then and this set is empty — so it cannot divert a type that scaffolds.
        if (factories.Count > 1) {
            return ScaffoldOutcome.Refused(ScaffoldStatus.NoEligibleConstructor,
                                           [.. factories.Select(candidate => candidate.ToDisplayString())
                                                        .OrderBy(name => name, StringComparer.Ordinal)]);
        }

        if (factory is null && target.IsAbstract) { return ScaffoldOutcome.Refused(ScaffoldStatus.TypeIsAbstract); }

        // No candidates: what is left here is a type with nothing to call and no factory the engine would
        // ever reach for. Naming one it holds shut — the public `ref` constructor of §5.1.5 keeps the route
        // closed however many factories sit beside it — would offer a remedy that changes nothing.
        if (constructor is null && factory is null) { return ScaffoldOutcome.Refused(ScaffoldStatus.NoEligibleConstructor); }

        if (constructor is not null && LeavesRequiredMembersUnset(target, constructor)) {
            return ScaffoldOutcome.Refused(ScaffoldStatus.RequiredMembersUnset);
        }

        IMethodSymbol chosen = constructor ?? factory!;

        string?      fileNamespace = options.NamespaceOverride ?? NamespaceOf(target);
        TypeNames    names         = new(fileNamespace);
        GeneratorFor generators    = new(library, names, compilation, options.Naming);
        GuardReading guards        = Guards.Read(chosen, compilation, names);

        string targetName = names.Of(target);

        List<ScaffoldedParameter> parameters = [];

        foreach (IParameterSymbol parameter in chosen.Parameters) {
            parameters.Add(Resolve(parameter, names, generators, guards));
        }

        ScaffoldPlan plan = new(new TargetType(targetName, fileNamespace, StyleOf(target, fileNamespace)),
                                TypeNaming.GeneratorNameFor(target, options.Naming),
                                names.Usings,
                                parameters,
                                factory is null ? null : targetName + "." + factory.Name);

        return ScaffoldOutcome.Scaffolded(plan,
                                          GeneratorEmitter.Emit(plan),
                                          Shadowing(plan, library),
                                          EntryPointEmitter.Emit(plan, options.EntryPoint));
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

        if (!drawn.Resolved) {
            return ScaffoldedParameter.Unresolved(parameter.Name, typeDisplay, provenance, drawn.AmbiguousGenerators);
        }

        IReadOnlyList<GuardConstraint> tightening = guards.For(parameter.Name);
        ChainReport                    written    = GeneratorFor.Chain(drawn, tightening);

        // Computed from the constraints APPLIED, not the constraints read. §6 words this column `tightened`,
        // and a guard the generator carries no member for tightened nothing however well it was understood.
        if (written.GuardApplied) { provenance |= Provenance.Guard; }
        if (written.GuardsNotCombined) { provenance |= Provenance.GuardsNotCombined; }
        if (written.ConstraintUnavailable) { provenance |= Provenance.ConstraintUnavailable; }

        return ScaffoldedParameter.DrawnFrom(parameter.Name, typeDisplay, written.Expression, provenance);
    }

    /// <summary>
    ///     Whether the emitted file would have a type argument it cannot supply.
    /// </summary>
    /// <remarks>
    ///     The containing types count: <c>Outer&lt;T&gt;.Inner</c> is not itself generic and still cannot be
    ///     named without <c>T</c>.
    /// </remarks>
    private static bool IsGeneric(INamedTypeSymbol target) {
        for (INamedTypeSymbol? type = target; type is not null; type = type.ContainingType) {
            if (type.IsGenericType) { return true; }
        }

        return false;
    }

    /// <summary>
    ///     Whether <c>new</c> through <paramref name="constructor" /> would leave a required member unset.
    /// </summary>
    /// <remarks>
    ///     Inherited ones count as much as declared ones — the compiler asks for every required member in the
    ///     hierarchy — and <c>[SetsRequiredMembers]</c> answers for all of them at once, which is why it is
    ///     read first rather than per member.
    /// </remarks>
    private static bool LeavesRequiredMembersUnset(INamedTypeSymbol target, IMethodSymbol constructor) {
        if (SetsRequiredMembers(constructor)) { return false; }

        for (INamedTypeSymbol? type = target; type is not null; type = type.BaseType) {
            if (type.GetMembers().Any(IsRequired)) { return true; }
        }

        return false;
    }

    private static bool IsRequired(ISymbol member) {
        return member is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true };
    }

    private static bool SetsRequiredMembers(IMethodSymbol constructor) {
        return constructor.GetAttributes()
                          .Any(attribute => attribute.AttributeClass?.ToDisplayString()
                                         == "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute");
    }

    /// <summary>
    ///     The factories §5.1.2 would choose between, where the type has no accessible constructor at all.
    /// </summary>
    /// <remarks>
    ///     The canonical validating value object — a private constructor behind a public <c>Create</c> — is
    ///     this rule's whole audience, and §5.1.2 already says what qualifies: public static, returning the
    ///     type, one parameter, one of the four recognised names, <c>Create</c> winning ties.
    ///     <para>
    ///         Only where <b>no</b> public instance constructor exists, because §5.1 words it that way on
    ///         purpose: a type whose public constructors are all ineligible — a <c>ref</c> parameter, say —
    ///         ends unresolved rather than routed around its own declared surface.
    ///     </para>
    ///     <para>
    ///         The <b>set</b> rather than the pick, because the caller has two questions to ask of it and one
    ///         answer cannot serve both: which factory to call, and — when several tie — which names the
    ///         refusal has to print. A single nullable answer said "closed" and "ambiguous" with the same
    ///         <c>null</c>, and a caller reading it as the first offered a way out of the second that does
    ///         not exist.
    ///     </para>
    /// </remarks>
    private static IReadOnlyList<IMethodSymbol> EligibleFactories(INamedTypeSymbol target) {
        // A struct's synthesized public parameterless constructor is not "its own declared surface" this
        // remark means — the developer wrote none, the compiler always adds one — so it does not gate the
        // factory the way an ineligible constructor the developer actually wrote does.
        if (target.InstanceConstructors.Any(candidate => candidate.DeclaredAccessibility == Accessibility.Public
                                                       && !(target.TypeKind == TypeKind.Struct && candidate.IsImplicitlyDeclared))) {
            return [];
        }

        return Composition.FactoriesFor(target);
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
                     // A struct always carries this constructor, synthesized rather than written — accepting it
                     // here routes straight past a private constructor and its factory, zero-initializing every
                     // field under a recap that says nothing was left unread. A class's own implicit default
                     // constructor is unaffected: it is the only one there is, not a bypass of one the developer
                     // wrote.
                     .Where(candidate => !(target.TypeKind == TypeKind.Struct && candidate.IsImplicitlyDeclared))
                     .OrderByDescending(candidate => candidate.Parameters.Length)
                     .FirstOrDefault();
    }

    /// <summary>
    ///     Whether the scaffolded name is one the library already uses (§7).
    /// </summary>
    /// <remarks>
    ///     Read off the library the developer references rather than from a list here, for the same reason
    ///     every other member is (ADR-0063), and compared on <b>arity</b> as well as name: a generic
    ///     <c>AnySet&lt;T&gt;</c> cannot be shadowed by a scaffolded <c>AnySet</c>, since arity is part of a
    ///     type's identity in C#. Warning on the generic ones would cry wolf on a domain type named
    ///     <c>Set</c>, <c>List</c> or <c>Sequence</c>.
    /// </remarks>
    private static IReadOnlyList<ScaffoldWarning> Shadowing(ScaffoldPlan plan, LibrarySurface library) {
        INamedTypeSymbol? shadowed = library.Any
                                            .ContainingNamespace
                                            .GetTypeMembers()
                                            .FirstOrDefault(type => type.Arity == 0
                                                                 && type.DeclaredAccessibility == Accessibility.Public
                                                                 && type.Name == plan.GeneratorName);

        return shadowed is null ? [] : [ScaffoldWarning.Shadows(plan.GeneratorName, shadowed.ToDisplayString())];
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
