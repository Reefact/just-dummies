using Microsoft.CodeAnalysis;

namespace JustDummies.Analyzers;

/// <summary>
///     A call <see cref="DeclarationWalk" /> found answering a declaration made before it on the same chain: either a
///     second, different declaration of a constraint the generator takes once, which the run time rejects (JD034), or the
///     same declaration repeated, which the run time ignores (JD035) — a whole call, or a value an exclusion list already
///     holds.
/// </summary>
internal sealed class DeclarationFinding {

    private DeclarationFinding(IOperation call, IOperation earlier, string? conflict) {
        Call     = call;
        Earlier  = earlier;
        Conflict = conflict;
    }

    /// <summary>The call the finding is about, or the value written twice in one exclusion list.</summary>
    public IOperation Call { get; }

    /// <summary>The declaration before it that the call answers.</summary>
    public IOperation Earlier { get; }

    /// <summary>Why the run time rejects the call, or <c>null</c> when the call repeats <see cref="Earlier" />.</summary>
    public string? Conflict { get; }

    /// <summary>Whether the call repeats <see cref="Earlier" />, so it changes nothing.</summary>
    public bool IsRepetition => Conflict is null;

    #region Statics members declarations

    public static DeclarationFinding Conflicting(IOperation call, IOperation earlier, string conflict) {
        return new DeclarationFinding(call, earlier, conflict);
    }

    public static DeclarationFinding Repeating(IOperation call, IOperation earlier) {
        return new DeclarationFinding(call, earlier, null);
    }

    #endregion

}
