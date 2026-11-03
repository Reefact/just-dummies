// Scaffolded by `any` (JustDummies), then rewritten by hand: kind, inputs and outputs are tied by the
// domain (arity per kind, sub-lots for a split, one dimension, output <= input), which no
// per-parameter recipe can honour, so Generate() derives them from the kind (see "When a guard ties
// two parameters" in the CLI documentation). `any generate Transformation --force` would lose this.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>
///     A generator of arbitrary, internally coherent <see cref="Transformation" /> values: whatever the
///     kind, the inputs and outputs have the arity it demands, share one dimension and never produce more
///     than they consume. Inputs and outputs can be pinned; when they are not, they are derived from the kind.
/// </summary>
public sealed partial class AnyTransformation : IAny<Transformation> {

    private IAny<string>                      _id;
    private IAny<TransformationKind>          _kind;
    private IAny<DateOnly>                    _performedOn;
    private IReadOnlyList<LotQuantity>?       _inputsPin;
    private IReadOnlyList<LotQuantity>?       _outputsPin;

    private AnyPattern                  _idRecipe;
    private AnyEnum<TransformationKind> _kindRecipe;
    private AnyDateOnly                 _performedOnRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTransformation()
        : this(id:          AnyValidId(),
               kind:        AnyValidKind(),
               performedOn: AnyValidPerformedOn()) { }

    private static AnyPattern AnyValidId() {
        return Any.StringMatching(@"TRF-[0-9]{6}");
    }

    private static AnyEnum<TransformationKind> AnyValidKind() {
        return Any.Enum<TransformationKind>();
    }

    private static AnyDateOnly AnyValidPerformedOn() {
        return Any.DateOnly();
    }

    private AnyTransformation(AnyPattern id, AnyEnum<TransformationKind> kind, AnyDateOnly performedOn) {
        _id                = id;
        _idRecipe          = id;
        _kind              = kind;
        _kindRecipe        = kind;
        _performedOn       = performedOn;
        _performedOnRecipe = performedOn;
    }

    private AnyTransformation(AnyTransformation from) {
        _id                = from._id;
        _idRecipe          = from._idRecipe;
        _kind              = from._kind;
        _kindRecipe        = from._kindRecipe;
        _performedOn       = from._performedOn;
        _performedOnRecipe = from._performedOnRecipe;
        _inputsPin         = from._inputsPin;
        _outputsPin        = from._outputsPin;
    }

    public AnyTransformation WithId(string value) {
        AnyTransformation copy = new AnyTransformation(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    public AnyTransformation WithId(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransformation copy = new AnyTransformation(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    public AnyTransformation WithKind(TransformationKind value) {
        AnyTransformation copy = new AnyTransformation(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    public AnyTransformation WithKind(Func<AnyEnum<TransformationKind>, AnyEnum<TransformationKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransformation copy = new AnyTransformation(this);

        copy._kindRecipe = configure(_kindRecipe);
        copy._kind       = copy._kindRecipe;

        return copy;
    }

    public AnyTransformation WithPerformedOn(DateOnly value) {
        AnyTransformation copy = new AnyTransformation(this);

        copy._performedOn = Any.Fixed(value);

        return copy;
    }

    public AnyTransformation WithPerformedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransformation copy = new AnyTransformation(this);

        copy._performedOnRecipe = configure(_performedOnRecipe);
        copy._performedOn       = copy._performedOnRecipe;

        return copy;
    }

    /// <summary>Pins the inputs; the outputs are then pinned as well, since they must fit them.</summary>
    public AnyTransformation WithInputsAndOutputs(IReadOnlyList<LotQuantity> inputs, IReadOnlyList<LotQuantity> outputs) {
        AnyTransformation copy = new AnyTransformation(this);

        copy._inputsPin  = inputs;
        copy._outputsPin = outputs;

        return copy;
    }

    public Transformation Generate() {
        TransformationKind kind = _kind.Generate();

        if (_inputsPin is not null && _outputsPin is not null) {
            return new Transformation(_id.Generate(), kind, _inputsPin, _outputsPin, _performedOn.Generate());
        }

        (IReadOnlyList<LotQuantity> inputs, IReadOnlyList<LotQuantity> outputs) = DrawParts(kind);

        return new Transformation(_id.Generate(), kind, inputs, outputs, _performedOn.Generate());
    }

    private static (IReadOnlyList<LotQuantity> Inputs, IReadOnlyList<LotQuantity> Outputs) DrawParts(TransformationKind kind) {
        QuantityUnit unit        = Any.Enum<QuantityUnit>().Generate();
        int          inputCount  = kind switch { TransformationKind.Split => 1, TransformationKind.Blend => Any.Int32().Between(2, 4).Generate(), _ => Any.Int32().Between(1, 3).Generate() };
        int          outputCount = kind switch { TransformationKind.Split => Any.Int32().Between(2, 4).Generate(), _ => 1 };

        List<LotNumber> inputLots;
        List<LotNumber> outputLots;
        if (kind == TransformationKind.Split) {
            LotNumber source = new LotNumber(new AnyLotNumber().Generate().BaseLot);
            inputLots  = [source];
            outputLots = Any.SetOf(Any.Int32().Between(1, 99)).WithCount(outputCount).Generate().Select(source.SubLot).ToList();
        } else {
            List<LotNumber> lots = Any.ListOf(new AnyLotNumber()).WithCount(inputCount + 1).Distinct().Generate();
            inputLots  = lots.Take(inputCount).ToList();
            outputLots = lots.Skip(inputCount).ToList();
        }

        List<LotQuantity> inputs = inputLots.Select(lot => new LotQuantity(lot, new Quantity(Any.Decimal().Between(1m, 1_000m).WithScale(3).Generate(), unit))).ToList();
        decimal           total  = inputs.Sum(input => input.Quantity.Amount);
        decimal           ceiling = Math.Floor(total / outputCount * 1_000m) / 1_000m;

        List<LotQuantity> outputs = outputLots.Select(lot => new LotQuantity(lot, new Quantity(Any.Decimal().Between(0.001m, ceiling).WithScale(3).Generate(), unit))).ToList();

        return (inputs, outputs);
    }

}
