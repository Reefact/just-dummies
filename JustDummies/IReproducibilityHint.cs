namespace JustDummies;

/// <summary>
///     Implemented by derived generators (<c>As</c>, <c>Combine</c>) to report whether every operand they draw from is
///     itself reproducible. A single source-less (foreign) operand makes the derived value unreproducible even when
///     another operand supplies a non-null <see cref="IHasRandomSource.Source" /> for the replay hint to name — so a
///     full-replay promise must be withheld. Generators that draw only from their own source do not implement this and
///     are treated as reproducible whenever they carry a source.
/// </summary>
internal interface IReproducibilityHint {

    bool DrawsOnlyFromSource { get; }

}
