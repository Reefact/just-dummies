namespace Repro.Domain;

public sealed class Sku {
    public Sku(string code) {
        if (string.IsNullOrWhiteSpace(code)) {
            throw new ArgumentException("Code is required.", nameof(code));
        }
        Code = code;
    }

    public string Code { get; }
}
