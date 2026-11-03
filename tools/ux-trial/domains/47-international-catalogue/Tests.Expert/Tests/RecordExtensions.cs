namespace InternationalCatalogue.Domain.Tests;

internal static class RecordExtensions {

    /// <summary>
    ///     A record that describes the same edition as <paramref name="record" /> — same title, subtitle, contributors,
    ///     language, edition and year — under a new id, with no ISBN. A test then changes the one thing it is about.
    /// </summary>
    public static AnyBibliographicRecord DescribingTheSameWorkAs(this AnyBibliographicRecord generator, BibliographicRecord record) {
        return generator.WithTitle(record.Title)
                        .WithSubtitle(record.Subtitle)
                        .WithContributors(record.Contributors)
                        .WithLanguage(record.Language)
                        .WithScript(record.Script)
                        .WithEdition(record.Edition)
                        .WithPublicationYear(record.PublicationYear)
                        .WithoutIsbn();
    }

}
