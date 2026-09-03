namespace JustDummies.GenDummy;

/// <summary>
///     How a scaffolded generator is reached, beyond <c>new Dummy{Type}()</c>.
/// </summary>
/// <remarks>
///     An entry point is <b>additive</b>: whichever kind is chosen, the generator file itself is emitted
///     unchanged, and <c>new Dummy{Type}()</c> keeps working. What varies is whether a second file is written
///     beside it, and what the developer may write instead.
/// </remarks>
public enum EntryPointKind {

    /// <summary>No entry point. The generator is reached with <c>new Dummy{Type}()</c>, and nothing else is emitted.</summary>
    None = 0,

    /// <summary>
    ///     A static root the developer owns — <c>Dummies.Order()</c>.
    /// </summary>
    /// <remarks>
    ///     The root is <c>partial</c> and each scaffold contributes its own part, so no file is ever read and
    ///     rewritten to add a member to it. It uses no construct newer than C# 7.3, like the generator itself.
    /// </remarks>
    StaticRoot = 1,

    /// <summary>
    ///     An extension member on the library's own façade — <c>Dummy.Order()</c>.
    /// </summary>
    /// <remarks>
    ///     A <c>partial</c> declaration cannot cross an assembly boundary, and a static class named <c>Dummy</c>
    ///     in the developer's own project would hide <c>JustDummies.Dummy</c> for its whole namespace rather than
    ///     extend it — <c>Dummy.Int32()</c> would stop compiling. A C# 14 static extension member is what reaches
    ///     this spelling without either.
    /// </remarks>
    Dummy = 2

}
