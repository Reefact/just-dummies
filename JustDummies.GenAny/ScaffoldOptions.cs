using System;

namespace JustDummies.GenAny;

/// <summary>
///     What the caller may vary about a scaffold. Everything else is the target type's own doing.
/// </summary>
public sealed class ScaffoldOptions {

    private ScaffoldOptions(string? namespaceOverride, NamingOptions naming) {
        NamespaceOverride = namespaceOverride;
        Naming            = naming;
    }

    /// <summary>The generator lands in the target type's own namespace, under the v1.0 name (ADR-0062).</summary>
    public static ScaffoldOptions Default { get; } = new(namespaceOverride: null, NamingOptions.Default);

    /// <summary>Where the emitted type is declared, or null to follow the target type (<c>--namespace</c>).</summary>
    public string? NamespaceOverride { get; }

    /// <summary>How the emitted type is named.</summary>
    public NamingOptions Naming { get; }

    /// <summary>The same options, emitting into <paramref name="namespace" /> instead of the target's own.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="namespace" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="namespace" /> is blank.</exception>
    public ScaffoldOptions InNamespace(string @namespace) {
        if (@namespace is null) { throw new ArgumentNullException(nameof(@namespace)); }
        if (@namespace.Trim().Length == 0) {
            throw new ArgumentException("A namespace override names a namespace; omit it to follow the target type.",
                                        nameof(@namespace));
        }

        return new ScaffoldOptions(@namespace, Naming);
    }

}
