namespace Drafting.Domain;

/// <summary>ISO 286-1 table 1: standard tolerance values IT6 to IT11, in micrometres, for nominal sizes up to 500 mm.</summary>
public static class StandardTolerance {

    public const double LargestNominalMillimetres = 500.0;

    private static readonly double[] UpperBounds = [3, 6, 10, 18, 30, 50, 80, 120, 180, 250, 315, 400, 500];

    private static readonly int[][] Micrometres = [
        [6, 8, 9, 11, 13, 16, 19, 22, 25, 29, 32, 36, 40],                   // IT6
        [10, 12, 15, 18, 21, 25, 30, 35, 40, 46, 52, 57, 63],                // IT7
        [14, 18, 22, 27, 33, 39, 46, 54, 63, 72, 81, 89, 97],                // IT8
        [25, 30, 36, 43, 52, 62, 74, 87, 100, 115, 130, 140, 155],           // IT9
        [40, 48, 58, 70, 84, 100, 120, 140, 160, 185, 210, 230, 250],        // IT10
        [60, 75, 90, 110, 130, 160, 190, 220, 250, 290, 320, 360, 400]       // IT11
    ];

    public static int InMicrometres(double nominalMillimetres, int grade) {
        if (nominalMillimetres <= 0 || nominalMillimetres > LargestNominalMillimetres) {
            throw new ArgumentOutOfRangeException(nameof(nominalMillimetres), nominalMillimetres, "ISO 286 tables are covered up to 500 mm here.");
        }

        int range = 0;
        while (nominalMillimetres > UpperBounds[range]) {
            range++;
        }

        return Micrometres[grade - IsoTolerance.FinestGrade][range];
    }

}
