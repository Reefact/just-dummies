namespace BatchGenealogy.Tests.Expert;

/// <summary>Literal quantities, for the tests whose assertion is about them.</summary>
internal static class Quantities {

    public static Quantity Kg(decimal amount) => new(amount, QuantityUnit.Kilogram);

    public static Quantity G(decimal amount) => new(amount, QuantityUnit.Gram);

    public static Quantity L(decimal amount) => new(amount, QuantityUnit.Litre);

    /// <summary>Reads as the input of a step: <c>Kg(300).Of(api)</c>.</summary>
    public static (LotNumber Lot, Quantity Quantity) Of(this Quantity quantity, LotNumber lot) => (lot, quantity);

}
