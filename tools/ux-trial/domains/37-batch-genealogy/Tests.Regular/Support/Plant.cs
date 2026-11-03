namespace BatchGenealogy.Domain.Tests;

/// <summary>A quantity of a lot, in kilograms, taking part in a step: the amounts the tests assert on are literals.</summary>
internal readonly record struct Part(Lot Lot, decimal Kilograms);

/// <summary>A transformation built and its output lots registered, but not yet recorded.</summary>
internal sealed class Step {

    public Step(Transformation transformation, IReadOnlyList<Lot> outputs) {
        Transformation = transformation;
        Outputs        = outputs;
    }

    public Transformation      Transformation { get; }
    public IReadOnlyList<Lot>  Outputs        { get; }

}

internal static class PartExtensions {

    public static Part Take(this Lot lot, decimal kilograms) => new Part(lot, kilograms);

}

internal static class Kg {

    public static Quantity Of(decimal amount) => new Quantity(amount, QuantityUnit.Kilogram);

}

/// <summary>
///     A manufacturing site under test: lots stocked and released, steps prepared and recorded. Everything the
///     tests do not assert on (numbers, materials, dates, ids, shelf lives) is drawn; quantities are literals.
/// </summary>
internal sealed class Plant {

    private readonly List<string> _usedTransformationIds = new List<string>();

    public Plant() {
        Genealogy = Any.BatchGenealogy().Generate();
        Day0      = Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2024, 12, 31)).Generate();
    }

    public BatchGenealogy Genealogy { get; }

    /// <summary>The day the raw lots were manufactured; the steps happen after it.</summary>
    public DateOnly Day0 { get; }

    public DateOnly Later(int days) => Day0.AddDays(days);

    /// <summary>A registered lot in kilograms, released by default.</summary>
    public Lot Stock(decimal kilograms, DateOnly? manufacturedOn = null, bool released = true, MaterialCode? material = null) {
        Lot lot = NewLot(UnusedNumber(), kilograms, manufacturedOn ?? Day0, material);
        Genealogy.Register(lot);
        if (released) { lot.Release(); }

        return lot;
    }

    public LotNumber UnusedNumber() {
        LotNumber number = Any.LotNumber().WhollyLot().Generate();
        while (Genealogy.Lots.Any(lot => lot.Number.BaseLot == number.BaseLot)) {
            number = Any.LotNumber().WhollyLot().Generate();
        }

        return number;
    }

    /// <summary>
    ///     Built with the constructor rather than <c>Any.Lot().WithManufacturedOn(..).WithExpiresOn(..)</c>: the scaffolded
    ///     pair applies the first pin before the second, so two pins outside the recipe's window are refused.
    /// </summary>
    public Lot NewLot(LotNumber number, decimal kilograms, DateOnly manufacturedOn, MaterialCode? material = null) {
        return new Lot(number,
                       material ?? Any.MaterialCode().Generate(),
                       Kg.Of(kilograms),
                       manufacturedOn,
                       manufacturedOn.AddDays(Any.Int32().Between(400, 3_000).Generate()));
    }

    public Step PrepareBlend(DateOnly on, decimal outputKilograms, params Part[] inputs) {
        return Prepare(TransformationKind.Blend, on, inputs, new[] { OutputLot(UnusedNumber(), outputKilograms, on, null) });
    }

    public Step PrepareProcess(DateOnly on, decimal outputKilograms, params Part[] inputs) {
        return Prepare(TransformationKind.Process, on, inputs, new[] { OutputLot(UnusedNumber(), outputKilograms, on, null) });
    }

    public Step PrepareSplit(DateOnly on, Part input, params decimal[] outputKilograms) {
        Lot[] outputs = new Lot[outputKilograms.Length];
        for (int i = 0; i < outputs.Length; i++) {
            outputs[i] = OutputLot(input.Lot.Number.SubLot(i + 1), outputKilograms[i], on, input.Lot.Material);
        }

        return Prepare(TransformationKind.Split, on, new[] { input }, outputs);
    }

    /// <summary>Records a prepared step and releases what it produced.</summary>
    public IReadOnlyList<Lot> Apply(Step step) {
        Genealogy.Record(step.Transformation);
        foreach (Lot output in step.Outputs) { output.Release(); }

        return step.Outputs;
    }

    public Lot Blend(DateOnly on, decimal outputKilograms, params Part[] inputs) => Apply(PrepareBlend(on, outputKilograms, inputs))[0];

    public Lot Process(DateOnly on, decimal outputKilograms, params Part[] inputs) => Apply(PrepareProcess(on, outputKilograms, inputs))[0];

    public IReadOnlyList<Lot> Split(DateOnly on, Part input, params decimal[] outputKilograms) => Apply(PrepareSplit(on, input, outputKilograms));

    private Lot OutputLot(LotNumber number, decimal kilograms, DateOnly on, MaterialCode? material) {
        Lot lot = NewLot(number, kilograms, on, material);
        Genealogy.Register(lot);

        return lot;
    }

    private Step Prepare(TransformationKind kind, DateOnly on, Part[] inputs, Lot[] outputs) {
        string id = Any.StringMatching("TRF-[0-9]{6}").Except(_usedTransformationIds).Generate();
        _usedTransformationIds.Add(id);

        Transformation transformation = new Transformation(id,
                                                           kind,
                                                           inputs.Select(part => new LotQuantity(part.Lot.Number, Kg.Of(part.Kilograms))).ToArray(),
                                                           outputs.Select(lot => new LotQuantity(lot.Number, lot.InitialQuantity)).ToArray(),
                                                           on);

        return new Step(transformation, outputs);
    }

}
