namespace RadiocarbonChronology.Domain;

/// <summary>Raised when the stratigraphy contradicts itself (a cycle) or contradicts the dates (no year left for a layer).</summary>
public sealed class StratigraphicInconsistencyException : Exception
{
    public StratigraphicInconsistencyException(string olderLayerCode, string youngerLayerCode, string message)
        : base(message)
    {
        OlderLayerCode = olderLayerCode;
        YoungerLayerCode = youngerLayerCode;
    }

    public string OlderLayerCode { get; }

    public string YoungerLayerCode { get; }
}
