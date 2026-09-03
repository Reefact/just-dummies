namespace JustDummies.GenDummy;

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
    ///         declares forty public <c>Dummy*</c> names, but eight of them are generic — <c>DummyList&lt;T&gt;</c>,
    ///         <c>DummySet&lt;T&gt;</c>, <c>DummyEnum&lt;T&gt;</c> and the rest — and arity is part of a type's
    ///         identity in C#, so a scaffolded <c>DummySet</c> and the library's <c>DummySet&lt;T&gt;</c> coexist
    ///         without shadowing anything. A domain type named <c>Set</c>, <c>List</c> or <c>Sequence</c> is a
    ///         false alarm, and warning on all forty would cry wolf on the eight that cannot collide.
    ///     </para>
    /// </remarks>
    ShadowsLibraryType = 0

}
