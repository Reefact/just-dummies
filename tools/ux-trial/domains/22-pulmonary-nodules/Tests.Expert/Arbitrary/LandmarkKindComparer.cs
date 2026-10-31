using System.Collections.Generic;

using PulmonaryNodules.Domain;

namespace PulmonaryNodules.Tests.Expert.Arbitrary;

/// <summary>Two landmarks are the same fiducial when they locate the same anatomical structure.</summary>
public sealed class LandmarkKindComparer : IEqualityComparer<AnatomicalLandmark> {

    public static LandmarkKindComparer Instance { get; } = new LandmarkKindComparer();

    public bool Equals(AnatomicalLandmark? x, AnatomicalLandmark? y) {
        return x?.Kind == y?.Kind;
    }

    public int GetHashCode(AnatomicalLandmark obj) {
        return obj.Kind.GetHashCode();
    }

}
