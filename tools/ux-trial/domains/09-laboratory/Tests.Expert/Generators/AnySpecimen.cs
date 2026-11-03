using System;

using JustDummies;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Specimen" /> values. Started from the scaffold, then reworked by hand:
///     the received instant is not drawn on its own, it follows the collection by a transit time inside the
///     stability window, which is the only way "received after collected, within 72 hours" holds by construction.
/// </summary>
public sealed partial class AnySpecimen : IAny<Specimen> {

    private IAny<SampleId>        _id;
    private IAny<SpecimenType>    _type;
    private IAny<DateTimeOffset>  _collectedAt;
    private IAny<TimeSpan>        _transit;

    private AnySampleId           _idRecipe;
    private AnyEnum<SpecimenType> _typeRecipe;

    private DateTimeOffset? _collectedAtPin;
    private DateTimeOffset? _receivedAtPin;

    public AnySpecimen()
        : this(id:          AnyValidId(),
               type:        AnyValidType(),
               collectedAt: AnyValidCollectedAt(),
               transit:     AnyValidTransit()) { }

    private static AnySampleId AnyValidId() {
        return new AnySampleId();
    }

    private static AnyEnum<SpecimenType> AnyValidType() {
        return Any.Enum<SpecimenType>();
    }

    private static AnyDateTimeOffset AnyValidCollectedAt() {
        return Any.DateTimeOffset().Between(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                            new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    private static AnyTimeSpan AnyValidTransit() {
        return Any.TimeSpan().Between(TimeSpan.Zero, Specimen.StabilityWindow);
    }

    private AnySpecimen(AnySampleId           id,
                        AnyEnum<SpecimenType> type,
                        AnyDateTimeOffset     collectedAt,
                        AnyTimeSpan           transit) {
        _id         = id;
        _idRecipe   = id;
        _type       = type;
        _typeRecipe = type;
        _collectedAt = collectedAt;
        _transit    = transit;
    }

    private AnySpecimen(AnySpecimen from) {
        _id            = from._id;
        _idRecipe      = from._idRecipe;
        _type          = from._type;
        _typeRecipe    = from._typeRecipe;
        _collectedAt   = from._collectedAt;
        _transit       = from._transit;
        _collectedAtPin = from._collectedAtPin;
        _receivedAtPin = from._receivedAtPin;
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

    /// <summary>Pins the collection; the reception still follows it by a transit time.</summary>
    public AnySpecimen WithCollectedAt(DateTimeOffset value) {
        AnySpecimen copy = new AnySpecimen(this);

        copy._collectedAtPin = value;

        return copy;
    }

    /// <summary>Pins the reception; the collection then precedes it by a transit time (unless it is pinned too).</summary>
    public AnySpecimen WithReceivedAt(DateTimeOffset value) {
        AnySpecimen copy = new AnySpecimen(this);

        copy._receivedAtPin = value;

        return copy;
    }

    /// <summary>Pins how long the specimen travelled.</summary>
    public AnySpecimen WithTransit(TimeSpan value) {
        AnySpecimen copy = new AnySpecimen(this);

        copy._transit = Any.Fixed(value);

        return copy;
    }

    public Specimen Generate() {
        TimeSpan transit = _transit.Generate();

        DateTimeOffset collectedAt;
        DateTimeOffset receivedAt;
        if (_receivedAtPin is { } pinnedReceivedAt) {
            receivedAt  = pinnedReceivedAt;
            collectedAt = _collectedAtPin ?? receivedAt - transit;
        } else {
            collectedAt = _collectedAtPin ?? _collectedAt.Generate();
            receivedAt  = collectedAt + transit;
        }

        return new Specimen(_id.Generate(), _type.Generate(), collectedAt, receivedAt);
    }

}
