using System.Text.RegularExpressions;

namespace GuardMiss.Domain;

// (a1) if-guard below a property assignment
public sealed class Tariff {
    public decimal Rate { get; }
    public string Currency { get; }
    public Tariff(decimal rate, string currency) {
        Rate = rate;
        if (currency.Length != 3) { throw new ArgumentException("ISO code", nameof(currency)); }
        Currency = currency;
    }
}

// (a2) recognised helper below a property assignment
public sealed class Slot {
    public int Aisle { get; }
    public int Rack { get; }
    public Slot(int aisle, int rack) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rack);
        Aisle = aisle;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(rack, 99);
        Rack = rack;
    }
}

// (b1) guard through a Match local
public sealed class LotCode {
    public string Value { get; }
    public LotCode(string value) {
        Match m = Regex.Match(value, "^LOT-[0-9]{4}$");
        if (!m.Success) { throw new ArgumentException("format", nameof(value)); }
        Value = value;
    }
}

// (b2) guard through a bool local
public sealed class LotPrefix {
    public string Value { get; }
    public LotPrefix(string value) {
        bool ok = value.StartsWith("LOT-", StringComparison.Ordinal);
        if (!ok) { throw new ArgumentException("prefix", nameof(value)); }
        Value = value;
    }
}

// (b3) int.Parse local
public sealed class Week {
    public string Value { get; }
    public Week(string week) {
        int parsed = int.Parse(week);
        if (parsed < 1 || parsed > 53) { throw new ArgumentOutOfRangeException(nameof(week)); }
        Value = week;
    }
}

// (b4) switch expression into a bool local
public sealed class Seat {
    public char Letter { get; }
    public Seat(char letter) {
        bool valid = letter switch { 'A' or 'B' or 'C' => true, _ => false };
        if (!valid) { throw new ArgumentOutOfRangeException(nameof(letter)); }
        Letter = letter;
    }
}

// (c) switch expression with throw arm, assigned to a property
public sealed class Level {
    public int Value { get; }
    public Level(int level) {
        Value = level switch { >= 1 and <= 20 => level, _ => throw new ArgumentOutOfRangeException(nameof(level)) };
    }
}

// (d) guard against a field copy of the parameter
public sealed class Role {
    private readonly string _name;
    public Role(string name) {
        _name = name;
        if (_name.Length > 5) { throw new ArgumentException("too long", nameof(name)); }
    }
    public string Name => _name;
}

// (e) throw expression inside a ternary
public sealed class Quantity {
    public int Value { get; }
    public Quantity(int value) {
        Value = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }
}

// control: (a-control) same Tariff guard placed above the assignment
public sealed class TariffControl {
    public decimal Rate { get; }
    public string Currency { get; }
    public TariffControl(decimal rate, string currency) {
        if (currency.Length != 3) { throw new ArgumentException("ISO code", nameof(currency)); }
        Rate = rate;
        Currency = currency;
    }
}
