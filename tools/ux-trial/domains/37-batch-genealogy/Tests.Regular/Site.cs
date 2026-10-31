using BatchGenealogy.Domain;

using JustDummies;

namespace GenealogyTests;

/// <summary>A genealogy under construction, so a test states its graph rather than its plumbing.</summary>
internal sealed class Site {

    public Site() {
        Genealogy = new BatchGenealogy.Domain.BatchGenealogy(Arbitrary.SiteCode().Generate());
        Day       = Arbitrary.ProductionDay().Generate();
    }

    public BatchGenealogy.Domain.BatchGenealogy Genealogy { get; }

    /// <summary>The day every transformation of the test is performed on.</summary>
    public DateOnly Day { get; }

    /// <summary>Registers and releases a raw lot of an arbitrary material holding <paramref name="quantity" />.</summary>
    public LotNumber ReleasedLot(Quantity quantity) {
        Lot lot = Arbitrary.Lot().WithInitialQuantity(quantity).Generate();
        Genealogy.Register(lot);
        lot.Release();

        return lot.Number;
    }

    /// <summary>Registers the lot a transformation of the test will produce, with what it produces.</summary>
    public LotNumber OutputLot(Quantity quantity, LotNumber? number = null, MaterialCode? material = null) {
        AnyLot anyLot = Arbitrary.Lot().WithInitialQuantity(quantity).WithManufacturedOn(Day);
        if (number is not null) { anyLot = anyLot.WithNumber(number); }
        if (material is not null) { anyLot = anyLot.WithMaterial(material); }

        Lot lot = anyLot.Generate();
        Genealogy.Register(lot);

        return lot.Number;
    }

    /// <summary>Records a transformation of <paramref name="kind" /> on <see cref="Day" />, then releases its outputs.</summary>
    public Transformation Record(TransformationKind kind, LotQuantity[] inputs, LotQuantity[] outputs) {
        Transformation transformation = new(Arbitrary.TransformationId().Generate(), kind, inputs, outputs, Day);
        Genealogy.Record(transformation);
        foreach (LotQuantity output in outputs) {
            Genealogy.Get(output.Lot).Release();
        }

        return transformation;
    }

    public static Quantity Kg(decimal amount) {
        return new Quantity(amount, QuantityUnit.Kilogram);
    }

    public static LotQuantity Part(LotNumber lot, Quantity quantity) {
        return new LotQuantity(lot, quantity);
    }

}
