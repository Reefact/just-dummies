using System.Text.RegularExpressions;

namespace TodoWording.Domain;

// (1) one unread regex guard among readable ones: which one is unread?
public sealed class AnalyteCode {
    public string Value { get; }
    public AnalyteCode(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 7) { throw new ArgumentOutOfRangeException(nameof(value)); }
        if (!Regex.IsMatch(value, @"^\d{1,5}-\d$")) { throw new ArgumentException("format", nameof(value)); }
        Value = value;
    }
}

// (2) cross-field guard reading two parameters, naming only one
public sealed class Diagnosis {
    public int Energy { get; }
    public int Emissions { get; }
    public int Declared { get; }
    public Diagnosis(int energy, int emissions, int declared) {
        ArgumentOutOfRangeException.ThrowIfNegative(energy);
        ArgumentOutOfRangeException.ThrowIfNegative(emissions);
        int computed = (energy + emissions) / 100;
        if (computed != declared) { throw new ArgumentException("rating", nameof(declared)); }
        Energy = energy; Emissions = emissions; Declared = declared;
    }
}

// (3) ordered pair
public sealed class Window {
    public int Start { get; }
    public int End { get; }
    public Window(int start, int end) {
        if (start >= end) { throw new ArgumentException("order", nameof(end)); }
        Start = start; End = end;
    }
}

// (2b) the same cross-field guard, inline rather than through a local
public sealed class DiagnosisInline {
    public int Declared { get; }
    public DiagnosisInline(int energy, int emissions, int declared) {
        ArgumentOutOfRangeException.ThrowIfNegative(energy);
        ArgumentOutOfRangeException.ThrowIfNegative(emissions);
        if ((energy + emissions) / 100 != declared) { throw new ArgumentException("rating", nameof(declared)); }
        Declared = declared;
    }
}
