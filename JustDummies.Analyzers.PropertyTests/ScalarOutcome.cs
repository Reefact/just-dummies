namespace JustDummies.Analyzers.PropertyTests;

/// <summary>What the run time did with a <see cref="ScalarChain" />.</summary>
internal enum ScalarVerdict {

    /// <summary>Every declaration was applied; the generator stands.</summary>
    Built,

    /// <summary>A declaration left no value, and was refused naming both sides — what JD023 reports.</summary>
    AdmitsNoValue,

    /// <summary>A declaration was refused on its own argument, or as already defined: JD014's or JD034's, which JD023 and JD038 leave alone.</summary>
    OwnedByAnotherRule

}

/// <summary>The run time's verdict on a <see cref="ScalarChain" />, with the generator's own count of its values when it was built.</summary>
internal sealed class ScalarOutcome {

    public ScalarOutcome(ScalarVerdict verdict, long? cardinality) {
        Verdict     = verdict;
        Cardinality = cardinality;
    }

    public ScalarVerdict Verdict { get; }

    /// <summary>How many distinct values the built generator can produce, where it can count them.</summary>
    public long? Cardinality { get; }

}
