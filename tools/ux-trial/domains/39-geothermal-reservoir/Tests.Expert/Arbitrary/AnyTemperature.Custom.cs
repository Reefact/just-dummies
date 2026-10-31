using GeothermalReservoir.Domain;

namespace GeothermalReservoir.Tests.Expert.Arbitrary;

public sealed partial class AnyTemperature {

    /// <summary>Geologists think in degrees Celsius; the domain stores kelvin.</summary>
    public AnyTemperature BetweenCelsius(double lowest, double highest) {
        return WithKelvin(kelvin => kelvin.Between(lowest + Temperature.FreezingKelvin, highest + Temperature.FreezingKelvin));
    }

}
