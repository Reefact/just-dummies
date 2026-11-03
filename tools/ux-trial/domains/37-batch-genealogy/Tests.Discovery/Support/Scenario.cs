using BatchGenealogy.Domain;

using JustDummies;

namespace BatchGenealogy.Tests.Discovery;

/// <summary>
///     Builders for the tests that need a genealogy with lots already in it. Everything a test is not about
///     (numbers, material, dates around the day of the step) is drawn; the quantities that the assertions
///     compute with are always passed in as literals.
/// </summary>
internal static class Scenario {

    public static BatchGenealogy.Domain.BatchGenealogy AnyGenealogy() {
        return Any.BatchGenealogy().Generate();
    }

    public static DateOnly AnyDay() {
        return Any.DateOnly().Between(new DateOnly(2022, 1, 1), new DateOnly(2026, 12, 31)).Generate();
    }

    /// <summary>A later day, still well inside the shelf life of the lots a step produces.</summary>
    public static DateOnly AnyDayAfter(DateOnly day) {
        return day.AddDays(Any.Int32().Between(1, 50).Generate());
    }

    public static Quantity Grams(decimal amount) {
        return new Quantity(amount, QuantityUnit.Gram);
    }

    public static Quantity Litres(decimal amount) {
        return new Quantity(amount, QuantityUnit.Litre);
    }

    public static string AnyTransformationId() {
        return Any.TransformationId().Generate();
    }

    /// <summary>A lot made before <paramref name="day" />, still valid on it, already released and registered.</summary>
    public static Lot ReleasedLotOn(BatchGenealogy.Domain.BatchGenealogy genealogy, DateOnly day, Quantity quantity, LotNumber? number = null, MaterialCode? material = null) {
        Lot lot = AnyLotAround(day, quantity, number, material);
        lot.Release();
        genealogy.Register(lot);

        return lot;
    }

    /// <summary>A lot made before <paramref name="day" />, still valid on it, registered but not released.</summary>
    public static Lot QuarantinedLotOn(BatchGenealogy.Domain.BatchGenealogy genealogy, DateOnly day, Quantity quantity, LotNumber? number = null, MaterialCode? material = null) {
        Lot lot = AnyLotAround(day, quantity, number, material);
        genealogy.Register(lot);

        return lot;
    }

    /// <summary>The lot a step performed on <paramref name="day" /> produces: made on that day, registered with its quantity.</summary>
    public static Lot ProducedLotOn(BatchGenealogy.Domain.BatchGenealogy genealogy, DateOnly day, Quantity quantity, LotNumber? number = null, MaterialCode? material = null) {
        AnyLot recipe = Any.Lot().WithManufacturedOn(day).WithExpiresOn(e => e.After(day.AddDays(100))).WithInitialQuantity(quantity);
        if (number is not null) { recipe = recipe.WithNumber(number); }
        if (material is not null) { recipe = recipe.WithMaterial(material); }

        Lot lot = recipe.Generate();
        genealogy.Register(lot);

        return lot;
    }

    public static LotQuantity Part(Lot lot, Quantity quantity) {
        return new LotQuantity(lot.Number, quantity);
    }

    public static Transformation Blend(DateOnly day, LotQuantity[] inputs, LotQuantity output) {
        return new Transformation(AnyTransformationId(), TransformationKind.Blend, inputs, [output], day);
    }

    public static Transformation Process(DateOnly day, LotQuantity[] inputs, LotQuantity output) {
        return new Transformation(AnyTransformationId(), TransformationKind.Process, inputs, [output], day);
    }

    public static Transformation Split(DateOnly day, LotQuantity input, LotQuantity[] outputs) {
        return new Transformation(AnyTransformationId(), TransformationKind.Split, [input], outputs, day);
    }

    private static Lot AnyLotAround(DateOnly day, Quantity quantity, LotNumber? number, MaterialCode? material) {
        AnyLot recipe = Any.Lot()
                           .WithManufacturedOn(m => m.Before(day))
                           .WithExpiresOn(e => e.After(day.AddDays(200)))
                           .WithInitialQuantity(quantity);
        if (number is not null) { recipe = recipe.WithNumber(number); }
        if (material is not null) { recipe = recipe.WithMaterial(material); }

        return recipe.Generate();
    }

}
