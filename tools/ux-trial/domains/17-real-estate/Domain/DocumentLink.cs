namespace RealEstate.Domain;

/// <summary>A document stored in the registry's document store: an absolute https URI.</summary>
public sealed class DocumentLink {

    public DocumentLink(DocumentKind kind, Uri location, DateTime uploadedAtUtc) {
        Validate(kind, location, uploadedAtUtc);
        Kind = kind;
        Location = location;
        UploadedAtUtc = uploadedAtUtc;
    }

    public DocumentKind Kind { get; }

    public Uri Location { get; }

    public DateTime UploadedAtUtc { get; }

    public string FileName => Location.Segments.Length > 1 ? Uri.UnescapeDataString(Location.Segments[^1]) : string.Empty;

    private static void Validate(DocumentKind kind, Uri location, DateTime uploadedAtUtc) {
        if (!Enum.IsDefined(kind)) {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }
        ArgumentNullException.ThrowIfNull(location);
        if (!location.IsAbsoluteUri) {
            throw new ArgumentException("A document link is absolute.", nameof(location));
        }
        if (location.Scheme != Uri.UriSchemeHttps) {
            throw new ArgumentException("Documents are only served over https.", nameof(location));
        }
        if (uploadedAtUtc.Kind != DateTimeKind.Utc) {
            throw new ArgumentException("Upload instants are recorded in UTC.", nameof(uploadedAtUtc));
        }
    }
}
