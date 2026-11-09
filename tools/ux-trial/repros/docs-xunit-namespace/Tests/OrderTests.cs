using JustDummies;
using Repro.Domain;
using Xunit;

// Written exactly as doc/handwritten/for-users/packages/justdummies-xunit.en.md shows it:
// the page never shows `using JustDummies.Xunit;`.
public sealed class OrderTests {

    [Fact, Reproducible]
    public void An_order_keeps_its_customer_name() {
        string anyCustomerName = Any.String().Alpha().WithLengthBetween(1, 50).Generate();

        Order order = new Order(anyCustomerName);

        Assert.Equal(anyCustomerName, order.CustomerName);
    }

}
