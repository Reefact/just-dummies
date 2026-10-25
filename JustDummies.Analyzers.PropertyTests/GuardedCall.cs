using Microsoft.CodeAnalysis.Diagnostics;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     One call on a fresh generator, its argument generated, written once as C# for the analyzer and applied once by
///     the run time, which is the oracle — with whether <c>JD014</c> claims the guard that call meets.
/// </summary>
/// <remarks>
///     <c>JD014</c> claims neither a host that is not empty, not ASCII, nor made of digits and dots, which
///     <c>System.Uri</c> judges and whose parsing differs across target frameworks, nor a user-info part holding a
///     character whose invariant lower case differs across runtimes. Its page states both. A call with such an
///     argument may still be reported for another one the guard certainly rejects.
/// </remarks>
internal sealed class GuardedCall {

    private readonly DeclarationChain _chain;

    public GuardedCall(DeclarationVocabulary vocabulary, DeclarationCall call, bool claimed) {
        _chain  = new DeclarationChain(vocabulary, [call]);
        Claimed = claimed;
    }

    /// <summary>Whether <c>JD014</c> claims to read the guard the call meets for every argument it passes.</summary>
    public bool Claimed { get; }

    /// <summary>
    ///     Whether the call throws on its own argument. Applied to a fresh generator, since the guard runs before the
    ///     call reads anything the chain declared; nothing is drawn.
    /// </summary>
    public bool RuntimeRejectsTheArgument() {
        object generator = _chain.Vocabulary.Create(Any.WithSeed(0));
        try {
            _chain.Calls[0].ApplyTo(generator);

            return false;
        } catch (ArgumentException) {
            return true;
        }
    }

    /// <summary>The ids <paramref name="analyzer" /> reports on the call.</summary>
    public ISet<string> ReportedBy(DiagnosticAnalyzer analyzer) {
        return _chain.ReportedBy(analyzer);
    }

    /// <inheritdoc />
    public override string ToString() {
        return _chain.ToString();
    }

}
