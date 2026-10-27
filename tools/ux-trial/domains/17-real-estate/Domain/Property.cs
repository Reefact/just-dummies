namespace RealEstate.Domain;

/// <summary>A registered property: one parcel, one building, its DPE and its documents.</summary>
public sealed class Property {

    public const int MaxDocuments = 20;

    private readonly List<DocumentLink> _documents;

    public Property(CadastralReference parcel, PropertyKind kind, Surface livingArea, EnergyPerformanceDiagnosis diagnosis, IReadOnlyList<DocumentLink> documents) {
        ArgumentNullException.ThrowIfNull(parcel);
        ArgumentNullException.ThrowIfNull(livingArea);
        ArgumentNullException.ThrowIfNull(diagnosis);
        ArgumentNullException.ThrowIfNull(documents);
        _ = kind switch {
            PropertyKind.House or PropertyKind.Apartment or PropertyKind.Commercial => kind,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        if (documents.Count > MaxDocuments) {
            throw new ArgumentException($"At most {MaxDocuments} documents.", nameof(documents));
        }
        if (documents.Select(document => document.Location).Distinct().Count() != documents.Count) {
            throw new ArgumentException("A document is attached once.", nameof(documents));
        }
        Parcel = parcel;
        Kind = kind;
        LivingArea = livingArea;
        Diagnosis = diagnosis;
        _documents = documents.ToList();
    }

    public CadastralReference Parcel { get; }

    public PropertyKind Kind { get; }

    public Surface LivingArea { get; }

    public EnergyPerformanceDiagnosis Diagnosis { get; private set; }

    public IReadOnlyList<DocumentLink> Documents => _documents;

    public void Attach(DocumentLink document) {
        ArgumentNullException.ThrowIfNull(document);
        if (_documents.Count == MaxDocuments) {
            throw new InvalidOperationException("The document folder is full.");
        }
        if (_documents.Any(existing => existing.Location == document.Location)) {
            throw new InvalidOperationException("That document is already attached.");
        }
        _documents.Add(document);
    }

    public void Rediagnose(EnergyPerformanceDiagnosis diagnosis) {
        ArgumentNullException.ThrowIfNull(diagnosis);
        if (diagnosis.IssuedOn <= Diagnosis.IssuedOn) {
            throw new InvalidOperationException("A new diagnosis must be more recent than the current one.");
        }
        Diagnosis = diagnosis;
    }

    /// <summary>
    /// The Climate and Resilience Act calendar: a home rated G may not be newly let from 2025,
    /// F from 2028, E from 2034. Commercial premises are outside the rule. The diagnosis must be valid.
    /// </summary>
    public bool CanBeLetOn(DateOnly day) {
        if (!Diagnosis.IsValidOn(day)) {
            return false;
        }
        if (Kind == PropertyKind.Commercial) {
            return true;
        }
        DateOnly? bannedFrom = Diagnosis.Rating switch {
            EnergyRating.G => new DateOnly(2025, 1, 1),
            EnergyRating.F => new DateOnly(2028, 1, 1),
            EnergyRating.E => new DateOnly(2034, 1, 1),
            _ => null
        };
        return bannedFrom is null || day < bannedFrom.Value;
    }

    public decimal PricePerSquareMeter(decimal price) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);
        return Math.Round(price / (decimal)LivingArea.SquareMeters, 2, MidpointRounding.ToEven);
    }
}
