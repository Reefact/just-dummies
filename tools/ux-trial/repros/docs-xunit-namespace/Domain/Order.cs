namespace Repro.Domain;

public sealed class Order {
    public Order(string customerName) { CustomerName = customerName; }
    public string CustomerName { get; }
}
