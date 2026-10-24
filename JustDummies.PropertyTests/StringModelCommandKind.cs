namespace JustDummies.PropertyTests;

/// <summary>
///     What a <see cref="StringModelCommand" /> declares, as far as building and shrinking a chain is concerned: an
///     exclusion is only meaningful to the model beside a value set, so the two are told apart from the rest.
/// </summary>
internal enum StringModelCommandKind {

    /// <summary>A length, an anchor or a character constraint.</summary>
    Shape,

    /// <summary>A <c>OneOf(...)</c> value set.</summary>
    ValueSet,

    /// <summary>An <c>Except(...)</c> or a <c>DifferentFrom(...)</c>.</summary>
    Exclusion

}
