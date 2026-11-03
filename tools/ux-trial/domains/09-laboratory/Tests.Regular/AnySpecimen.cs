// Scaffolded by `any` (JustDummies), then completed by hand. This file is yours: read it, edit it, commit it.
// The scaffold drew collectedAt and receivedAt as an ordered pair, which does not know the 72-hour stability
// window; this version draws the collection instant, then a transit time within the window, and derives the
// reception from them. `any generate Specimen --force` would overwrite it.

using System;

using JustDummies;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Specimen" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySpecimen : IAny<Specimen> {

    private IAny<SampleId>     _id;
    private IAny<SpecimenType> _type;
    private IAny<TimeSpan>     _transit;

    private AnySampleId           _idRecipe;
    private AnyEnum<SpecimenType> _typeRecipe;
    private AnyDateTimeOffset     _collectedAtRecipe;
    private AnyTimeSpan           _transitRecipe;

    private DateTimeOffset? _collectedAtPin;
    private DateTimeOffset? _receivedAtPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
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
        // Bounded away from the ends of the type: the reception is collectedAt plus up to 72 hours.
        return Any.DateTimeOffset().Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                            new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    private static AnyTimeSpan AnyValidTransit() {
        return Any.TimeSpan().Between(TimeSpan.Zero, Specimen.StabilityWindow);
    }

    private AnySpecimen(AnySampleId           id,
                        AnyEnum<SpecimenType> type,
                        AnyDateTimeOffset     collectedAt,
                        AnyTimeSpan           transit) {
        _id                = id;
        _idRecipe          = id;
        _type              = type;
        _typeRecipe        = type;
        _collectedAtRecipe = collectedAt;
        _transit           = transit;
        _transitRecipe     = transit;
    }

    private AnySpecimen(AnySpecimen from) {
        _id                = from._id;
        _idRecipe          = from._idRecipe;
        _type              = from._type;
        _typeRecipe        = from._typeRecipe;
        _collectedAtRecipe = from._collectedAtRecipe;
        _collectedAtPin    = from._collectedAtPin;
        _receivedAtPin     = from._receivedAtPin;
        _transit           = from._transit;
        _transitRecipe     = from._transitRecipe;
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

    /// <summary>Pins the collection instant; the reception follows it by a transit within the window, unless it is pinned too.</summary>
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

    /// <summary>Pins the reception instant; the collection precedes it by a transit within the window, unless it is pinned too.</summary>
    public AnySpecimen WithReceivedAt(DateTimeOffset value) {
        AnySpecimen copy = new AnySpecimen(this);

        copy._receivedAtPin = value;

        return copy;
    }

    /// <summary>Pins the transit time (collection to reception).</summary>
    public AnySpecimen WithTransit(TimeSpan value) {
        AnySpecimen copy = new AnySpecimen(this);

        copy._transit = Any.Fixed(value);

        return copy;
    }

    public AnySpecimen WithTransit(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpecimen copy = new AnySpecimen(this);

        copy._transitRecipe = configure(_transitRecipe);
        copy._transit       = copy._transitRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Specimen" />.</summary>
    public Specimen Generate() {
        DateTimeOffset collectedAt;
        DateTimeOffset receivedAt;

        if (_collectedAtPin is { } pinnedCollection && _receivedAtPin is { } pinnedReception) {
            collectedAt = pinnedCollection;
            receivedAt  = pinnedReception;
        } else if (_receivedAtPin is { } reception) {
            receivedAt  = reception;
            collectedAt = reception - _transit.Generate();
        } else {
            collectedAt = _collectedAtPin ?? _collectedAtRecipe.Generate();
            receivedAt  = collectedAt + _transit.Generate();
        }

        return new Specimen(_id.Generate(),
                            _type.Generate(),
                            collectedAt,
                            receivedAt);
    }

}
