namespace JustDummies.Analyzers;

/// <summary>The temporal types whose constants <see cref="TemporalFacts" /> reads.</summary>
internal enum TemporalKind {

    DateOnly,
    DateTime,
    DateTimeOffset,
    TimeOnly,
    TimeSpan

}
