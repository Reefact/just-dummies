namespace JustDummies.GenDummy;

/// <summary>
///     What writing one chain settled, beyond the text of it.
/// </summary>
/// <remarks>
///     The expression alone cannot answer the question §6 asks. A chain reading
///     <c>Dummy.Enum&lt;Status&gt;()</c> is what the base table produces when there was nothing to add, and it is
///     also what comes out when a guard was read, understood, and then found to have no member on that
///     generator — two facts a developer needs to tell apart, and one string that cannot tell them.
///     <para>
///         So the writing reports itself, and the recap is computed from what was <b>applied</b> rather than
///         from what was read. That one word is the difference between a column that is honest and a column
///         that asserts an invariant nobody honoured.
///     </para>
/// </remarks>
internal sealed class ChainReport {

    internal ChainReport(string expression, bool guardApplied, bool guardsNotCombined, bool constraintUnavailable) {
        Expression            = expression;
        GuardApplied          = guardApplied;
        GuardsNotCombined     = guardsNotCombined;
        ConstraintUnavailable = constraintUnavailable;
    }

    /// <summary>The chain as the emitted file spells it.</summary>
    internal string Expression { get; }

    /// <summary>
    ///     Whether anything a guard said reached the chain.
    /// </summary>
    /// <remarks>
    ///     Read before the range fold, since that rewrites a floor and a ceiling into one call neither of them
    ///     is — the fold is how the chain is spelled, never what it says.
    /// </remarks>
    internal bool GuardApplied { get; }

    /// <summary>Whether guards were dropped because together they admitted no value (§5.3).</summary>
    internal bool GuardsNotCombined { get; }

    /// <summary>Whether a guard survived composition and then found no member to be written with (ADR-0059).</summary>
    internal bool ConstraintUnavailable { get; }

}
