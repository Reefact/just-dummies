namespace JustDummies.GenAny;

/// <summary>
///     The words a drawn pair is written in, which are its builder's own (ADR-0131): the four bounds a pinned side puts
///     on the other, and the two relations <c>Having</c> declares. The dates order by <c>Before</c> and <c>After</c>,
///     everything else by <c>LessThan</c> and <c>GreaterThan</c>.
/// </summary>
/// <remarks>
///     One place, read twice: <see cref="LibrarySurface.CarriesPair" /> asks the compilation for exactly these members
///     before a pair is drawn, and the emitter writes exactly these, so a file never names a member the library it
///     compiles against lacks (ADR-0059).
/// </remarks>
internal sealed class PairVocabulary {

    private static readonly PairVocabulary Comparison = new("GreaterThanOrEqualTo", "GreaterThan", "LessThanOrEqualTo", "LessThan", "Item1LessThanOrEqualToItem2", "Item1LessThanItem2");
    private static readonly PairVocabulary Chronology = new("AfterOrEqualTo", "After", "BeforeOrEqualTo", "Before", "Item1BeforeOrEqualToItem2", "Item1BeforeItem2");

    private PairVocabulary(string atLeast, string above, string atMost, string below, string ordered, string strictlyOrdered) {
        AtLeast         = atLeast;
        Above           = above;
        AtMost          = atMost;
        Below           = below;
        Ordered         = ordered;
        StrictlyOrdered = strictlyOrdered;
    }

    /// <summary>The bound keeping a value at or above another.</summary>
    internal string AtLeast { get; }

    /// <summary>The bound keeping a value strictly above another.</summary>
    internal string Above { get; }

    /// <summary>The bound keeping a value at or below another.</summary>
    internal string AtMost { get; }

    /// <summary>The bound keeping a value strictly below another.</summary>
    internal string Below { get; }

    /// <summary>The relation putting <c>Item1</c> at or below <c>Item2</c>.</summary>
    internal string Ordered { get; }

    /// <summary>The relation putting <c>Item1</c> strictly below <c>Item2</c>.</summary>
    internal string StrictlyOrdered { get; }

    /// <summary>The vocabulary of the builder named <paramref name="builder" /> — <c>AnyDateOnly</c>, <c>AnyInt32</c>.</summary>
    internal static PairVocabulary For(string builder) {
        return builder is "AnyDateTime" or "AnyDateTimeOffset" or "AnyDateOnly" ? Chronology : Comparison;
    }

    /// <summary>The bound that keeps the second side on the right of a pinned first.</summary>
    internal string From(bool strict) {
        return strict ? Above : AtLeast;
    }

    /// <summary>The bound that keeps the first side on the right of a pinned second.</summary>
    internal string UpTo(bool strict) {
        return strict ? Below : AtMost;
    }

    /// <summary>The relation that orders two drawn sides.</summary>
    internal string Relation(bool strict) {
        return strict ? StrictlyOrdered : Ordered;
    }

}
