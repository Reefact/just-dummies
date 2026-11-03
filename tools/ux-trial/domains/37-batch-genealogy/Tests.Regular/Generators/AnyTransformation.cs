using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Transformation" /> values. Hand-written from the scaffold: a
///     transformation's arity, its sub-lots and its output ceiling all depend on its kind and on each
///     other, so inputs and outputs are drawn together, in <see cref="Generate" />, rather than
///     parameter by parameter. Quantities are whole numbers of a unit, so the output never exceeds the input.
/// </summary>
public sealed partial class AnyTransformation : IAny<Transformation> {

    private static readonly QuantityUnit[] MassUnits   = { QuantityUnit.Milligram, QuantityUnit.Gram, QuantityUnit.Kilogram };
    private static readonly QuantityUnit[] VolumeUnits = { QuantityUnit.Millilitre, QuantityUnit.Litre };

    private IAny<string>             _id          = Any.StringMatching("TRF-[0-9]{6}");
    private IAny<TransformationKind> _kind        = Any.Enum<TransformationKind>();
    private IAny<DateOnly>           _performedOn = Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2029, 12, 31));
    private bool                     _oneUnit;

    public AnyTransformation() { }

    private AnyTransformation(AnyTransformation from) {
        _id          = from._id;
        _kind        = from._kind;
        _performedOn = from._performedOn;
        _oneUnit     = from._oneUnit;
    }

    public AnyTransformation WithId(string value) {
        AnyTransformation copy = new AnyTransformation(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    public AnyTransformation WithKind(TransformationKind value) {
        AnyTransformation copy = new AnyTransformation(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    public AnyTransformation WithPerformedOn(DateOnly value) {
        AnyTransformation copy = new AnyTransformation(this);

        copy._performedOn = Any.Fixed(value);

        return copy;
    }

    /// <summary>Every input of the step is measured in the same unit, drawn once.</summary>
    public AnyTransformation InputsInOneUnit() {
        AnyTransformation copy = new AnyTransformation(this);

        copy._oneUnit = true;

        return copy;
    }

    /// <summary>Produces one arbitrary, valid <see cref="Transformation" />.</summary>
    public Transformation Generate() {
        TransformationKind kind  = _kind.Generate();
        bool               mass  = Any.Boolean().Generate();
        QuantityUnit[]     units = mass ? MassUnits : VolumeUnits;
        QuantityUnit       small = units[0];
        QuantityUnit       only  = Any.OneOf(units).Generate();

        int inputCount = kind switch {
            TransformationKind.Blend => Any.Int32().Between(2, 4).Generate(),
            TransformationKind.Split => 1,
            _                        => Any.Int32().Between(1, 3).Generate()
        };
        int outputCount = kind == TransformationKind.Split ? Any.Int32().Between(2, 4).Generate() : 1;

        int                  distinctLots = kind == TransformationKind.Split ? 1 : inputCount + outputCount;
        IReadOnlyList<LotNumber> lots         = Any.ListOf(new AnyLotNumber()).WithCount(distinctLots).Distinct().Generate();
        List<LotNumber>      inputLots    = lots.Take(inputCount).ToList();
        List<LotNumber>      outputLots   = new List<LotNumber>();
        if (kind == TransformationKind.Split) {
            LotNumber source  = inputLots[0];
            int       exclude = source.SubLotIndex ?? 0;
            foreach (int index in Any.SetOf(Any.Int32().Between(1, 99).Except(exclude)).WithCount(outputCount).Generate()) {
                outputLots.Add(source.SubLot(index));
            }
        } else {
            outputLots.AddRange(lots.Skip(inputCount));
        }

        List<LotQuantity> inputs = inputLots
                                   .Select(lot => new LotQuantity(lot, new Quantity(Any.Int32().Between(10, 10_000).Generate(), _oneUnit ? only : Any.OneOf(units).Generate())))
                                   .ToList();

        decimal totalInSmallestUnit = inputs.Sum(input => input.Quantity.BaseAmount) / new Quantity(1m, small).BaseAmount;
        decimal ceilingPerOutput    = Math.Min(Math.Floor(totalInSmallestUnit / outputCount), Quantity.MaximumAmount);
        List<LotQuantity> outputs = outputLots
                                    .Select(lot => new LotQuantity(lot, new Quantity(Any.Int64().Between(1L, (long)ceilingPerOutput).Generate(), small)))
                                    .ToList();

        return new Transformation(_id.Generate(), kind, inputs, outputs, _performedOn.Generate());
    }

}
