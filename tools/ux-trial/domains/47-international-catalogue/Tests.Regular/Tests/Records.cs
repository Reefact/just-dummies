using InternationalCatalogue.Domain;

using JustDummies;

namespace InternationalCatalogue.Tests;

/// <summary>Builders shared by the record and catalogue tests.</summary>
internal static class Records {

    /// <summary>A generator pinned on every value of <paramref name="record" />: override only what a test is about.</summary>
    public static AnyBibliographicRecord Like(BibliographicRecord record) {
        return Any.BibliographicRecord()
                  .WithId(record.Id)
                  .WithScript(record.Script)
                  .WithTitle(record.Title)
                  .WithSubtitle(record.Subtitle)
                  .WithContributors(record.Contributors)
                  .WithLanguage(record.Language)
                  .WithEdition(record.Edition)
                  .WithPublicationYear(record.PublicationYear)
                  .WithIsbn(record.Isbn)
                  .WithIssn(record.Issn)
                  .WithCallNumber(record.CallNumber);
    }

    /// <summary>The same edition of the same work as <paramref name="record" />, filed under another id and with nothing identifying it.</summary>
    public static AnyBibliographicRecord SameWorkAs(BibliographicRecord record) {
        return Like(record).WithId(Any.RecordId().DifferentFrom(record.Id).Generate());
    }

    /// <summary>A record that has an ISBN, hence a year from 2007 on.</summary>
    public static AnyBibliographicRecord WithIsbn() {
        return Any.BibliographicRecord().WithAnyIsbn();
    }

    /// <summary>A serial: an ISSN and no ISBN.</summary>
    public static AnyBibliographicRecord Serial() {
        return Any.BibliographicRecord().WithIssn(Any.Issn().Generate()).WithoutIsbn();
    }

    /// <summary>A record with neither identifier.</summary>
    public static AnyBibliographicRecord Unnumbered() {
        return Any.BibliographicRecord().WithoutIsbn().WithoutIssn();
    }

    public static Contributor Author(string family, string given) {
        return new Contributor(new PersonName(family, given), ContributorRole.Author);
    }

}
