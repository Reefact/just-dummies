namespace JustDummies.GenDummy;

/// <summary>
///     How the emitted generator type is named, as a pattern rather than as a hardcoded prefix.
/// </summary>
/// <remarks>
///     v1.0 offers exactly one pattern, <see cref="Default" />, and no way to choose another: naming options
///     (<c>--name</c>, <c>--pattern</c>, a project-wide <c>dum.json</c>) are deferred to v1.1 (specification §16).
///     <para>
///         This type exists now anyway, and carries the pattern rather than the prefix, because §11.3 asks that
///         v1.1 be a change to <see cref="TypeNaming.GeneratorNameFor" /> plus an options binding — not a sweep
///         through every site that concatenated <c>"Dummy"</c> onto a type name.
///     </para>
/// </remarks>
public sealed class NamingOptions {

    /// <summary>
    ///     The only placeholder a pattern carries. It stands for the target type's own name — the nested type's
    ///     name alone, never the containing type's (specification §3.2).
    /// </summary>
    public const string TypePlaceholder = "{Type}";

    /// <summary>
    ///     <c>Dummy{Type}</c>: the v1.0 pattern, and the default v1.1 keeps so that an existing project sees no
    ///     change when the option arrives.
    /// </summary>
    public static NamingOptions Default { get; } = new("Dummy" + TypePlaceholder);

    private NamingOptions(string pattern) {
        Pattern = pattern;
    }

    /// <summary>
    ///     The pattern a generator name is rendered from, carrying <see cref="TypePlaceholder" /> once.
    /// </summary>
    public string Pattern { get; }

}
