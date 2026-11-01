using System;

namespace JustDummies.GenAny;

/// <summary>
///     A composed type <c>any</c> cannot scaffold — today, an abstract one — and the generator a composing scaffold
///     would have named for it (§5.5).
/// </summary>
/// <remarks>
///     Carried so the open parameter's comment can name both, which is the instruction the developer acts on: write
///     <c>AnyIRowElement : IAny&lt;IRowElement&gt;</c> by hand. Names only, as the emitted file spells them; nothing
///     about the type's implementations is looked up (ADR-0089).
/// </remarks>
public sealed class Unscaffoldable {

    /// <summary>Declares an abstract type and the generator to write for it.</summary>
    /// <param name="type">The type, as the emitted file spells it — <c>IRowElement</c>.</param>
    /// <param name="generator">The generator a composing scaffold names for it — <c>AnyIRowElement</c>.</param>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public Unscaffoldable(string type, string generator) {
        Type      = type ?? throw new ArgumentNullException(nameof(type));
        Generator = generator ?? throw new ArgumentNullException(nameof(generator));
    }

    /// <summary>The type, as the emitted file spells it.</summary>
    public string Type { get; }

    /// <summary>The generator to write for it by hand.</summary>
    public string Generator { get; }

    /// <inheritdoc />
    public override string ToString() {
        return $"{Generator} : IAny<{Type}>";
    }

}
