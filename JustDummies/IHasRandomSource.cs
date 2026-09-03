namespace JustDummies;

/// <summary>
///     Implemented by the library's own generators so that derived generators (<c>As</c>, <c>Combine</c>) can
///     propagate the random context of their operands, and so that a generation failure can resolve the seed to
///     report. Foreign <see cref="IDummy{T}" /> implementations simply do not carry one, and a derived generator
///     built over a foreign one carries <c>null</c>.
/// </summary>
internal interface IHasRandomSource {

    RandomSource? Source { get; }

}
