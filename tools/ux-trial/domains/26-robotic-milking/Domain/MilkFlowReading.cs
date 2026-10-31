namespace RoboticMilking.Domain;

/// <summary>One sample from a quarter's milk meter and sensors.</summary>
public sealed class MilkFlowReading
{
    public const decimal MaxFlowKgPerMinute = 8m;
    public const decimal MastitisConductivityThreshold = 7.5m;

    /// <param name="quarter">The quarter the sample comes from.</param>
    /// <param name="elapsed">Time since the session started; strictly positive.</param>
    /// <param name="flowKgPerMinute">Instantaneous flow, 0 to 8 kg/min.</param>
    /// <param name="conductivityMsPerCm">Electrical conductivity, 2 to 15 mS/cm.</param>
    /// <param name="temperatureCelsius">Milk temperature, 30 to 42 °C.</param>
    /// <param name="bloodDetected">Whether the colour sensor saw blood.</param>
    public MilkFlowReading(
        UdderQuarter quarter,
        TimeSpan elapsed,
        decimal flowKgPerMinute,
        decimal conductivityMsPerCm,
        decimal temperatureCelsius,
        bool bloodDetected)
    {
        if (!Enum.IsDefined(quarter))
        {
            throw new ArgumentOutOfRangeException(nameof(quarter), quarter, "Unknown udder quarter.");
        }

        if (elapsed <= TimeSpan.Zero || elapsed > TimeSpan.FromHours(1))
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed), elapsed, "A reading is taken within the first hour of a session.");
        }

        if (flowKgPerMinute < 0m || flowKgPerMinute > MaxFlowKgPerMinute)
        {
            throw new ArgumentOutOfRangeException(nameof(flowKgPerMinute), flowKgPerMinute, "Flow is 0 to 8 kg/min.");
        }

        if (conductivityMsPerCm < 2m || conductivityMsPerCm > 15m)
        {
            throw new ArgumentOutOfRangeException(nameof(conductivityMsPerCm), conductivityMsPerCm, "Conductivity is 2 to 15 mS/cm.");
        }

        if (temperatureCelsius < 30m || temperatureCelsius > 42m)
        {
            throw new ArgumentOutOfRangeException(nameof(temperatureCelsius), temperatureCelsius, "Milk temperature is 30 to 42 °C.");
        }

        Quarter = quarter;
        Elapsed = elapsed;
        FlowKgPerMinute = flowKgPerMinute;
        ConductivityMsPerCm = conductivityMsPerCm;
        TemperatureCelsius = temperatureCelsius;
        BloodDetected = bloodDetected;
    }

    public UdderQuarter Quarter { get; }

    public TimeSpan Elapsed { get; }

    public decimal FlowKgPerMinute { get; }

    public decimal ConductivityMsPerCm { get; }

    public decimal TemperatureCelsius { get; }

    public bool BloodDetected { get; }

    public bool SuggestsMastitis => ConductivityMsPerCm > MastitisConductivityThreshold;
}
