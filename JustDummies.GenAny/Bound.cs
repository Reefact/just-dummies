namespace JustDummies.GenAny;

/// <summary>
///     What a guard-derived constraint pins down, so that two of them can be told apart from two of the same.
/// </summary>
internal enum Bound {

    /// <summary>Whether the value may be empty at all — <c>NonEmpty</c>.</summary>
    Emptiness = 0,

    /// <summary>A floor — <c>GreaterThanOrEqualTo</c>, <c>WithMinLength</c>, <c>WithMinCount</c>.</summary>
    Lower = 1,

    /// <summary>A ceiling — <c>LessThanOrEqualTo</c>, <c>WithMaxLength</c>, <c>WithMaxCount</c>.</summary>
    Upper = 2,

    /// <summary>An exact size — <c>WithLength</c>, <c>WithCount</c>.</summary>
    Exact = 3,

    /// <summary>Which side of zero — <c>Positive</c>, <c>Negative</c>.</summary>
    Sign = 4,

    /// <summary>Zero itself — <c>NonZero</c>.</summary>
    Zero = 5

}
