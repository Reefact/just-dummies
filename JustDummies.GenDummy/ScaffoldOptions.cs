using System;

namespace JustDummies.GenDummy;

/// <summary>
///     What the caller may vary about a scaffold. Everything else is the target type's own doing.
/// </summary>
public sealed class ScaffoldOptions {

    private ScaffoldOptions(string? namespaceOverride, NamingOptions naming, EntryPointOptions entryPoint) {
        NamespaceOverride = namespaceOverride;
        Naming            = naming;
        EntryPoint        = entryPoint;
    }

    /// <summary>The generator lands in the target type's own namespace, under the v1.0 name (ADR-0062).</summary>
    public static ScaffoldOptions Default { get; } = new(namespaceOverride: null, NamingOptions.Default, EntryPointOptions.None);

    /// <summary>Where the emitted type is declared, or null to follow the target type (<c>--namespace</c>).</summary>
    public string? NamespaceOverride { get; }

    /// <summary>How the emitted type is named.</summary>
    public NamingOptions Naming { get; }

    /// <summary>Which entry point is emitted beside the generator, if any (<c>--entry-point</c>).</summary>
    public EntryPointOptions EntryPoint { get; }

    /// <summary>The same options, emitting into <paramref name="namespace" /> instead of the target's own.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="namespace" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="namespace" /> is blank.</exception>
    public ScaffoldOptions InNamespace(string @namespace) {
        if (@namespace is null) { throw new ArgumentNullException(nameof(@namespace)); }
        if (@namespace.Trim().Length == 0) {
            throw new ArgumentException("A namespace override names a namespace; omit it to follow the target type.",
                                        nameof(@namespace));
        }

        return new ScaffoldOptions(@namespace, Naming, EntryPoint);
    }

    /// <summary>The same options, also emitting <paramref name="entryPoint" /> (<c>--entry-point</c>).</summary>
    /// <exception cref="ArgumentNullException"><paramref name="entryPoint" /> is null.</exception>
    public ScaffoldOptions WithEntryPoint(EntryPointOptions entryPoint) {
        if (entryPoint is null) { throw new ArgumentNullException(nameof(entryPoint)); }

        return new ScaffoldOptions(NamespaceOverride, Naming, entryPoint);
    }

}
