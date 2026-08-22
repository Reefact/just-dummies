namespace JustDummies;

/// <summary>
///     A quantifier: the child repeated between <c>min</c> and <c>max</c> times. An unbounded quantifier
///     (<c>*</c>, <c>+</c>, <c>{n,}</c>) has no <c>max</c>; generation then draws <c>min</c> plus 0 to
///     <see cref="UnboundedExtra" /> extra repetitions, the same bounded-spread default the rest of the library uses.
/// </summary>
internal sealed class RegexRepeat : RegexNode {

    #region Statics members declarations

    /// <summary>How many repetitions above the minimum an unbounded quantifier may add.</summary>
    internal const int UnboundedExtra = 8;

    #endregion

    #region Fields declarations

    private readonly RegexNode _child;
    private readonly int?      _max;
    private readonly int       _min;

    #endregion

    internal RegexRepeat(RegexNode child, int min, int? max) {
        if (child is null) { throw new ArgumentNullException(nameof(child)); }
        _child = child;
        _min   = min;
        _max   = max;
    }

    internal override void Append(RegexGenerationContext context) {
        if (context is null) { throw new ArgumentNullException(nameof(context)); }
        // The unbounded count is widened to long before the extra repetitions are added: a minimum within
        // UnboundedExtra of int.MaxValue would otherwise wrap negative, and a negative count writes nothing at all —
        // silently yielding a value the pattern does not match, which is the one outcome generation must never
        // produce. Widened, such a count simply walks until the generation ceiling reports the overrun, exactly as
        // any other minimum too large to fit does.
        long count = _max is int max
                         ? context.Random.NextInt32Inclusive(_min, max)
                         : (long)_min + context.Random.Next(0, UnboundedExtra + 1);

        for (long repetition = 0; repetition < count; repetition++) { _child.Append(context); }
    }

}
