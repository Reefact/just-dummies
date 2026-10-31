using BatchGenealogy.Domain;

using JustDummies;

namespace BatchGenealogy.Tests.Expert;

/// <summary>
///     A site whose genealogy a test grows step by step. The test states what the genealogy is about
///     — which lots feed which step, and in what quantities — and the plant supplies everything a
///     record needs to be accepted and nothing else matters to: lot numbers, transformation ids,
///     materials, dates, the receipt quantity of a raw material.
/// </summary>
internal sealed class Plant {

    // Raw materials are received in 2024 (AnyLot's manufacturing window, narrowed); every step is
    // performed in 2025, later than any receipt and earlier than any expiry AnyLot draws.
    private static readonly DateOnly LastReceipt     = new(2024, 12, 31);
    private static readonly DateOnly FirstStep       = new(2025, 1, 1);
    private static readonly DateOnly LastStep        = new(2025, 12, 31);

    private readonly HashSet<string> _lotNumbers        = new();
    private readonly HashSet<string> _transformationIds = new();
    private          DateOnly        _lastStep          = FirstStep;

    public Plant() {
        Genealogy = new Domain.BatchGenealogy(Any.SiteCode().Generate());
    }

    public Domain.BatchGenealogy Genealogy { get; }

    public Lot this[LotNumber number] => Genealogy.Get(number);

    /// <summary>A released raw material lot holding at least <paramref name="enoughFor" />.</summary>
    public LotNumber ReleasedRawMaterial(Quantity enoughFor) {
        Lot lot = Any.Lot()
                     .WithNumber(NextLotNumber())
                     .WithInitialQuantity(enoughFor.Add(SomeMore(enoughFor)))
                     .WithManufacturedOn(date => date.BeforeOrEqualTo(LastReceipt))
                     .Generate();
        Genealogy.Register(lot);
        lot.Release();

        return lot.Number;
    }

    /// <summary>Blends the inputs into one new, released lot of <paramref name="output" />.</summary>
    public LotNumber Blend(Quantity output, params (LotNumber Lot, Quantity Quantity)[] inputs) {
        return MakeOne(TransformationKind.Blend, output, inputs);
    }

    /// <summary>Processes the inputs into one new, released lot of <paramref name="output" />.</summary>
    public LotNumber Process(Quantity output, params (LotNumber Lot, Quantity Quantity)[] inputs) {
        return MakeOne(TransformationKind.Process, output, inputs);
    }

    /// <summary>Divides <paramref name="input" /> into released sub-lots of the given sizes, without loss.</summary>
    public IReadOnlyList<LotNumber> Split(LotNumber input, params Quantity[] parts) {
        Lot      source      = Genealogy.Get(input);
        DateOnly performedOn = NextStepDate();
        LotNumber[] subLots = parts.Select((_, index) => source.Number.SubLot(index + 1)).ToArray();
        LotQuantity[] outputs = subLots.Select((subLot, index) => new LotQuantity(subLot, parts[index])).ToArray();
        foreach (LotQuantity output in outputs) {
            Genealogy.Register(Any.Lot()
                                  .WithNumber(output.Lot)
                                  .WithMaterial(source.Material)
                                  .WithInitialQuantity(output.Quantity)
                                  .WithManufacturedOn(performedOn)
                                  .Generate());
        }

        Genealogy.Record(new Transformation(NextTransformationId(), TransformationKind.Split,
                                            [new LotQuantity(input, parts.Skip(1).Aggregate(parts[0], (total, part) => total.Add(part)))], outputs, performedOn));
        foreach (LotNumber subLot in subLots) { Genealogy.Get(subLot).Release(); }

        return subLots;
    }

    /// <summary>A lot registered, unreleased, ready to be named as the output of a step a test records itself.</summary>
    public Lot RegisteredOutput(Quantity quantity, DateOnly manufacturedOn) {
        Lot lot = Any.Lot().WithNumber(NextLotNumber()).WithInitialQuantity(quantity).WithManufacturedOn(manufacturedOn).Generate();
        Genealogy.Register(lot);

        return lot;
    }

    public DateOnly NextStepDate() {
        _lastStep = Any.DateOnly().Between(_lastStep, LastStep).Generate();

        return _lastStep;
    }

    public string NextTransformationId() {
        string id = Unused(Any.TransformationId(), _transformationIds).Generate();
        _transformationIds.Add(id);

        return id;
    }

    private LotNumber MakeOne(TransformationKind kind, Quantity output, (LotNumber Lot, Quantity Quantity)[] inputs) {
        DateOnly performedOn = NextStepDate();
        Lot      product     = RegisteredOutput(output, performedOn);
        Genealogy.Record(new Transformation(NextTransformationId(), kind,
                                            inputs.Select(input => new LotQuantity(input.Lot, input.Quantity)).ToArray(),
                                            [new LotQuantity(product.Number, output)],
                                            performedOn));
        product.Release();

        return product.Number;
    }

    private LotNumber NextLotNumber() {
        LotNumber number = Any.LotNumber().OfAWholeLot(pattern => Unused(pattern, _lotNumbers)).Generate();
        _lotNumbers.Add(number.Value);

        return number;
    }

    // Except() refuses an empty list ("At least one value is required"), so an exclusion set that
    // starts empty cannot be passed as it is.
    private static AnyPattern Unused(AnyPattern pattern, HashSet<string> used) {
        return used.Count == 0 ? pattern : pattern.Except(used.ToArray());
    }

    // A receipt holds what the scenario consumes plus an arbitrary remainder, in the same unit.
    private static Quantity SomeMore(Quantity than) {
        return Any.Quantity().WithUnit(than.Unit).WithAmount(amount => amount.LessThanOrEqualTo(1_000m)).Generate();
    }

}
