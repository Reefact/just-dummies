namespace Repro.Domain;

public sealed class Code {
    public Code(string value) {
        throw new ArgumentException("always rejects", nameof(value));
    }
}
