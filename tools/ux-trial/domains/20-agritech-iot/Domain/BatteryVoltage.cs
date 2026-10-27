namespace AgriTech.Domain;

/// <summary>A Li-ion cell voltage, transmitted as a half-precision float to save radio airtime.</summary>
public sealed class BatteryVoltage {

    public static readonly Half Minimum = (Half)2.5f;
    public static readonly Half Maximum = (Half)4.2f;
    public static readonly Half LowThreshold = (Half)3.3f;

    public BatteryVoltage(Half volts) {
        if (Half.IsNaN(volts) || volts < Minimum || volts > Maximum) {
            throw new ArgumentOutOfRangeException(nameof(volts), volts, "A cell voltage lies between 2.5 V and 4.2 V.");
        }
        Volts = volts;
    }

    public Half Volts { get; }

    public bool IsLow => Volts < LowThreshold;

}
