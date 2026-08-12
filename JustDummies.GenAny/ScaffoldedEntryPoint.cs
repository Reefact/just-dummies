namespace JustDummies.GenAny;

/// <summary>
///     The second file a scaffold may produce, and the call it makes possible.
/// </summary>
/// <remarks>
///     <see cref="Call" /> is carried rather than left for the shell to assemble, for the same reason every
///     provenance word is (§6): the engine decides, the shell renders. A console that rebuilt
///     <c>Dummies.Order()</c> from a root name and a type name would be a second place the spelling is
///     decided, and the two would drift.
/// </remarks>
public sealed class ScaffoldedEntryPoint {

    internal ScaffoldedEntryPoint(ScaffoldedFile file, string call) {
        File = file;
        Call = call;
    }

    /// <summary>The emitted file — <c>AnyOrder.Entry.cs</c>.</summary>
    public ScaffoldedFile File { get; }

    /// <summary>What the developer may now write — <c>Dummies.Order()</c>, or <c>Any.Order()</c>.</summary>
    public string Call { get; }

    /// <inheritdoc />
    public override string ToString() {
        return $"{File.FileName} ({Call})";
    }

}
