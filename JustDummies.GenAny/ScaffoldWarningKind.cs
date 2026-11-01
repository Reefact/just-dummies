namespace JustDummies.GenAny;

/// <summary>
///     The warnings a scaffold can carry.
/// </summary>
public enum ScaffoldWarningKind {

    /// <summary>
    ///     The scaffolded generator's name is one the library already uses, and inside the emitted file's own
    ///     namespace it silently shadows it — C# resolves the enclosing namespace before any <c>using</c>.
    /// </summary>
    /// <remarks>
    ///     It compiles; it is just wrong later. The tool warns, names both types, and generates anyway: under
    ///     the design rule that the developer stays in charge of their own code, renaming is their call, and
    ///     v1.1's naming option (§16) is what gives them the switch.
    ///     <para>
    ///         The check compares <b>arity</b> as well as name, and that narrows it considerably. The library
    ///         declares forty public <c>Any*</c> names, but eight of them are generic — <c>AnyList&lt;T&gt;</c>,
    ///         <c>AnySet&lt;T&gt;</c>, <c>AnyEnum&lt;T&gt;</c> and the rest — and arity is part of a type's
    ///         identity in C#, so a scaffolded <c>AnySet</c> and the library's <c>AnySet&lt;T&gt;</c> coexist
    ///         without shadowing anything. A domain type named <c>Set</c>, <c>List</c> or <c>Sequence</c> is a
    ///         false alarm, and warning on all forty would cry wolf on the eight that cannot collide.
    ///     </para>
    /// </remarks>
    ShadowsLibraryType = 0,

    /// <summary>
    ///     The entry-point namespace ends in the root class's own name, so the namespace hides the class from any
    ///     call site written in its parent (§4.5).
    /// </summary>
    /// <remarks>
    ///     C# resolves a simple name in the enclosing namespace before it looks at a type another one declares:
    ///     with <c>Arbitrary</c> declared in <c>Shop.Tests.Arbitrary</c>, a test in <c>Shop.Tests</c> binds
    ///     <c>Arbitrary</c> to the namespace and <c>Arbitrary.Order()</c> fails with <c>CS0234</c>. It compiles
    ///     where the file is, which is why this is a warning and not the refusal of the name clashes that share its
    ///     check: a test in another namespace, or one opening the namespace with a <c>using</c>, resolves it, and
    ///     the engine cannot see the call sites.
    /// </remarks>
    RootHiddenByNamespace = 1

}
