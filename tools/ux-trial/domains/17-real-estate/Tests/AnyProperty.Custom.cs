using RealEstate.Domain;

namespace RealEstate.Tests;

/// <summary>Two document links are the same attachment when they point at the same location.</summary>
public sealed class DocumentLocationComparer : IEqualityComparer<DocumentLink> {

    public static readonly DocumentLocationComparer Instance = new DocumentLocationComparer();

    public bool Equals(DocumentLink? x, DocumentLink? y) {
        return ReferenceEquals(x, y) || (x is not null && y is not null && x.Location == y.Location);
    }

    public int GetHashCode(DocumentLink obj) {
        return obj.Location.GetHashCode();
    }
}
