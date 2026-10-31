namespace KnittingPatterns.Domain;

/// <summary>Something written inside a row instruction: a stitch run or a bracketed repeat.</summary>
public interface IRowElement
{
    /// <summary>Live stitches this element consumes.</summary>
    int Consumes { get; }

    /// <summary>Stitches this element produces.</summary>
    int Produces { get; }

    /// <summary>Number of single-stitch operations once fully expanded.</summary>
    int OperationCount { get; }

    /// <summary>The single-stitch operations, in working order.</summary>
    IEnumerable<StitchKind> Expand();
}
