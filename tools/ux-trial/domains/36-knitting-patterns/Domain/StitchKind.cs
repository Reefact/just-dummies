namespace KnittingPatterns.Domain;

/// <summary>A single stitch symbol of a chart or written pattern.</summary>
public enum StitchKind
{
    /// <summary>Knit: consumes one stitch, produces one.</summary>
    Knit,
    /// <summary>Purl: consumes one stitch, produces one.</summary>
    Purl,
    /// <summary>Slip one purlwise: consumes one stitch, produces one.</summary>
    Slip,
    /// <summary>Yarn over: an increase that consumes nothing and produces one stitch.</summary>
    YarnOver,
    /// <summary>Knit front and back: an increase that consumes one stitch and produces two.</summary>
    KnitFrontAndBack,
    /// <summary>Knit two together: a right-leaning decrease that consumes two stitches and produces one.</summary>
    KnitTwoTogether,
    /// <summary>Slip, slip, knit: a left-leaning decrease that consumes two stitches and produces one.</summary>
    SlipSlipKnit,
    /// <summary>Central double decrease: consumes three stitches and produces one.</summary>
    CentralDoubleDecrease,
}

/// <summary>Stitch arithmetic of each <see cref="StitchKind"/>.</summary>
public static class StitchKindExtensions
{
    /// <summary>Number of live stitches taken from the left needle.</summary>
    public static int Consumes(this StitchKind kind) => kind switch
    {
        StitchKind.YarnOver => 0,
        StitchKind.KnitTwoTogether or StitchKind.SlipSlipKnit => 2,
        StitchKind.CentralDoubleDecrease => 3,
        _ => 1,
    };

    /// <summary>Number of stitches put on the right needle.</summary>
    public static int Produces(this StitchKind kind) => kind == StitchKind.KnitFrontAndBack ? 2 : 1;
}
