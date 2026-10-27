using JustDummies;
using JustDummies.Xunit;

using Shop.Domain;

using Xunit;

namespace Shop.Tests;

/// <summary>Harness checks: each scaffolded generator, as resolved by hand, never makes a constructor throw.</summary>
[Reproducible]
public sealed class ScaffoldedGeneratorTests {

    private const int Draws = 500;

    private static void AlwaysBuilds<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            T value = generator.Generate();
            Assert.NotNull(value);
        }
    }

    [Fact]
    public void Scaffolded_AnySku_always_builds_a_valid_Sku() => AlwaysBuilds(Any.Sku());

    [Fact]
    public void Scaffolded_AnyQuantity_always_builds_a_valid_Quantity() => AlwaysBuilds(Any.Quantity());

    [Fact]
    public void Scaffolded_AnyMoney_always_builds_a_valid_Money() => AlwaysBuilds(Any.Money());

    [Fact]
    public void Scaffolded_AnyDiscountPercentage_always_builds_a_valid_DiscountPercentage() => AlwaysBuilds(Any.DiscountPercentage());

    [Fact]
    public void Scaffolded_AnyShippingAddress_always_builds_a_valid_ShippingAddress() => AlwaysBuilds(Any.ShippingAddress());

    [Fact]
    public void Scaffolded_AnyOrderLine_always_builds_a_valid_OrderLine() => AlwaysBuilds(Any.OrderLine());

    [Fact]
    public void Scaffolded_AnyOrder_always_builds_a_valid_Order() => AlwaysBuilds(Any.Order());

    [Fact]
    public void Scaffolded_AnyGiftCard_always_builds_a_valid_GiftCard() => AlwaysBuilds(Any.GiftCard());

    /// <summary>
    ///     The recap reported 'maxUses  int  Any.Int32()' with no provenance at all — neither 'guard' nor
    ///     'to verify' — although the constructor guards it with a throw expression:
    ///     <c>MaxUses = maxUses &gt; 0 ? maxUses : throw new ArgumentOutOfRangeException(...)</c>.
    ///     The emitted base compiles and draws zero or a negative number about half the time.
    /// </summary>
    [Fact]
    public void Bug_Scaffolded_AnyPromoCode_misses_the_throw_expression_guard_on_maxUses() => AlwaysBuilds(Any.PromoCode());

}
