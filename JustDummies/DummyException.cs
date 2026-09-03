namespace JustDummies;

/// <summary>
///     Base class of every exception the library throws on its own behalf, so a caller can catch "anything JustDummies
///     rejected" with a single clause. Concrete cases:
///     <see cref="ConflictingDummyConstraintException" /> when two declared constraints cannot be satisfied together,
///     and <see cref="DummyGenerationException" /> when a generation fails even though the constraints were accepted.
/// </summary>
/// <remarks>
///     Named after the dummies the library produces rather than after the <see cref="Dummy" /> entry point: an
///     entry-point-prefixed name reads as a generic catch-all rather than the bounded, library-specific set this
///     type denotes — which made a single-clause catch look like it caught everything.
/// </remarks>
public abstract class DummyException : Exception {

    /// <summary>
    ///     Initializes a new instance of the <see cref="DummyException" /> class.
    /// </summary>
    /// <param name="message">A description of the failure.</param>
    protected DummyException(string message) : base(message) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DummyException" /> class wrapping an underlying failure.
    /// </summary>
    /// <param name="message">A description of the failure.</param>
    /// <param name="innerException">The underlying failure.</param>
    protected DummyException(string message, Exception innerException) : base(message, innerException) { }

}
