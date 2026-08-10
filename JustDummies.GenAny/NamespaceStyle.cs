namespace JustDummies.GenAny;

/// <summary>
///     How the emitted file declares its namespace.
/// </summary>
/// <remarks>
///     Copied from the target type's own declaration rather than chosen, so the scaffolded file looks like the
///     files around it (specification §4.4). It is the one place the emitted code is allowed past C# 7.3: a
///     file-scoped namespace is C# 10, and it is emitted only where the developer already writes one.
/// </remarks>
public enum NamespaceStyle {

    /// <summary>The target type sits in the global namespace; the emitted file declares none.</summary>
    None = 0,

    /// <summary><c>namespace Shop.Domain;</c> — C# 10 and later.</summary>
    FileScoped = 1,

    /// <summary><c>namespace Shop.Domain { … }</c> — every language version.</summary>
    Block = 2

}
