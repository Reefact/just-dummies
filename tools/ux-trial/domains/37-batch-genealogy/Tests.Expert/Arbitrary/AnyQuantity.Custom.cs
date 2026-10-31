using JustDummies;

namespace BatchGenealogy.Domain;

public sealed partial class AnyQuantity {

    /// <summary>A quantity weighed: milligrams, grams or kilograms.</summary>
    public AnyQuantity Mass() {
        return WithUnit(unit => unit.OneOf(QuantityUnit.Milligram, QuantityUnit.Gram, QuantityUnit.Kilogram));
    }

    /// <summary>A quantity measured by volume: millilitres or litres.</summary>
    public AnyQuantity Volume() {
        return WithUnit(unit => unit.OneOf(QuantityUnit.Millilitre, QuantityUnit.Litre));
    }

    /// <summary>A quantity a lot or a transformation can carry: never zero.</summary>
    public AnyQuantity NonZero() {
        return WithAmount(amount => amount.Positive());
    }

}
