using System;

namespace JustDummies.GenAny;

/// <summary>
///     Something worth saying about a scaffold that did not stop it.
/// </summary>
/// <remarks>
///     Typed, never a console string (§10.3): the shell renders it, and an IDE consumer would show it its own
///     way. A warning never changes the exit code — the file is written either way, and under design rule 4 the
///     decision is the developer's.
/// </remarks>
public sealed class ScaffoldWarning {

    private ScaffoldWarning(ScaffoldWarningKind kind, string subject, string other) {
        Kind    = kind;
        Subject = subject;
        Other   = other;
    }

    /// <summary>What kind of warning this is.</summary>
    public ScaffoldWarningKind Kind { get; }

    /// <summary>The name it is about — the scaffolded <c>AnyPattern</c>, or the root class <c>Arbitrary</c>.</summary>
    public string Subject { get; }

    /// <summary>What it collides with — <c>JustDummies.AnyPattern</c>, or the namespace <c>Shop.Tests.Arbitrary</c>.</summary>
    public string Other { get; }

    /// <summary>
    ///     The scaffolded generator's name is one the library already uses (§7).
    /// </summary>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static ScaffoldWarning Shadows(string generator, string libraryType) {
        if (generator is null) { throw new ArgumentNullException(nameof(generator)); }
        if (libraryType is null) { throw new ArgumentNullException(nameof(libraryType)); }

        return new ScaffoldWarning(ScaffoldWarningKind.ShadowsLibraryType, generator, libraryType);
    }

    /// <summary>
    ///     The entry-point namespace ends in the root class's name, which hides the class from its parent's call
    ///     sites (§4.5).
    /// </summary>
    /// <param name="root">The root class — <c>Arbitrary</c>, or the library's <c>Any</c>.</param>
    /// <param name="entryPointNamespace">The namespace whose last segment is <paramref name="root" />.</param>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static ScaffoldWarning HidesRoot(string root, string entryPointNamespace) {
        if (root is null) { throw new ArgumentNullException(nameof(root)); }
        if (entryPointNamespace is null) { throw new ArgumentNullException(nameof(entryPointNamespace)); }

        return new ScaffoldWarning(ScaffoldWarningKind.RootHiddenByNamespace, root, entryPointNamespace);
    }

    /// <inheritdoc />
    public override string ToString() {
        return $"{Kind}: {Subject} / {Other}";
    }

}
