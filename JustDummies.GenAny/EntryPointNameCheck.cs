using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace JustDummies.GenAny;

/// <summary>
///     The pre-write name check of §4.5: what an entry-point file would declare, against the names already
///     borne where it would declare them.
/// </summary>
/// <remarks>
///     One check rather than one per site, because its questions are one question asked at different moments,
///     and a clash found at one of them has to read like a clash found at another. It asks three: whether
///     the type the entry point declares is the generator of another type of the same run
///     (<see cref="AgainstTheBatch" />), whether the project already declares a type by that name
///     (<see cref="AgainstTheCompilation" />), and whether the entry-point namespace ends in the root class's
///     own name, which C# resolves before the class (<see cref="RootAgainstNamespace" />, a warning).
///     <para>
///         Only <see cref="EntryPointKind.Any" /> can clash this way: it declares <c>Any{Type}Entry</c>, which is
///         exactly the generator name of a domain type <c>{Type}Entry</c>. A static root is <c>partial</c>, and a
///         declaration already bearing its name is the part an earlier scaffold wrote (§4.5) — the one case
///         where a name already borne is the design rather than a defect.
///     </para>
///     <para>
///         The answer is a refusal, never a rename (ADR-0046). A cleverer name would move the clash to the next
///         domain type that happens to spell it, and a developer who met <c>AnyVoiceEntry2</c> would not know
///         why. Naming both declarations, each with its file, is what they can act on: move the entry points with
///         <c>--entry-point-namespace</c>, ask for a static root, or rename a type.
///     </para>
/// </remarks>
public static class EntryPointNameCheck {

    /// <summary>
    ///     Across one run: a scaffold whose entry point would declare the generator name of another scaffold of
    ///     the same run is refused, before any of them is written.
    /// </summary>
    /// <param name="batch">The outcomes of one run, in the order they were asked for.</param>
    /// <returns>
    ///     The same outcomes, in the same order, each clashing one replaced by a refusal carrying both
    ///     declarations. Refused outcomes, and scaffolds without an entry point, pass through untouched.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="batch" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="batch" /> carries a null element.</exception>
    public static IReadOnlyList<ScaffoldOutcome> AgainstTheBatch(IReadOnlyList<ScaffoldOutcome> batch) {
        if (batch is null) { throw new ArgumentNullException(nameof(batch)); }
        if (batch.Any(outcome => outcome is null)) {
            throw new ArgumentException("An outcome is never absent from a run.", nameof(batch));
        }

        List<ScaffoldOutcome> checkedBatch = [];

        foreach (ScaffoldOutcome scaffold in batch) {
            ScaffoldOutcome? bearer = CanClash(scaffold)
                                          ? batch.FirstOrDefault(other => !ReferenceEquals(other, scaffold) && Bears(other, scaffold.EntryPoint!))
                                          : null;

            checkedBatch.Add(bearer is null ? scaffold : Refused(scaffold, GeneratorOf(bearer)));
        }

        return checkedBatch;
    }

    /// <summary>
    ///     Against the project: a scaffold whose entry point would declare a type the compilation already holds
    ///     under that name, in that namespace, is refused.
    /// </summary>
    /// <param name="compilation">The developer's compilation, which every symbol is resolved against.</param>
    /// <param name="scaffold">One scaffold, as the engine produced it.</param>
    /// <returns>The refusal carrying both declarations, or null where nothing bears the name.</returns>
    /// <remarks>
    ///     The project's own types, whatever their accessibility, and the accessible types of what it references:
    ///     a public <c>Shop.Domain.AnyVoiceEntry</c> in a referenced generators project is hidden by the new
    ///     declaration rather than refused by the compiler (<c>CS0436</c>), and every <c>new AnyVoiceEntry()</c>
    ///     in the project then binds to a static class. A generic type does not count: arity is part of a type's
    ///     identity, as the shadowing check of §7 already reads it.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static ScaffoldOutcome? AgainstTheCompilation(Compilation compilation, ScaffoldOutcome scaffold) {
        if (compilation is null) { throw new ArgumentNullException(nameof(compilation)); }
        if (scaffold is null) { throw new ArgumentNullException(nameof(scaffold)); }

        if (!CanClash(scaffold)) { return null; }

        ScaffoldedEntryPoint entry  = scaffold.EntryPoint!;
        INamedTypeSymbol?    bearer = Declared(compilation, entry.Namespace, entry.TypeName);

        return bearer is null ? null : Refused(scaffold, DeclarationOf(bearer));
    }

    /// <summary>
    ///     The entry-point namespace against the root class: a namespace whose last segment is the root's own name
    ///     hides the class from every call site written in its parent.
    /// </summary>
    /// <param name="entryPoint">The entry-point file a scaffold produced, or null where none was asked for.</param>
    /// <returns>The warning, or null where the namespace leaves the root visible.</returns>
    /// <remarks>
    ///     A warning, not a refusal, unlike the two questions above: nothing here fails to compile where it is
    ///     written, and a call site in another namespace, or one that opens this namespace with a
    ///     <c>using</c>, resolves the root all the same. The tool cannot see the call sites, so it says what the
    ///     common layout costs and leaves the choice to the developer (issue #384, whose own documentation page
    ///     suggested exactly this layout).
    ///     <para>
    ///         The root is <c>Any</c> for <see cref="EntryPointKind.Any" /> — a namespace <c>Shop.Tests.Any</c>
    ///         hides the library's façade in the same way — and the static root's own name otherwise.
    ///     </para>
    /// </remarks>
    public static ScaffoldWarning? RootAgainstNamespace(ScaffoldedEntryPoint? entryPoint) {
        if (entryPoint?.Namespace is not { } @namespace) { return null; }

        string root = entryPoint.Kind == EntryPointKind.Any ? EntryPointOptions.ReservedRootName : entryPoint.TypeName;
        int    last = @namespace.LastIndexOf('.');

        return string.Equals(@namespace.Substring(last + 1), root, StringComparison.Ordinal) ? ScaffoldWarning.HidesRoot(root, @namespace) : null;
    }

    /// <summary>Whether <paramref name="scaffold" /> declares a type this check is about.</summary>
    private static bool CanClash(ScaffoldOutcome scaffold) {
        return scaffold.Succeeded && scaffold.EntryPoint is { Kind: EntryPointKind.Any };
    }

    /// <summary>Whether <paramref name="other" />'s generator bears the name <paramref name="entry" /> declares.</summary>
    private static bool Bears(ScaffoldOutcome other, ScaffoldedEntryPoint entry) {
        return other.Succeeded
            && other.Plan!.GeneratorName == entry.TypeName
            && other.Plan.Target.Namespace == entry.Namespace;
    }

    /// <summary>
    ///     The type the compilation declares as <paramref name="name" /> in <paramref name="namespace" />, or null.
    /// </summary>
    private static INamedTypeSymbol? Declared(Compilation compilation, string? @namespace, string name) {
        INamespaceSymbol? scope = compilation.GlobalNamespace;

        foreach (string segment in (@namespace ?? string.Empty).Split(['.'], StringSplitOptions.RemoveEmptyEntries)) {
            scope = scope.GetNamespaceMembers().FirstOrDefault(member => member.Name == segment);

            if (scope is null) { return null; }
        }

        return scope.GetTypeMembers(name, arity: 0)
                    .FirstOrDefault(type => SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly)
                                         || compilation.IsSymbolAccessibleWithin(type, compilation.Assembly));
    }

    private static ScaffoldOutcome Refused(ScaffoldOutcome scaffold, string bearer) {
        return ScaffoldOutcome.Refused(ScaffoldStatus.EntryPointNameTaken, [EntryPointOf(scaffold), bearer]);
    }

    /// <summary>What the refused scaffold would have declared, and where — the first of the two candidates.</summary>
    private static string EntryPointOf(ScaffoldOutcome scaffold) {
        ScaffoldedEntryPoint entry = scaffold.EntryPoint!;

        return $"{entry.TypeName} in {entry.File.FileName} — the entry point of {scaffold.Plan!.Target.Argument}";
    }

    /// <summary>The generator of another type of the run, bearing the name — the second candidate.</summary>
    private static string GeneratorOf(ScaffoldOutcome bearer) {
        return $"{bearer.Plan!.GeneratorName} in {bearer.File!.FileName} — the generator of {bearer.Plan.Target.Argument}, scaffolded in this run";
    }

    /// <summary>
    ///     A type the project already holds under the name — the second candidate — placed by its file where it
    ///     has one, by its assembly where it was referenced.
    /// </summary>
    private static string DeclarationOf(INamedTypeSymbol bearer) {
        string? path = bearer.DeclaringSyntaxReferences
                             .Select(reference => reference.SyntaxTree.FilePath)
                             .FirstOrDefault(file => file.Length > 0);

        string place = path is null ? bearer.ContainingAssembly.Name : Path.GetFileName(path);

        return $"{bearer.ToDisplayString()} in {place} — already declared";
    }

}
