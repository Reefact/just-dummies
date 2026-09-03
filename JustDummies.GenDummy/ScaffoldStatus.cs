namespace JustDummies.GenDummy;

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
    NoEligibleConstructor = 2,

    /// <summary>
    ///     The named type matched nothing in this compilation. The outcome carries the closest names, so the
    ///     answer is a correction rather than a denial (§3.2).
    /// </summary>
    TypeNotFound = 3,

    /// <summary>
    ///     The name matched several types. The outcome carries their full names, and the developer says which
    ///     — the engine does not pick.
    /// </summary>
    TypeAmbiguous = 4,

    /// <summary>
    ///     The target is abstract, so <c>Generate()</c>'s <c>new</c> would not compile (§5.1).
    /// </summary>
    /// <remarks>
    ///     A public constructor on an abstract class is legal and effectively protected, so the constructor
    ///     choice of §5.1 finds one and the file emitted from it looked complete — <c>CS0144</c> at the
    ///     developer's next build, after a recap claiming every parameter inferred.
    /// </remarks>
    TypeIsAbstract = 5,

    /// <summary>
    ///     The target is generic, or nested in a generic type, so the emitted file could not name it (§5.1).
    /// </summary>
    /// <remarks>
    ///     Nothing supplies the type argument: the generator would declare <c>IDummy&lt;Envelope&lt;TPayload&gt;&gt;</c>
    ///     with <c>TPayload</c> bound to nothing, which is <c>CS0246</c> wherever it appears.
    /// </remarks>
    TypeIsGeneric = 6,

    /// <summary>
    ///     The target declares required members the chosen constructor does not set (§5.1, §16).
    /// </summary>
    /// <remarks>
    ///     §16 defers required members to a later version, and this is what deferring one has to mean: a
    ///     refusal naming the case, never a file that says <c>1 of 1 parameters inferred</c> and then fails
    ///     the developer's build with <c>CS9035</c>. A constructor marked <c>[SetsRequiredMembers]</c> sets
    ///     them, and is scaffolded like any other.
    /// </remarks>
    RequiredMembersUnset = 7

}
