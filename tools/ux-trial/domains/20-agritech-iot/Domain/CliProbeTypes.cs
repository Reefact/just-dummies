namespace AgriTech.Domain;

// Guard shapes probed for the `any` scaffolder. Not part of the field-sensor model.

/// <summary>Longitude half-open guard, without the NaN check.</summary>
public sealed class HalfOpenLongitude {
    public HalfOpenLongitude(double value) {
        if (value < -180.0) {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        if (value >= 180.0) {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        Value = value;
    }
    public double Value { get; }
}

/// <summary>Same guard as helpers: ThrowIfLessThan / ThrowIfGreaterThanOrEqual.</summary>
public sealed class HelperLongitude {
    public HelperLongitude(double value) {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, -180.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, 180.0);
        Value = value;
    }
    public double Value { get; }
}

/// <summary>Latitude by magnitude: Math.Abs.</summary>
public sealed class AbsLatitude {
    public AbsLatitude(double value) {
        if (Math.Abs(value) > 90.0) {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        Value = value;
    }
    public double Value { get; }
}

/// <summary>Altitude: finite check first, then a bound.</summary>
public sealed class Altitude {
    public Altitude(double metres) {
        if (!double.IsFinite(metres)) {
            throw new ArgumentException("finite", nameof(metres));
        }
        ArgumentOutOfRangeException.ThrowIfLessThan(metres, -430.0);
        Metres = metres;
    }
    public double Metres { get; }
}

/// <summary>Half with plain comparisons against literals.</summary>
public sealed class HalfReading {
    public HalfReading(Half value) {
        if (value < (Half)0) {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        if (value > (Half)100) {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        Value = value;
    }
    public Half Value { get; }
}

/// <summary>A non-zero signed double: -0.0 compares equal to 0.0.</summary>
public sealed class Heading {
    public Heading(double degrees) {
        if (degrees == 0.0) {
            throw new ArgumentOutOfRangeException(nameof(degrees));
        }
        ArgumentOutOfRangeException.ThrowIfGreaterThan(degrees, 180.0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(degrees, -180.0);
        Degrees = degrees;
    }
    public double Degrees { get; }
}

/// <summary>A float sample array with an upper count bound and non-empty.</summary>
public sealed class Waveform {
    public const int MaxSamples = 1_000_000;
    public Waveform(float[] samples) {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentOutOfRangeException.ThrowIfZero(samples.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(samples.Length, MaxSamples);
        Samples = samples;
    }
    public float[] Samples { get; }
}

/// <summary>A collection with more than the cap allowed.</summary>
public sealed class Archive {
    public Archive(IReadOnlyCollection<double> values) {
        if (values.Count > 2_000_000) {
            throw new ArgumentException("too many", nameof(values));
        }
        Values = values;
    }
    public IReadOnlyCollection<double> Values { get; }
}
