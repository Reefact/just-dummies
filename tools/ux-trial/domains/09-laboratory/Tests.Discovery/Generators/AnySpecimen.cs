// Scaffolded by `any` (JustDummies), then completed by hand: the receipt delay is derived from the collection
// instant so that the 72-hour stability window holds by construction (a guard `any` could not read).

using System;

using JustDummies;

namespace Laboratory.Domain;

public sealed partial class AnySpecimen : IAny<Specimen> {

    private IAny<SampleId>     _id;
    private IAny<SpecimenType> _type;

    private AnySampleId           _idRecipe;
    private AnyEnum<SpecimenType> _typeRecipe;
    private AnyDateTimeOffset     _collectedAtRecipe;
    private AnyTimeSpan           _transitRecipe;

    private DateTimeOffset? _collectedAtPin;
    private DateTimeOffset? _receivedAtPin;

    public AnySpecimen()
        : this(id:          new AnySampleId(),
               type:        Any.Enum<SpecimenType>(),
               collectedAt: AnyValidCollectedAt(),
               transit:     AnyValidTransit()) { }

    private static AnyDateTimeOffset AnyValidCollectedAt() {
        return Any.DateTimeOffset().Between(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                            new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    // Time spent between collection and receipt: never negative, never beyond the stability window.
    private static AnyTimeSpan AnyValidTransit() {
        return Any.TimeSpan().Between(TimeSpan.Zero, Specimen.StabilityWindow);
    }

    private AnySpecimen(AnySampleId id, AnyEnum<SpecimenType> type, AnyDateTimeOffset collectedAt, AnyTimeSpan transit) {
        _id                = id;
        _idRecipe          = id;
        _type              = type;
        _typeRecipe        = type;
        _collectedAtRecipe = collectedAt;
        _transitRecipe     = transit;
    }

    private AnySpecimen(AnySpecimen from) {
        _id                = from._id;
        _idRecipe          = from._idRecipe;
        _type              = from._type;
        _typeRecipe        = from._typeRecipe;
        _collectedAtRecipe = from._collectedAtRecipe;
        _transitRecipe     = from._transitRecipe;
        _collectedAtPin    = from._collectedAtPin;
        _receivedAtPin     = from._receivedAtPin;
    }

    public AnySpecimen WithId(SampleId value) {
        AnySpecimen copy = new AnySpecimen(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    public AnySpecimen WithId(Func<AnySampleId, AnySampleId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpecimen copy = new AnySpecimen(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    public AnySpecimen WithType(SpecimenType value) {
        AnySpecimen copy = new AnySpecimen(this);

        copy._type = Any.Fixed(value);

        return copy;
    }

    public AnySpecimen WithType(Func<AnyEnum<SpecimenType>, AnyEnum<SpecimenType>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpecimen copy = new AnySpecimen(this);

        copy._typeRecipe = configure(_typeRecipe);
        copy._type       = copy._typeRecipe;

        return copy;
    }

    public AnySpecimen WithCollectedAt(DateTimeOffset value) {
        AnySpecimen copy = new AnySpecimen(this);

        copy._collectedAtPin = value;

        return copy;
    }

    public AnySpecimen WithCollectedAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpecimen copy = new AnySpecimen(this);

        copy._collectedAtRecipe = configure(_collectedAtRecipe);
        copy._collectedAtPin    = null;

        return copy;
    }

    public AnySpecimen WithReceivedAt(DateTimeOffset value) {
        AnySpecimen copy = new AnySpecimen(this);

        copy._receivedAtPin = value;

        return copy;
    }

    /// <summary>Refines the time spent between collection and receipt, which the stability window already bounds.</summary>
    public AnySpecimen WithTransit(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpecimen copy = new AnySpecimen(this);

        copy._transitRecipe = configure(_transitRecipe);

        return copy;
    }

    public Specimen Generate() {
        DateTimeOffset collectedAt;
        DateTimeOffset receivedAt;

        if (_receivedAtPin is { } pinnedReceivedAt && _collectedAtPin is null) {
            receivedAt  = pinnedReceivedAt;
            collectedAt = receivedAt - _transitRecipe.Generate();
        } else {
            collectedAt = _collectedAtPin ?? _collectedAtRecipe.Generate();
            receivedAt  = _receivedAtPin ?? collectedAt + _transitRecipe.Generate();
        }

        return new Specimen(_id.Generate(), _type.Generate(), collectedAt, receivedAt);
    }

}
