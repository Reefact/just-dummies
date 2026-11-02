namespace SmartGrid.Domain;

// Written by hand, beside the scaffolded file: it survives `any generate --force`.
public sealed partial class AnyMeterReading {

    /// <summary>Pins the index to a register value written as a number, so a test reads "100 kWh", not "new MeterIndex(100m)".</summary>
    public AnyMeterReading WithIndexOf(decimal kwh) {
        return WithIndex(new MeterIndex(kwh));
    }

}
