namespace Ambig.Domain;

public sealed class Meter {
    public Meter(string serial) { Serial = serial; }
    public string Serial { get; }
}

public sealed class Measurement {
    public Measurement(double value) { Value = value; }
    public double Value { get; }
}

public sealed class Claim {
    public Claim(string reference) { Reference = reference; }
    public string Reference { get; }
}

public sealed class Scope {
    public Scope(string name) { Name = name; }
    public string Name { get; }
}
