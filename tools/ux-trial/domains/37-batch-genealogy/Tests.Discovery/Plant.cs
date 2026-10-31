using BatchGenealogy.Domain;

using JustDummies;

using Genealogy = BatchGenealogy.Domain.BatchGenealogy;

namespace BatchGenealogy.Tests.Discovery;

/// <summary>Test-side helpers that set up a site genealogy; every value they draw is a dummy.</summary>
internal static class Plant {

    public static Quantity Kg(decimal amount) {
        return new Quantity(amount, QuantityUnit.Kilogram);
    }

    public static AnyPattern AnyTransformationId() {
        return Any.StringMatching("TRF-[0-9]{6}");
    }

    public static Genealogy AnySite() {
        return new Genealogy(Any.StringMatching("[A-Z]{4}").Generate());
    }

    public static List<LotNumber> AnyDistinctLotNumbers(int count) {
        return Any.ListOf(Any.LotNumber()).WithCount(count).Distinct().Generate();
    }

    /// <summary>Registers a released lot, manufactured on <paramref name="manufacturedOn" /> and still usable for a month.</summary>
    public static Lot RegisterReleased(Genealogy site, LotNumber number, Quantity quantity, DateOnly manufacturedOn, MaterialCode? material = null) {
        Lot lot = Register(site, number, quantity, manufacturedOn, material);
        lot.Release();

        return lot;
    }

    /// <summary>Registers a lot as quality would before release: quarantined.</summary>
    public static Lot Register(Genealogy site, LotNumber number, Quantity quantity, DateOnly manufacturedOn, MaterialCode? material = null) {
        AnyLot recipe = Any.Lot()
                           .WithNumber(number)
                           .WithInitialQuantity(quantity)
                           .WithManufacturedOn(manufacturedOn)
                           .WithShelfLifeDays(days => days.GreaterThanOrEqualTo(30));
        if (material is not null) {
            recipe = recipe.WithMaterial(material);
        }

        Lot lot = recipe.Generate();
        site.Register(lot);

        return lot;
    }

    public static DateOnly AnyProductionDay() {
        return Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2030, 12, 31)).Generate();
    }

}
