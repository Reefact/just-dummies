using JustDummies;

using Shop.Domain;

namespace Shop.Tests;

/// <summary>
///     Mistakes an analyzer might catch at build time. Never executed: it exists only so the build output can be
///     inspected for JDxxx diagnostics (see NOTES.md).
/// </summary>
internal static class AnalyzerProbes {

    private static readonly Sku DrawnAtTypeLoad = Any.Sku().Generate();

    private static readonly string LibraryDrawAtTypeLoad = Any.String().NotBlank().WithMaxLength(10).Generate();

    private static readonly Sku AsDrawAtTypeLoad = Any.StringMatching(@"[A-Z]{3}-\d{4}").As(value => new Sku(value)).Generate();

    internal static void Mistakes() {
        // a) a decimal chain narrowed to nothing, all constants
        decimal a = Any.Decimal().Between(0m, 100m).GreaterThan(200m).Generate();

        // b) Distinct over a type without value equality
        List<OrderLine> b = Any.ListOf(Any.OrderLine()).WithCount(3).Distinct().Generate();

        // c) more distinct enum members than exist
        HashSet<ShippingMethod> c = Any.SetOf(Any.Enum<ShippingMethod>()).WithCount(4).Generate();

        // d) an operand drawn and thrown away
        IAny<Sku> d = Any.Combine(Any.Sku(), Any.Quantity(), (sku, quantity) => sku);

        // e) the same bound twice
        decimal e = Any.Decimal().Between(0m, 100m).WithScale(1).Between(0m, 100m).Generate();

        // f) a constant non-regular pattern
        string f = Any.StringMatching(@"(?!ZZZ)[A-Z]{3}-\d{4}").Generate();

        // g) a scale with no point in a constant range
        decimal g = Any.Decimal().Between(0.001m, 0.009m).WithScale(2).Generate();

        // h) a contradictory refinement written through a scaffolded generator
        Order h = Any.Order().WithDiscount(discount => discount.WithValue(value => value.GreaterThan(150m))).Generate();

        // i) a scaffolded generator built and discarded
        Any.Sku().WithValue(value => value.DifferentFrom("ABC-0001"));

        // j) a library generator built and discarded
        Any.Decimal().Between(0m, 100m);

        // k) a lower-case SKU pinned in a pool the pattern refuses
        string k = Any.StringMatching(@"[A-Z]{3}-\d{4}").OneOf("abc-0001").Generate();

        // l) a drawn value in the assertion position (a property with a sample size of one)
        Quantity anyQuantity = Any.Quantity().Generate();
        Xunit.Assert.Equal(anyQuantity.Units * 2, anyQuantity.Add(anyQuantity).Units);

        // n) Between declared twice, overlapping
        decimal n = Any.Decimal().Between(0m, 100m).Between(50m, 200m).Generate();

        // o) Distinct over the same element type, built inline through As(...)
        List<Sku> o = Any.ListOf(Any.StringMatching(@"[A-Z]{3}-\d{4}").As(value => new Sku(value))).WithCount(3).Distinct().Generate();
        List<Quantity> o2 = Any.ListOf(Any.Int32().Between(1, 2).As(units => new Quantity(units))).WithCount(3).Distinct().Generate();
        List<OrderLine> o3 = Any.ListOf(Any.Sku().As(sku => new OrderLine(sku, new Quantity(1), new Money(1m, "EUR")))).WithCount(3).Distinct().Generate();

        // m) a clock-relative bound
        DateTime m = Any.DateTime().Before(DateTime.Now).Generate();

        _ = (a, b, c, d, e, f, g, h, k, m, n, o, o2, o3, DrawnAtTypeLoad, LibraryDrawAtTypeLoad, AsDrawAtTypeLoad);
    }

}
