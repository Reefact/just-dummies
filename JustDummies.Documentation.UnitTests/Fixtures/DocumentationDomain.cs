namespace JustDummies.Documentation.UnitTests.Fixtures;

/// <summary>
///     The small illustrative domain every documentation sample is allowed to name, supplied once here so a page can
///     show what matters — the constraint, the composition, the replay — without spending five lines re-declaring an
///     order reference first.
/// </summary>
/// <remarks>
///     <para>
///         These types are <c>public</c> because the sample compiler references THIS assembly and binds each snippet
///         against it: an <c>internal</c> fixture would be invisible to the very compilation that has to resolve it.
///     </para>
///     <para>
///         They are deliberately ordinary. A fixture that demonstrated a clever pattern of its own would compete with
///         the page for the reader's attention, and a page whose sample cannot be read without first reading the
///         fixture has moved the explanation somewhere the reader will never look.
///     </para>
/// </remarks>
public static class DocumentationDomain {

    /// <summary>
    ///     The prefix <see cref="OrderReference" /> requires, exposed so a page can constrain a string to it without
    ///     repeating the literal and drifting from what the factory actually accepts.
    /// </summary>
    public const string OrderReferencePrefix = "ORD-";

    /// <summary>The exact length <see cref="OrderReference" /> requires.</summary>
    public const int OrderReferenceLength = 12;

}

/// <summary>
///     A value object with a stricter contract than the primitive it is built from: the canonical target of
///     <c>.As(OrderReference.Create)</c> in the composition guide. Its factory validates, which is the whole point —
///     a dummy that reaches it has to have been constrained well enough to satisfy it.
/// </summary>
public sealed record OrderReference {

    private OrderReference(string value) {
        Value = value;
    }

    /// <summary>The validated reference.</summary>
    public string Value { get; }

    /// <summary>Builds a reference, refusing anything that is not one.</summary>
    /// <exception cref="ArgumentException">The value is not a well-formed order reference.</exception>
    public static OrderReference Create(string value) {
        ArgumentNullException.ThrowIfNull(value);

        if (!value.StartsWith(DocumentationDomain.OrderReferencePrefix, StringComparison.Ordinal)) {
            throw new ArgumentException($"An order reference starts with '{DocumentationDomain.OrderReferencePrefix}'; got '{value}'.", nameof(value));
        }
        if (value.Length != DocumentationDomain.OrderReferenceLength) {
            throw new ArgumentException($"An order reference is {DocumentationDomain.OrderReferenceLength} characters long; got {value.Length}.", nameof(value));
        }

        return new OrderReference(value);
    }

    /// <inheritdoc />
    public override string ToString() {
        return Value;
    }

}

/// <summary>
///     A two-part value object, used wherever a page needs to show <c>Any.Combine</c> folding several constrained
///     primitives into one type rather than a single <c>.As(...)</c> transformation.
/// </summary>
public sealed record Money {

    private Money(decimal amount, string currency) {
        Amount   = amount;
        Currency = currency;
    }

    /// <summary>The amount, never negative.</summary>
    public decimal Amount { get; }

    /// <summary>The ISO 4217 currency code, three upper-case letters.</summary>
    public string Currency { get; }

    /// <summary>Builds an amount of money, refusing anything that is not one.</summary>
    /// <exception cref="ArgumentException">The amount is negative, or the currency is not a three-letter code.</exception>
    public static Money Create(decimal amount, string currency) {
        ArgumentNullException.ThrowIfNull(currency);

        if (amount < 0m) {
            throw new ArgumentException($"An amount of money is not negative; got {amount}.", nameof(amount));
        }
        if (currency.Length != 3) {
            throw new ArgumentException($"A currency code is three letters; got '{currency}'.", nameof(currency));
        }

        return new Money(amount, currency);
    }

    /// <inheritdoc />
    public override string ToString() {
        return $"{Amount} {Currency}";
    }

}

/// <summary>A plain aggregate, so a page can show a whole object being composed from several generators.</summary>
/// <param name="Id">The customer's identifier.</param>
/// <param name="Name">The customer's display name.</param>
/// <param name="Email">The customer's e-mail address.</param>
public sealed record Customer(Guid Id, string Name, string Email);

/// <summary>
///     An aggregate carrying both kinds of construction argument at once, so a page can show the difference in a
///     single call: a reference and a customer name the discount rule never consults — the dummies — beside an amount
///     it is entirely about, which therefore stays a literal.
/// </summary>
public sealed class Order {

    /// <summary>Builds an order at its undiscounted amount.</summary>
    /// <exception cref="ArgumentNullException">The reference or the customer name is <see langword="null" />.</exception>
    public Order(OrderReference reference, string customerName, decimal amount) {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(customerName);

        Reference    = reference;
        CustomerName = customerName;
        Total        = amount;
    }

    /// <summary>The order's reference.</summary>
    public OrderReference Reference { get; }

    /// <summary>The name the order was placed under.</summary>
    public string CustomerName { get; }

    /// <summary>The amount currently owed, after whatever discounts have been applied.</summary>
    public decimal Total { get; private set; }

    /// <summary>Takes a percentage off the total.</summary>
    public void ApplyDiscount(int percentage) {
        Total -= Total * percentage / 100m;
    }

}

/// <summary>
///     A rule with a threshold in it, so a page can show the difference between constraining a dummy to the shape of
///     an assertion — which proves nothing — and constraining it to the domain and letting the assertion carry the
///     rule.
/// </summary>
public static class Shipping {

    /// <summary>The order total at or below which shipping is charged.</summary>
    public const decimal FreeShippingThreshold = 100m;

    /// <summary>The fee charged when the threshold is not cleared.</summary>
    public const decimal StandardFee = 4.90m;

    /// <summary>The shipping fee for an order total: waived above the threshold, charged otherwise.</summary>
    public static decimal FeeFor(decimal orderTotal) {
        return orderTotal > FreeShippingThreshold ? 0m : StandardFee;
    }

}

/// <summary>An ordinary enumeration, for the enum generator's membership and exclusion constraints.</summary>
public enum OrderStatus {

    /// <summary>Not submitted yet.</summary>
    Draft,

    /// <summary>Submitted, awaiting payment.</summary>
    Submitted,

    /// <summary>Paid, awaiting shipment.</summary>
    Paid,

    /// <summary>Shipped to the customer.</summary>
    Shipped,

    /// <summary>Cancelled before completion.</summary>
    Cancelled

}

/// <summary>
///     A flags enumeration, for the <c>AllowingCombinations()</c> opt-in: without it a draw yields one declared
///     member, with it a draw may yield any combination of them.
/// </summary>
[Flags]
public enum Permissions {

    /// <summary>No permission at all.</summary>
    None = 0,

    /// <summary>May read.</summary>
    Read = 1,

    /// <summary>May write.</summary>
    Write = 2,

    /// <summary>May delete.</summary>
    Delete = 4

}

/// <summary>
///     A value object that is valid on its own whether or not it crosses a border, so a page can show a rule that
///     belongs to the type composing it rather than to this one: <see cref="Shipment" /> accepts only a domestic
///     route.
/// </summary>
public sealed record Route {

    /// <summary>Builds a route.</summary>
    /// <exception cref="ArgumentNullException">The origin or the destination is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The distance is not positive.</exception>
    public Route(string origin, string destination, int distanceKm, bool domestic) {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(destination);
        if (distanceKm <= 0) { throw new ArgumentOutOfRangeException(nameof(distanceKm), distanceKm, "A route has a positive length."); }

        Origin      = origin;
        Destination = destination;
        DistanceKm  = distanceKm;
        Domestic    = domestic;
    }

    /// <summary>Where the route starts.</summary>
    public string Origin { get; }

    /// <summary>Where the route ends.</summary>
    public string Destination { get; }

    /// <summary>The route's length, in kilometres.</summary>
    public int DistanceKm { get; }

    /// <summary>Whether the route stays within one country.</summary>
    public bool Domestic { get; }

}

/// <summary>
///     An aggregate with a rule on a value it composes — its route must be domestic — and a leaf guarded by its own
///     constructor, so a page can show a test refining a parameter's valid base rather than rebuilding it.
/// </summary>
public sealed class Shipment {

    /// <summary>The route length above which the driver has to stop on the way.</summary>
    public const int RestStopDistanceKm = 200;

    /// <summary>Builds a shipment.</summary>
    /// <exception cref="ArgumentNullException">The route or the labels are <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">The route crosses a border.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The shipment carries no parcel.</exception>
    public Shipment(Route route, int parcels, IReadOnlyList<string> labels, decimal? declaredValue) {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(labels);
        if (!route.Domestic) { throw new ArgumentException("A shipment route must be domestic.", nameof(route)); }
        if (parcels <= 0) { throw new ArgumentOutOfRangeException(nameof(parcels), parcels, "A shipment carries at least one parcel."); }

        Route         = route;
        Parcels       = parcels;
        Labels        = labels;
        DeclaredValue = declaredValue;
    }

    /// <summary>The route the shipment follows.</summary>
    public Route Route { get; }

    /// <summary>How many parcels it carries.</summary>
    public int Parcels { get; }

    /// <summary>The labels printed on it.</summary>
    public IReadOnlyList<string> Labels { get; }

    /// <summary>The value declared for insurance, if any.</summary>
    public decimal? DeclaredValue { get; }

    /// <summary>Whether the driver has to stop on the way.</summary>
    public bool NeedsRestStop => Route.DistanceKm > RestStopDistanceKm;

    /// <summary>Whether this shipment can share a truck with <paramref name="other" />: they follow the same route.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public bool CanTravelWith(Shipment other) {
        ArgumentNullException.ThrowIfNull(other);

        return Route == other.Route;
    }

}
