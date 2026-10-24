namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     The calls a string chain may declare only once — a second, different one is refused by the run time as
///     "already defined", a conflict of declaration rather than of values, which JD015 does not claim to report.
///     The generator keeps each slot to one call so the comparison stays within the rule's stated scope.
/// </summary>
internal enum StringChainSlot {

    /// <summary>No slot: a bound, a contained value, <c>NonEmpty()</c>, <c>NotBlank()</c>, a subtraction.</summary>
    None,

    /// <summary><c>WithLength(...)</c>.</summary>
    ExactLength,

    /// <summary><c>StartingWith(...)</c>.</summary>
    Prefix,

    /// <summary><c>EndingWith(...)</c>.</summary>
    Suffix,

    /// <summary>A named family or a <c>WithChars(...)</c> pool, which share the one family slot.</summary>
    Family,

    /// <summary><c>InUpperCase()</c> or <c>InLowerCase()</c>.</summary>
    Casing,

    /// <summary><c>OneOf(...)</c>.</summary>
    ValueSet,

    /// <summary><c>Except(...)</c> or <c>DifferentFrom(...)</c>; not a slot, but meaningful only beside a value set.</summary>
    Exclusion

}
