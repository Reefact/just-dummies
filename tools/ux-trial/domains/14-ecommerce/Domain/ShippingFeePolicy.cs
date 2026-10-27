namespace Shop.Domain;

/// <summary>
///     Standard shipping is 4.90 inside France and 9.90 abroad, free from the threshold up (after discount).
///     Express is 14.90 always. Pickup points are 2.90 always.
/// </summary>
public static class ShippingFeePolicy {

    public static Money FeeFor(Order order) {
        decimal fee = order.ShippingMethod switch {
            ShippingMethod.Express     => 14.90m,
            ShippingMethod.PickupPoint => 2.90m,
            ShippingMethod.Standard when order.DiscountedSubtotal.Amount >= Order.FreeShippingThreshold => 0m,
            ShippingMethod.Standard when order.ShippingAddress.IsDomestic                              => 4.90m,
            ShippingMethod.Standard                                                                    => 9.90m,
            _ => throw new ArgumentOutOfRangeException(nameof(order), order.ShippingMethod, "Unknown shipping method.")
        };

        return new Money(fee, order.Currency);
    }

}
