using System;

using Microsoft.CodeAnalysis.CSharp;

namespace JustDummies.GenDummy;

/// <summary>
///     Which entry point a scaffold also emits, and where that file is declared.
/// </summary>
/// <remarks>
///     Kept apart from <see cref="ScaffoldOptions" />'s own namespace override on purpose: the two answer
///     different questions. <see cref="ScaffoldOptions.NamespaceOverride" /> moves the generator, which every
///     call site names; <see cref="NamespaceOverride" /> moves only the entry-point file, which is what makes a
///     single root reachable across several namespaces without disturbing where the generators live (ADR-0062).
/// </remarks>
public sealed class EntryPointOptions {

    /// <summary>
    ///     The root name a static entry point may not take.
    /// </summary>
    /// <remarks>
    ///     A static class named <c>Dummy</c> in the developer's project does not extend the library's façade: C#
    ///     resolves a simple type name in the enclosing namespace before any <c>using</c>, so it hides it, and
    ///     <c>Dummy.Int32()</c> fails to compile with <c>CS0117</c> — verified. <see cref="EntryPointKind.Dummy" />
    ///     is what this name is asking for, and it is a different mechanism.
    /// </remarks>
    public const string ReservedRootName = "Dummy";

    private EntryPointOptions(EntryPointKind kind, string? rootName, string? namespaceOverride) {
        Kind              = kind;
        RootName          = rootName;
        NamespaceOverride = namespaceOverride;
    }

    /// <summary>Nothing beyond the generator itself, which is what a scaffold does unless asked otherwise.</summary>
    public static EntryPointOptions None { get; } = new(EntryPointKind.None, rootName: null, namespaceOverride: null);

    /// <summary>The entry point hangs off the library's own façade — <c>Dummy.Order()</c>.</summary>
    public static EntryPointOptions OnDummy { get; } = new(EntryPointKind.Dummy, rootName: null, namespaceOverride: null);

    /// <summary>Which entry point is emitted.</summary>
    public EntryPointKind Kind { get; }

    /// <summary>The static root's name for <see cref="EntryPointKind.StaticRoot" />, null otherwise.</summary>
    public string? RootName { get; }

    /// <summary>Where the entry-point file is declared, or null to follow the generator.</summary>
    public string? NamespaceOverride { get; }

    /// <summary>What the developer writes before the type's name — <c>Dummies</c>, or <c>Dummy</c>.</summary>
    public string Root => Kind == EntryPointKind.Dummy ? ReservedRootName : RootName ?? string.Empty;

    /// <summary>
    ///     The entry point is a static root the developer owns — <c>Dummies.Order()</c>.
    /// </summary>
    /// <param name="rootName">The root class's name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rootName" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="rootName" /> is not a C# identifier, or is
    ///     <see cref="ReservedRootName" />.</exception>
    public static EntryPointOptions OnStaticRoot(string rootName) {
        if (rootName is null) { throw new ArgumentNullException(nameof(rootName)); }

        string name = rootName.Trim();

        if (name == ReservedRootName) {
            throw new ArgumentException($"A static root named '{ReservedRootName}' hides the library's own façade "
                                      + "rather than extending it; ask for the Dummy kind instead.",
                                        nameof(rootName));
        }
        if (!SyntaxFacts.IsValidIdentifier(name) || SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None) {
            throw new ArgumentException($"'{rootName}' is not a C# identifier, so it cannot name a class.",
                                        nameof(rootName));
        }

        return new EntryPointOptions(EntryPointKind.StaticRoot, name, namespaceOverride: null);
    }

    /// <summary>The same entry point, declared in <paramref name="namespace" /> rather than beside the generator.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="namespace" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="namespace" /> is blank, or this is
    ///     <see cref="None" />.</exception>
    public EntryPointOptions InNamespace(string @namespace) {
        if (@namespace is null) { throw new ArgumentNullException(nameof(@namespace)); }
        if (@namespace.Trim().Length == 0) {
            throw new ArgumentException("A namespace override names a namespace; omit it to follow the generator.",
                                        nameof(@namespace));
        }
        if (Kind == EntryPointKind.None) {
            throw new ArgumentException("There is no entry-point file to place: ask for a kind first.",
                                        nameof(@namespace));
        }

        return new EntryPointOptions(Kind, RootName, @namespace.Trim());
    }

    /// <inheritdoc />
    public override string ToString() {
        return Kind == EntryPointKind.None ? nameof(EntryPointKind.None) : Root;
    }

}
