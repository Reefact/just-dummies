namespace Shop.Domain;

/// <summary>One SKU, a quantity of it, and its unit price.</summary>
public sealed class OrderLine {

    public OrderLine(Sku sku, Quantity quantity, Money unitPrice) {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(quantity);
        ArgumentNullException.ThrowIfNull(unitPrice);

        Sku       = sku;
        Quantity  = quantity;
        UnitPrice = unitPrice;
    }

    public Sku Sku { get; }

    public Quantity Quantity { get; }

    public Money UnitPrice { get; }

    public Money Total => UnitPrice.Times(Quantity.Units);

}
