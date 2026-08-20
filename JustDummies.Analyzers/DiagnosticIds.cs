using JustDummies.Diagnostics;

namespace JustDummies.Analyzers;

/// <summary>
///     Stable identifiers for every JustDummies diagnostic. <c>JD</c> is the JustDummies prefix, mirroring
///     <c>FCE</c> for FirstClassErrors; the number is only a stable handle.
/// </summary>
internal static class DiagnosticIds {

    // Category: Reproducibility
    public const string AsyncBodyPassedToReproducibly = JustDummiesRule.JD001.Id;
    public const string DiscardedReproduciblyAsyncResult = JustDummiesRule.JD002.Id;
    public const string AwaitableBodyPassedToReproducibly = JustDummiesRule.JD003.Id;
    public const string DiscardedSeedingResult = JustDummiesRule.JD004.Id;

    // Category: Usage
    public const string GeneratorRenderedAsText = JustDummiesRule.JD005.Id;
    public const string DiscardedGeneratorResult = JustDummiesRule.JD006.Id;

    // Category: Reproducibility — draws that escape the pinned seed scope
    public const string DrawOutsideThePinnedScope = JustDummiesRule.JD007.Id;
    public const string ArbitraryValueInTheoryData = JustDummiesRule.JD008.Id;
    public const string DrawInStaticInitializer = JustDummiesRule.JD009.Id;
    public const string ReproducibleOnNonTestMethod = JustDummiesRule.JD010.Id;

    // Category: Usage — a recipe reaching a position that wanted the value
    public const string GeneratorWhereValueExpected = JustDummiesRule.JD011.Id;
    public const string GeneratorPooledAsValue = JustDummiesRule.JD012.Id;
    public const string HeldCollectionPassedToOneOf = JustDummiesRule.JD013.Id;

    // Category: Constraints — decidable from compile-time constants
    public const string RejectedConstantArgument = JustDummiesRule.JD014.Id;
    public const string StringConstraintsAdmitNoValue = JustDummiesRule.JD015.Id;
    public const string CollectionConstraintsAdmitNoValue = JustDummiesRule.JD016.Id;
    public const string EnumUniverseViolation = JustDummiesRule.JD017.Id;

    // Category: Reproducibility — the seeding long tail
    public const string NestedReproducibilityScope = JustDummiesRule.JD018.Id;
    public const string CommittedReplaySeed = JustDummiesRule.JD019.Id;
    public const string SharedStaticAnyContext = JustDummiesRule.JD020.Id;
    public const string BlankReplaySnippet = JustDummiesRule.JD021.Id;
    public const string ParallelDrawWithoutPerItemSeed = JustDummiesRule.JD022.Id;

    public const string ScalarChainAdmitsNoValue = JustDummiesRule.JD023.Id;
    public const string ConstraintWithNoEffect = JustDummiesRule.JD024.Id;

    public const string DuplicatePoolValue = JustDummiesRule.JD025.Id;
    public const string EmptyRelativeUri = JustDummiesRule.JD026.Id;
    public const string PooledValueNeverDraws = JustDummiesRule.JD029.Id;

    /// <summary>JD030 — a string dummy that declares no length.</summary>
    public const string UndeclaredStringLength = JustDummiesRule.JD030.Id;

    /// <summary>JD031 — both inclusive bounds declared separately, where the generator names the range.</summary>
    public const string PairedBoundsHaveARangeForm = JustDummiesRule.JD031.Id;

    /// <summary>JD032 — the same bound declared twice on one chain; only the tighter one survives.</summary>
    public const string BoundDeclaredTwice = JustDummiesRule.JD032.Id;

    // Category: Composition — a part that reaches no result, a constraint that cannot bind
    public const string UnusedCombineOperand = JustDummiesRule.JD027.Id;
    public const string InertDistinctness = JustDummiesRule.JD028.Id;

}
