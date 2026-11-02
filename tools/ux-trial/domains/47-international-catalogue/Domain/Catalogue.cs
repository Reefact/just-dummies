using System.Text;

namespace InternationalCatalogue.Domain;

/// <summary>
/// The union catalogue: the records the contributing agencies hold, kept free of duplicates. Records absorbed by
/// a merge or withdrawn leave their id retired: it still resolves (to the survivor) but is never reused.
/// </summary>
public sealed class Catalogue
{
    public const int MaxNameLength = 120;

    private readonly Dictionary<RecordId, BibliographicRecord> _records = [];

    /// <summary>Retired ids: an absorbed record points to its survivor, a withdrawn one to nothing.</summary>
    private readonly Dictionary<RecordId, RecordId?> _retired = [];

    public Catalogue(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        string text = UnicodeText.ToNfc(name, nameof(name)).Trim();
        if (text.Length == 0)
        {
            throw new ArgumentException("A catalogue needs a name.", nameof(name));
        }

        if (text.EnumerateRunes().Any(UnicodeText.IsLineBreakOrControl))
        {
            throw new ArgumentException("A catalogue name cannot contain control or line-break characters.", nameof(name));
        }

        int length = UnicodeText.GraphemeCount(text);
        if (length > MaxNameLength)
        {
            throw new ArgumentOutOfRangeException(nameof(name), length, $"A catalogue name has at most {MaxNameLength} grapheme clusters.");
        }

        Name = text;
    }

    public string Name { get; }

    public int Count => _records.Count;

    public IReadOnlyList<BibliographicRecord> Records() => _records.Values.ToList();

    /// <summary>Why <see cref="Add"/> would refuse the record; <see cref="DuplicateReason.None"/> when it would accept it.</summary>
    public DuplicateReason CheckDuplicate(BibliographicRecord record) => FindDuplicate(record).Reason;

    public void Add(BibliographicRecord record)
    {
        (DuplicateReason reason, RecordId? existing) = FindDuplicate(record);
        if (reason != DuplicateReason.None)
        {
            throw new DuplicateRecordException(reason, existing!, $"Record {record.Id} is refused ({reason}): it duplicates {existing}.");
        }

        _records.Add(record.Id, record);
    }

    /// <summary>The current record for an id, following merges; <see langword="null"/> when unknown or withdrawn.</summary>
    public BibliographicRecord? Find(RecordId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        RecordId? current = Resolve(id);
        return current is not null && _records.TryGetValue(current, out BibliographicRecord? record) ? record : null;
    }

    public BibliographicRecord? FindByIsbn(Isbn13 isbn)
    {
        ArgumentNullException.ThrowIfNull(isbn);
        return _records.Values.FirstOrDefault(r => isbn.Equals(r.Isbn));
    }

    public BibliographicRecord? FindByIsbn(Isbn10 isbn)
    {
        ArgumentNullException.ThrowIfNull(isbn);
        return FindByIsbn(isbn.ToIsbn13());
    }

    /// <summary>The records whose title equals <paramref name="title"/> under NFC and case folding.</summary>
    public IReadOnlyList<BibliographicRecord> FindByTitle(Title title)
    {
        ArgumentNullException.ThrowIfNull(title);
        return _records.Values.Where(r => r.Title.Equals(title)).ToList();
    }

    /// <summary>
    /// Folds the absorbed record into the survivor. Both must be current records describing the same edition:
    /// equal title, same language, script, edition and publication year. The survivor keeps its own values and
    /// takes from the absorbed record what it lacks (subtitle, ISBN, ISSN, call number) and the contributors it
    /// does not list yet. The absorbed id is retired and resolves to the survivor.
    /// </summary>
    public BibliographicRecord Merge(RecordId survivorId, RecordId absorbedId)
    {
        ArgumentNullException.ThrowIfNull(survivorId);
        ArgumentNullException.ThrowIfNull(absorbedId);
        if (survivorId.Equals(absorbedId))
        {
            throw new ArgumentException("A record cannot be merged into itself.", nameof(absorbedId));
        }

        BibliographicRecord survivor = Current(survivorId);
        BibliographicRecord absorbed = Current(absorbedId);
        string? mismatch =
            !survivor.Title.Equals(absorbed.Title) ? "titles differ"
            : !survivor.Language.Equals(absorbed.Language) ? "languages differ"
            : survivor.Script != absorbed.Script ? "scripts differ"
            : survivor.Edition != absorbed.Edition ? "editions differ"
            : survivor.PublicationYear != absorbed.PublicationYear ? "publication years differ"
            : survivor.Isbn is not null && absorbed.Isbn is not null && !survivor.Isbn.Equals(absorbed.Isbn) ? "ISBNs conflict"
            : survivor.Issn is not null && absorbed.Issn is not null && !survivor.Issn.Equals(absorbed.Issn) ? "ISSNs conflict"
            : null;
        if (mismatch is not null)
        {
            throw new InvalidOperationException($"Cannot merge {absorbedId} into {survivorId}: {mismatch}.");
        }

        List<Contributor> contributors = survivor.Contributors.ToList();
        contributors.AddRange(absorbed.Contributors.Where(c => !contributors.Contains(c)));
        if (contributors.Count > BibliographicRecord.MaxContributors)
        {
            throw new InvalidOperationException($"Cannot merge {absorbedId} into {survivorId}: {contributors.Count} contributors exceed {BibliographicRecord.MaxContributors}.");
        }

        BibliographicRecord merged = new(
            survivor.Id,
            survivor.Title,
            survivor.Subtitle ?? absorbed.Subtitle,
            contributors,
            survivor.Language,
            survivor.Script,
            survivor.Edition,
            survivor.PublicationYear,
            survivor.Isbn ?? absorbed.Isbn,
            survivor.Issn ?? absorbed.Issn,
            survivor.CallNumber ?? absorbed.CallNumber);
        _records[survivor.Id] = merged;
        _records.Remove(absorbed.Id);
        _retired[absorbed.Id] = survivor.Id;
        return merged;
    }

    /// <summary>Removes a current record; its id is retired and never reused.</summary>
    public void Withdraw(RecordId id)
    {
        BibliographicRecord record = Current(id);
        _records.Remove(record.Id);
        _retired[record.Id] = null;
    }

    /// <summary>The records sorted by the collation, optionally only those in one script.</summary>
    public IReadOnlyList<BibliographicRecord> Listing(ListingCollation collation, WritingScript? script = null)
    {
        ArgumentNullException.ThrowIfNull(collation);
        if (script is not null && !Enum.IsDefined(script.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(script), script, "Unknown writing script.");
        }

        List<BibliographicRecord> listing = _records.Values.Where(r => script is null || r.Script == script).ToList();
        listing.Sort(collation);
        return listing;
    }

    /// <summary>One line per record in listing order: filing title, then the heading of the first contributor.</summary>
    public string RenderListing(ListingCollation collation, WritingScript? script = null)
    {
        StringBuilder builder = new();
        foreach (BibliographicRecord record in Listing(collation, script))
        {
            builder.Append(collation.FilingTitle(record));
            if (record.Contributors.Count > 0)
            {
                builder.Append(" / ").Append(record.Contributors[0].Name.Heading);
            }

            builder.Append(" (").Append(record.PublicationYear).Append(')').Append('\n');
        }

        return builder.ToString();
    }

    private (DuplicateReason Reason, RecordId? Existing) FindDuplicate(BibliographicRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (_records.ContainsKey(record.Id))
        {
            return (DuplicateReason.SameRecordId, record.Id);
        }

        if (_retired.ContainsKey(record.Id))
        {
            return (DuplicateReason.RetiredRecordId, Resolve(record.Id) ?? record.Id);
        }

        if (record.Isbn is not null && FindByIsbn(record.Isbn) is BibliographicRecord sameIsbn)
        {
            return (DuplicateReason.SameIsbn, sameIsbn.Id);
        }

        if (record.IsSerial && _records.Values.FirstOrDefault(r => r.IsSerial && r.Issn!.Equals(record.Issn)) is BibliographicRecord sameIssn)
        {
            return (DuplicateReason.SameIssn, sameIssn.Id);
        }

        BibliographicRecord? sameWork = _records.Values.FirstOrDefault(r => r.DescribesSameWorkAs(record));
        return sameWork is null ? (DuplicateReason.None, null) : (DuplicateReason.SameWork, sameWork.Id);
    }

    private BibliographicRecord Current(RecordId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (_records.TryGetValue(id, out BibliographicRecord? record))
        {
            return record;
        }

        throw new InvalidOperationException(_retired.ContainsKey(id)
            ? $"Record {id} is retired; use {Resolve(id)?.ToString() ?? "no record (withdrawn)"}."
            : $"Record {id} is not in the catalogue.");
    }

    /// <summary>Follows merge redirects to the current id; <see langword="null"/> for a withdrawn or unknown id.</summary>
    private RecordId? Resolve(RecordId id)
    {
        RecordId? current = id;
        while (current is not null && !_records.ContainsKey(current))
        {
            if (!_retired.TryGetValue(current, out current))
            {
                return null;
            }
        }

        return current;
    }
}
