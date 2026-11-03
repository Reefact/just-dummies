using BatchGenealogy.Domain;

using JustDummies;

using Genealogy = BatchGenealogy.Domain.BatchGenealogy;

namespace BatchGenealogy.Domain.Tests;

/// <summary>
///     A site on one production day, for the tests that need a genealogy with lots in it. Everything the
///     tests do not care about (site code, lot numbers, materials, how old a stock lot is, ids) is drawn;
///     the quantities are always passed in, because the shares and the balances are what the tests are about.
/// </summary>
internal sealed class Plant {

    private readonly HashSet<string> _usedTransformationIds = new HashSet<string>();

    public Plant() {
        Genealogy     = Any.BatchGenealogy().Generate();
        ProductionDay = Any.DateOnly().Between(new DateOnly(2021, 1, 1), new DateOnly(2030, 12, 31)).Generate();
    }

    public Genealogy Genealogy { get; }

    public DateOnly ProductionDay { get; }

    public Quantity Remaining(LotNumber lot) {
        return Genealogy.Get(lot).RemainingQuantity;
    }

    public static Quantity Kg(decimal amount) {
        return new Quantity(amount, QuantityUnit.Kilogram);
    }

    public static List<LotNumber> DistinctLotNumbers(int count) {
        return Any.ListOf(Any.LotNumber().Base()).WithCount(count).Distinct().Generate();
    }

    /// <summary>A released lot, manufactured before and expiring after the production day, so it can be consumed on it.</summary>
    public Lot ReleasedStock(LotNumber number, decimal kg) {
        Lot lot = Any.Lot()
                     .WithNumber(number)
                     .WithInitialQuantity(Kg(kg))
                     .WithManufacturedOn(ProductionDay.AddDays(-Any.Int32().Between(1, 1_000).Generate()))
                     .WithExpiresOn(ProductionDay.AddDays(Any.Int32().Between(1, 1_000).Generate()))
                     .Generate();
        lot.Release();
        Genealogy.Register(lot);

        return lot;
    }

    /// <summary>A released lot with the exact dates the test is about.</summary>
    public Lot ReleasedLot(LotNumber number, decimal kg, DateOnly manufacturedOn, DateOnly expiresOn) {
        Lot lot = Any.Lot()
                     .WithNumber(number)
                     .WithInitialQuantity(Kg(kg))
                     .WithManufacturedOn(manufacturedOn)
                     .WithExpiresOn(expiresOn)
                     .Generate();
        lot.Release();
        Genealogy.Register(lot);

        return lot;
    }

    /// <summary>A lot manufactured on the production day, still in quarantine, as the output of a step.</summary>
    public Lot ProducedLot(LotNumber number, decimal kg, MaterialCode? material = null) {
        AnyLot lots = Any.Lot()
                         .WithNumber(number)
                         .WithInitialQuantity(Kg(kg))
                         .WithManufacturedOn(ProductionDay)
                         .WithExpiresOn(ProductionDay.AddDays(Any.Int32().Between(1, 3_653).Generate()));
        if (material is not null) { lots = lots.WithMaterial(material); }

        Lot lot = lots.Generate();
        Genealogy.Register(lot);

        return lot;
    }

    /// <summary>A step on the production day. Nothing is registered or recorded.</summary>
    public Transformation Step(TransformationKind kind, (LotNumber Lot, decimal Kg)[] inputs, (LotNumber Lot, decimal Kg)[] outputs) {
        return Any.Transformation()
                  .WithKind(kind)
                  .WithId(NextTransformationId())
                  .WithPerformedOn(ProductionDay)
                  .WithInputsAndOutputs(inputs.Select(input => new LotQuantity(input.Lot, Kg(input.Kg))).ToList(),
                                        outputs.Select(output => new LotQuantity(output.Lot, Kg(output.Kg))).ToList())
                  .Generate();
    }

    /// <summary>
    ///     Registers the output as a produced lot, records the step, then releases the output so that it can feed
    ///     the next one: the common way of growing a graph. A test about the state a step leaves behind uses
    ///     <see cref="ProducedLot" /> and <see cref="Step" /> instead.
    /// </summary>
    public Transformation Blend(LotNumber output, decimal outputKg, params (LotNumber Lot, decimal Kg)[] inputs) {
        ProducedLot(output, outputKg);
        Transformation blend = Step(TransformationKind.Blend, inputs, [(output, outputKg)]);
        Genealogy.Record(blend);
        Genealogy.Get(output).Release();

        return blend;
    }

    public Transformation Process(LotNumber output, decimal outputKg, LotNumber input, decimal inputKg) {
        ProducedLot(output, outputKg);
        Transformation process = Step(TransformationKind.Process, [(input, inputKg)], [(output, outputKg)]);
        Genealogy.Record(process);
        Genealogy.Get(output).Release();

        return process;
    }

    /// <summary>Splits <paramref name="input" /> into sub-lots 01, 02, ... of the given amounts.</summary>
    public Transformation Split(LotNumber input, decimal inputKg, params decimal[] subLotKgs) {
        MaterialCode material = Genealogy.Get(input).Material;
        (LotNumber Lot, decimal Kg)[] outputs = subLotKgs.Select((kg, index) => (input.SubLot(index + 1), kg)).ToArray();
        foreach ((LotNumber lot, decimal kg) in outputs) { ProducedLot(lot, kg, material); }

        Transformation split = Step(TransformationKind.Split, [(input, inputKg)], outputs);
        Genealogy.Record(split);
        foreach ((LotNumber lot, decimal _) in outputs) { Genealogy.Get(lot).Release(); }

        return split;
    }

    private string NextTransformationId() {
        string id = Any.StringMatching(@"TRF-[0-9]{6}").Except(_usedTransformationIds).Generate();
        _usedTransformationIds.Add(id);

        return id;
    }

}
