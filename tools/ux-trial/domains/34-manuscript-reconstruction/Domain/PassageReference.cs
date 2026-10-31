using System.Globalization;

namespace ManuscriptReconstruction.Domain;

/// <summary>A canonical locus in the work: book, chapter and line, written "book.chapter.line".</summary>
public sealed class PassageReference : IEquatable<PassageReference>, IComparable<PassageReference>
{
    public const int MaxBook = 99;
    public const int MaxChapter = 999;
    public const int MaxLine = 9999;

    public PassageReference(int book, int chapter, int line)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(book, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(book, MaxBook);
        ArgumentOutOfRangeException.ThrowIfLessThan(chapter, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(chapter, MaxChapter);
        ArgumentOutOfRangeException.ThrowIfLessThan(line, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(line, MaxLine);
        Book = book;
        Chapter = chapter;
        Line = line;
    }

    public int Book { get; }

    public int Chapter { get; }

    public int Line { get; }

    public static PassageReference Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string[] parts = text.Split('.');
        if (parts.Length != 3
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int book)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int chapter)
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out int line))
        {
            throw new FormatException($"'{text}' is not a passage reference of the form book.chapter.line.");
        }

        return new PassageReference(book, chapter, line);
    }

    public int CompareTo(PassageReference? other)
    {
        if (other is null)
        {
            return 1;
        }

        int byBook = Book.CompareTo(other.Book);
        if (byBook != 0)
        {
            return byBook;
        }

        int byChapter = Chapter.CompareTo(other.Chapter);
        return byChapter != 0 ? byChapter : Line.CompareTo(other.Line);
    }

    public bool Equals(PassageReference? other) =>
        other is not null && Book == other.Book && Chapter == other.Chapter && Line == other.Line;

    public override bool Equals(object? obj) => Equals(obj as PassageReference);

    public override int GetHashCode() => HashCode.Combine(Book, Chapter, Line);

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Book}.{Chapter}.{Line}");
}
