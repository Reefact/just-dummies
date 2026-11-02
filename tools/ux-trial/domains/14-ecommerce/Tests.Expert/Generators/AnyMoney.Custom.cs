using JustDummies;

namespace Shop.Domain;

// Written by hand, beside the scaffolded file: `any generate Money --force` leaves it alone.
public sealed partial class AnyMoney {

    /// <summary>A currency the shop accepts, for a test that needs one on its own (CS0122 otherwise: the valid base is private).</summary>
    public static AnyString ValidCurrencies() {
        return AnyValidCurrency();
    }

    /// <summary>An amount Money accepts, for a test that needs one on its own.</summary>
    public static AnyDecimal ValidAmounts() {
        return AnyValidAmount();
    }

}
