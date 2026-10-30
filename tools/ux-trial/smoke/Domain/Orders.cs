namespace Smoke.Domain;

// A small domain whose every guard the scaffolder reads, so a freshly scaffolded generator compiles as it
// stands: two value objects and an aggregate built from them. The release's UX trial scaffolds these with the
// packed `any`, then compiles and runs the tests beside them.

public sealed class Sku {

    public Sku(string code) {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (code.Length > 12) { throw new ArgumentOutOfRangeException(nameof(code)); }

        Code = code;
    }

    public string Code { get; }

}

public sealed class Quantity {

    public Quantity(int value) {
        if (value <= 0) { throw new ArgumentOutOfRangeException(nameof(value)); }
        if (value > 999) { throw new ArgumentOutOfRangeException(nameof(value)); }

        Value = value;
    }

    public int Value { get; }

}

public sealed class OrderLine {

    public OrderLine(Sku sku, Quantity quantity) {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(quantity);

        Sku      = sku;
        Quantity = quantity;
    }

    public Sku      Sku      { get; }
    public Quantity Quantity { get; }

}
