namespace InternationalCatalogue.Domain;

/// <summary>The writing systems the catalogue records titles in.</summary>
public enum WritingScript
{
    Latin,
    Greek,
    Cyrillic,
    Arabic,
    Hebrew,
    Devanagari,

    /// <summary>Han ideographs, Japanese kana and Korean Hangul.</summary>
    Cjk,
}

public enum ContributorRole
{
    Author,
    Editor,
    Translator,
    Illustrator,
    Compiler,
}

/// <summary>Why the catalogue refuses a record as a duplicate; <see cref="None"/> when it is accepted.</summary>
public enum DuplicateReason
{
    None,
    SameRecordId,
    RetiredRecordId,
    SameIsbn,
    SameIssn,
    SameWork,
}
