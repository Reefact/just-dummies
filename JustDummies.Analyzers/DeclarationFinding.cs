using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     A call <see cref="DeclarationWalk" /> found answering a declaration made before it on the same chain: either a
///     second, different declaration of a constraint the generator takes once, which the run time rejects (JD034), or the
///     same declaration repeated, which the run time ignores (JD035).
/// </summary>
internal sealed class DeclarationFinding {

    private DeclarationFinding(IInvocationOperation call, IInvocationOperation earlier, string? conflict) {
        Call     = call;
        Earlier  = earlier;
        Conflict = conflict;
    }

    /// <summary>The call the finding is about.</summary>
    public IInvocationOperation Call { get; }

    /// <summary>The declaration before it that the call answers.</summary>
    public IInvocationOperation Earlier { get; }

    /// <summary>Why the run time rejects the call, or <c>null</c> when the call repeats <see cref="Earlier" />.</summary>
    public string? Conflict { get; }

    /// <summary>Whether the call repeats <see cref="Earlier" />, so it changes nothing.</summary>
    public bool IsRepetition => Conflict is null;

    #region Statics members declarations

    public static DeclarationFinding Conflicting(IInvocationOperation call, IInvocationOperation earlier, string conflict) {
        return new DeclarationFinding(call, earlier, conflict);
    }

    public static DeclarationFinding Repeating(IInvocationOperation call, IInvocationOperation earlier) {
        return new DeclarationFinding(call, earlier, null);
    }

    #endregion

}
