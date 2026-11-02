using JustDummies;

namespace Shop.Domain.Tests.Regular;

/// <summary>Small generators shared by several test classes.</summary>
internal static class Dummies {

    public static AnyOneOf<string> Currency() {
        return Any.OneOf("EUR", "USD", "GBP");
    }

    public static AnyDateTime Instant() {
        return Any.DateTime().Between(new DateTime(2000, 1, 1), new DateTime(2100, 1, 1));
    }

    public static OrderLine Line(decimal unitPrice, int units, string currency = "EUR") {
        return new OrderLine(Any.Sku().Generate(), new Quantity(units), new Money(unitPrice, currency));
    }

    public static Order OrderOf(IReadOnlyList<OrderLine> lines, ShippingMethod method = ShippingMethod.Standard, decimal discount = 0m, string countryCode = "FR") {
        return Any.Order()
                  .WithLines(lines)
                  .WithShippingMethod(method)
                  .WithDiscount(new DiscountPercentage(discount))
                  .WithShippingAddress(Any.ShippingAddress().WithCountryCode(countryCode).Generate())
                  .Generate();
    }

}
