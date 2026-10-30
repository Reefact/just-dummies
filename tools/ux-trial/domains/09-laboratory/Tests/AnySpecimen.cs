// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Specimen --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using Laboratory.Tests;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Specimen" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySpecimen : IAny<Specimen> {

    private readonly IAny<SampleId>       _id;
    private readonly IAny<SpecimenType>   _type;
    private readonly IAny<DateTimeOffset> _collectedAt;
    private readonly IAny<DateTimeOffset> _receivedAt;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySpecimen()
        : this(id:          AnyValidId(),
               type:        AnyValidType(),
               collectedAt: AnyValidCollectedAt(),
               receivedAt:  AnyValidReceivedAt()) { }

    private static AnySampleId AnyValidId() {
        return new AnySampleId();
    }

    private static AnyEnum<SpecimenType> AnyValidType() {
        return Any.Enum<SpecimenType>();
    }

    private static AnyDateTimeOffset AnyValidCollectedAt() {
        // receivedAt - collectedAt must lie in [0, 72h]: collection on day one, reception within the next two days.
        return Any.DateTimeOffset().Between(LaboratoryCalendar.CollectionStart, LaboratoryCalendar.CollectionEnd);
    }

    private static AnyDateTimeOffset AnyValidReceivedAt() {
        return Any.DateTimeOffset().Between(LaboratoryCalendar.CollectionEnd, LaboratoryCalendar.ReceptionEnd);
    }

    private AnySpecimen(IAny<SampleId>       id,
                        IAny<SpecimenType>   type,
                        IAny<DateTimeOffset> collectedAt,
                        IAny<DateTimeOffset> receivedAt) {
        _id          = id;
        _type        = type;
        _collectedAt = collectedAt;
        _receivedAt  = receivedAt;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnySpecimen WithId(SampleId value) {
        return new AnySpecimen(new FixedValue<SampleId>(value), _type, _collectedAt, _receivedAt);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpecimen WithId(Func<AnySampleId, AnySampleId> configure) {
        return new AnySpecimen(configure(AnyValidId()), _type, _collectedAt, _receivedAt);
    }

    /// <summary>Pins <c>type</c> to a fixed value.</summary>
    public AnySpecimen WithType(SpecimenType value) {
        return new AnySpecimen(_id, new FixedValue<SpecimenType>(value), _collectedAt, _receivedAt);
    }

    /// <summary>Draws <c>type</c> from <c>AnyValidType()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpecimen WithType(Func<AnyEnum<SpecimenType>, AnyEnum<SpecimenType>> configure) {
        return new AnySpecimen(_id, configure(AnyValidType()), _collectedAt, _receivedAt);
    }

    /// <summary>Pins <c>collectedAt</c> to a fixed value.</summary>
    public AnySpecimen WithCollectedAt(DateTimeOffset value) {
        return new AnySpecimen(_id, _type, new FixedValue<DateTimeOffset>(value), _receivedAt);
    }

    /// <summary>Draws <c>collectedAt</c> from <c>AnyValidCollectedAt()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpecimen WithCollectedAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        return new AnySpecimen(_id, _type, configure(AnyValidCollectedAt()), _receivedAt);
    }

    /// <summary>Pins <c>receivedAt</c> to a fixed value.</summary>
    public AnySpecimen WithReceivedAt(DateTimeOffset value) {
        return new AnySpecimen(_id, _type, _collectedAt, new FixedValue<DateTimeOffset>(value));
    }

    /// <summary>Draws <c>receivedAt</c> from <c>AnyValidReceivedAt()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpecimen WithReceivedAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        return new AnySpecimen(_id, _type, _collectedAt, configure(AnyValidReceivedAt()));
    }

    /// <summary>Produces one arbitrary <see cref="Specimen" />.</summary>
    public Specimen Generate() {
        return new Specimen(_id.Generate(),
                            _type.Generate(),
                            _collectedAt.Generate(),
                            _receivedAt.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
