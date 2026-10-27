namespace Repro.Domain;

public sealed class Money {
    public Money(decimal amount) {
        Amount = amount;
    }

    public decimal Amount { get; }
}
