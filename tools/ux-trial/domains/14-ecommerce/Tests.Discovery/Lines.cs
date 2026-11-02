using JustDummies;

namespace Shop.Domain;

/// <summary>Small builders shared by the tests: the price and quantity are what a test is about, the SKU never is.</summary>
internal static class Lines {

    public static OrderLine Of(decimal unitPrice, int units, string currency = "EUR") {
        Sku anySku = Any.Sku().Generate();

        return new OrderLine(anySku, new Quantity(units), new Money(unitPrice, currency));
    }

    public static AnyOrder OrderOf(params OrderLine[] lines) {
        return Any.Order().WithLines(lines);
    }

}
