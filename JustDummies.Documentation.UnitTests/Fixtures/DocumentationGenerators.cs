namespace JustDummies.Documentation.UnitTests.Fixtures;

/// <summary>
///     A named, reusable generator for <see cref="OrderReference" />, so a page can show a test's own generation
///     factored out of its Arrange rather than inlined every time.
/// </summary>
public sealed class AnyOrderReference : IAny<OrderReference> {

    /// <inheritdoc />
    public OrderReference Generate() {
        return Any.String().StartingWith(DocumentationDomain.OrderReferencePrefix).WithLength(DocumentationDomain.OrderReferenceLength).As(OrderReference.Create).Generate();
    }

}

/// <summary>A named, reusable generator for a customer's name, for the same reason as <see cref="AnyOrderReference" />.</summary>
public sealed class AnyCustomerName : IAny<string> {

    /// <inheritdoc />
    public string Generate() {
        return Any.String().Alpha().WithLengthBetween(1, 50).Generate();
    }

}

/// <summary>
///     Hangs <see cref="AnyOrderReference" />, <see cref="AnyCustomerName" /> and the two scaffolded generators off the
///     library's own entry point, as <c>any generate --entry-point any</c> would.
/// </summary>
public static class AnyEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary order reference.</summary>
        public static AnyOrderReference OrderReference() {
            return new AnyOrderReference();
        }

        /// <summary>Starts an arbitrary customer name.</summary>
        public static AnyCustomerName CustomerName() {
            return new AnyCustomerName();
        }

        /// <summary>Starts an arbitrary route, through the generator <c>any</c> scaffolded for it.</summary>
        public static AnyRoute Route() {
            return new AnyRoute();
        }

        /// <summary>Starts an arbitrary shipment, through the generator <c>any</c> scaffolded for it.</summary>
        public static AnyShipment Shipment() {
            return new AnyShipment();
        }

        /// <summary>Starts an arbitrary combination of runners, through the generator <c>any</c> scaffolded for it.</summary>
        public static AnyCombination Combination() {
            return new AnyCombination();
        }

    }

}
