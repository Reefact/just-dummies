namespace PulmonaryNodules.Tests.Discovery;

/// <summary>The exams of a follow-up: three landmarks the patient's position moves as a whole, or not.</summary>
internal static class Landmarks {

    private static readonly (LandmarkKind Kind, double X, double Y, double Z)[] Reference = [
        (LandmarkKind.CarinaBifurcation, 10, 20, 30),
        (LandmarkKind.AorticArchApex, -50, 40, -10),
        (LandmarkKind.SternalAngle, 80, -60, 100),
    ];

    /// <summary>The three reference landmarks, each displaced by its own shift.</summary>
    public static AnatomicalLandmark[] Displaced(params (double Dx, double Dy, double Dz)[] shifts) {
        AnatomicalLandmark[] landmarks = new AnatomicalLandmark[Reference.Length];
        for (int i = 0; i < Reference.Length; i++) {
            (double dx, double dy, double dz) = i < shifts.Length ? shifts[i] : (0.0, 0.0, 0.0);
            landmarks[i] = new AnatomicalLandmark(Reference[i].Kind, new SpatialPoint(Reference[i].X + dx, Reference[i].Y + dy, Reference[i].Z + dz));
        }

        return landmarks;
    }

    /// <summary>The three reference landmarks, the whole patient having moved by the same shift.</summary>
    public static AnatomicalLandmark[] Shifted(double dx, double dy, double dz) {
        return Displaced((dx, dy, dz), (dx, dy, dz), (dx, dy, dz));
    }

}
