namespace JustDummies.GenAny;

/// <summary>
///     The second file a scaffold may produce, and the call it makes possible.
/// </summary>
/// <remarks>
///     <see cref="Call" /> is carried rather than left for the shell to assemble, for the same reason every
///     provenance word is (§6): the engine decides, the shell renders. A console that rebuilt
///     <c>Arbitrary.Order()</c> from a root name and a type name would be a second place the spelling is
///     decided, and the two would drift.
///     <para>
///         <see cref="TypeName" /> and <see cref="Namespace" /> say what the file <b>declares</b>, which is the
///         question the pre-write name check of §4.5 asks of it (<see cref="EntryPointNameCheck" />): a name
///         the file would declare is compared with the names already borne before the file is written, and
///         the comparison reads these rather than the emitted text.
///     </para>
/// </remarks>
public sealed class ScaffoldedEntryPoint {

    internal ScaffoldedEntryPoint(ScaffoldedFile file, string call, EntryPointKind kind, string typeName, string? @namespace) {
        File      = file;
        Call      = call;
        Kind      = kind;
        TypeName  = typeName;
        Namespace = @namespace;
    }

    /// <summary>The emitted file — <c>AnyOrder.Entry.cs</c>.</summary>
    public ScaffoldedFile File { get; }

    /// <summary>What the developer may now write — <c>Arbitrary.Order()</c>, or <c>Any.Order()</c>.</summary>
    public string Call { get; }

    /// <summary>Which entry point the file carries.</summary>
    public EntryPointKind Kind { get; }

    /// <summary>
    ///     The type the file declares: the static root — <c>Arbitrary</c> — or, for <see cref="EntryPointKind.Any" />,
    ///     the class carrying the extension block — <c>AnyOrderEntry</c>.
    /// </summary>
    public string TypeName { get; }

    /// <summary>The namespace that type is declared in, or null for the global namespace.</summary>
    public string? Namespace { get; }

    /// <inheritdoc />
    public override string ToString() {
        return $"{File.FileName} ({Call})";
    }

}
