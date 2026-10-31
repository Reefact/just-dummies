namespace KnittingPatterns.Domain;

/// <summary>Stitches and rows over a 10 cm square of the swatch.</summary>
public sealed class Gauge
{
    public Gauge(decimal stitchesPer10Cm, decimal rowsPer10Cm)
    {
        if (stitchesPer10Cm < 4m || stitchesPer10Cm > 60m)
        {
            throw new ArgumentOutOfRangeException(nameof(stitchesPer10Cm), stitchesPer10Cm, "Stitch gauge lies between 4 and 60 stitches per 10 cm.");
        }

        if (rowsPer10Cm < 4m || rowsPer10Cm > 80m)
        {
            throw new ArgumentOutOfRangeException(nameof(rowsPer10Cm), rowsPer10Cm, "Row gauge lies between 4 and 80 rows per 10 cm.");
        }

        // A knit stitch is wider than it is tall: rows per 10 cm stay within half to twice the stitches.
        if (rowsPer10Cm < stitchesPer10Cm / 2m || rowsPer10Cm > stitchesPer10Cm * 2m)
        {
            throw new ArgumentException(
                $"A gauge of {stitchesPer10Cm} sts and {rowsPer10Cm} rows per 10 cm is not a plausible fabric.",
                nameof(rowsPer10Cm));
        }

        StitchesPer10Cm = stitchesPer10Cm;
        RowsPer10Cm = rowsPer10Cm;
    }

    public decimal StitchesPer10Cm { get; }

    public decimal RowsPer10Cm { get; }

    /// <summary>Stitches to cast on for a width, rounded to the nearest whole stitch.</summary>
    public int StitchesForWidth(decimal widthCm)
    {
        if (widthCm <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(widthCm), widthCm, "A width is positive.");
        }

        return Math.Max(1, (int)Math.Round(widthCm * StitchesPer10Cm / 10m, MidpointRounding.AwayFromZero));
    }

    /// <summary>Rows to work for a length, rounded to the nearest whole row.</summary>
    public int RowsForLength(decimal lengthCm)
    {
        if (lengthCm <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(lengthCm), lengthCm, "A length is positive.");
        }

        return Math.Max(1, (int)Math.Round(lengthCm * RowsPer10Cm / 10m, MidpointRounding.AwayFromZero));
    }

    /// <summary>Width in centimetres of a given number of stitches.</summary>
    public decimal WidthOf(int stitches)
    {
        if (stitches < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(stitches), stitches, "At least one stitch.");
        }

        return stitches * 10m / StitchesPer10Cm;
    }
}
