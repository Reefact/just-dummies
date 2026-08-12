using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace JustDummies.GenAny;

/// <summary>
///     How a type is spelled in the emitted file, and which namespace has to be opened for that spelling to
///     bind.
/// </summary>
/// <remarks>
///     The two go together on purpose. A short name and its <c>using</c> are one decision — write
///     <c>IReadOnlyList&lt;string&gt;</c> and you owe the file <c>System.Collections.Generic</c> — and splitting
///     them is how a scaffolder ends up emitting a name nothing resolves.
/// </remarks>
internal sealed class TypeNames {

    /// <summary>
    ///     Short names, keywords for the built-in types, type parameters included.
    /// </summary>
    private static readonly SymbolDisplayFormat Short =
        new(globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private static readonly SymbolDisplayFormat Bare =
        new(globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private readonly SortedSet<string> namespaces = new(StringComparer.Ordinal);

    private readonly string? own;

    /// <summary>
    ///     Names types for a file that will sit in <paramref name="fileNamespace" />.
    /// </summary>
    /// <param name="fileNamespace">
    ///     The namespace the emitted file declares, so a type already in it costs no <c>using</c> — and a type
    ///     that is <b>not</b> in it does, which is the case a <c>--namespace</c> override creates.
    /// </param>
    internal TypeNames(string? fileNamespace) {
        own = fileNamespace;
    }

    /// <summary>The namespaces named so far, ordered, with the library's own always among them.</summary>
    internal IReadOnlyList<string> Usings {
        get {
            List<string> opened = ["JustDummies"];

            opened.AddRange(namespaces.Where(@namespace => @namespace != "JustDummies"));

            return opened;
        }
    }

    /// <summary>
    ///     The short spelling of <paramref name="type" />, recording every namespace it needs on the way.
    /// </summary>
    /// <remarks>
    ///     The nullable annotation is dropped from a reference type because it would promise something the file
    ///     does not do: the emitted generator never draws null for a parameter (ADR-0064), so
    ///     <c>IAny&lt;Customer?&gt;</c> would be a lie in the field declaration. A value type's <c>?</c> is a
    ///     different thing — part of the type rather than an annotation on it — and stays.
    /// </remarks>
    internal string Of(ITypeSymbol type) {
        Record(type);

        return type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString(Short);
    }

    /// <summary>Opens a namespace the file needs for a name it did not get through <see cref="Of" />.</summary>
    internal void Open(string @namespace) {
        if (@namespace.Length > 0 && @namespace != own) { namespaces.Add(@namespace); }
    }

    /// <summary>
    ///     Walks a type for the namespaces its short spelling leans on: its own, its containing types', and
    ///     every type argument's, recursively. An array contributes its element's.
    /// </summary>
    private void Record(ITypeSymbol type) {
        if (type is IArrayTypeSymbol array) {
            Record(array.ElementType);

            return;
        }

        if (type is not INamedTypeSymbol named) { return; }

        // `int?` is written from its argument alone, so it owes System nothing — whatever T is owes its own.
        if (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) {
            Record(named.TypeArguments[0]);

            return;
        }

        foreach (ITypeSymbol argument in named.TypeArguments) { Record(argument); }

        // A type the compiler spells as a keyword — int, string, bool — names no namespace at the point of use.
        if (SyntaxFacts.GetKeywordKind(named.ToDisplayString(Bare)) != SyntaxKind.None) { return; }

        INamedTypeSymbol outermost = named;

        while (outermost.ContainingType is not null) { outermost = outermost.ContainingType; }

        Open(NamespaceOf(outermost));
    }

    /// <summary>
    ///     The namespace a type needs opening, or nothing when it needs none.
    /// </summary>
    /// <remarks>
    ///     The global namespace has to be read as "no namespace" rather than displayed, and that is the whole
    ///     of this method: <c>ToDisplayString()</c> renders it as the literal <c>&lt;global namespace&gt;</c>,
    ///     which the emitter would then write out as a <c>using</c> directive that does not parse. Two cases
    ///     reach it — a domain type declared outside any namespace, and an <b>error</b> type, since a
    ///     parameter whose type failed to bind is reported as living in the global namespace. The second is
    ///     the likelier one in the field: it needs only a project that opened with an unresolved reference,
    ///     which §11.1 surfaces and carries on from.
    /// </remarks>
    private static string NamespaceOf(INamedTypeSymbol type) {
        INamespaceSymbol? containing = type.ContainingNamespace;

        return containing is null || containing.IsGlobalNamespace ? string.Empty : containing.ToDisplayString();
    }

}
