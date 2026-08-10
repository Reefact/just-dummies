namespace JustDummies.GenAny;

/// <summary>
///     How a scaffold ended.
/// </summary>
/// <remarks>
///     Failure travels as data, never as an exception (§10.3). The shell maps these to the exit codes of §7
///     without catching anything, and an IDE consumer reads them without a try block — which is what keeps the
///     boundary between the engine and its callers from leaking.
/// </remarks>
public enum ScaffoldStatus {

    /// <summary>A file was produced. It may still carry TODOs, which is a success (§7).</summary>
    Scaffolded = 0,

    /// <summary>
    ///     The project does not reference JustDummies, so not one expression could be resolved (ADR-0059).
    /// </summary>
    LibraryNotReferenced = 1,

    /// <summary>
    ///     Nothing constructs the target: no public instance constructor, or only ones taking <c>ref</c> or
    ///     <c>out</c> parameters, which <c>Generate()</c> could not call (§5.1).
    /// </summary>
    NoEligibleConstructor = 2

}
