// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Shipment --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Freight.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Shipment" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyShipment : IAny<Shipment> {

    private readonly IAny<Guid>               _id;
    private readonly IAny<ContainerCode>      _container;
    private readonly IAny<ContainerType>      _containerType;
    private readonly IAny<Weight>             _grossWeight;
    private readonly IAny<Volume>             _volume;
    private readonly IAny<IReadOnlyList<Leg>> _legs;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyShipment()
        : this(id:            AnyValidId(),
               container:     AnyValidContainer(),
               containerType: AnyValidContainerType(),
               grossWeight:   AnyValidGrossWeight(),
               volume:        AnyValidVolume(),
               legs:          AnyValidLegs()) { }

    private static AnyGuid AnyValidId() {
        return Any.Guid().NonEmpty();
    }

    private static AnyContainerCode AnyValidContainer() {
        return new AnyContainerCode();
    }

    private static AnyEnum<ContainerType> AnyValidContainerType() {
        return Any.Enum<ContainerType>();
    }

    private static AnyWeight AnyValidGrossWeight() {
        // Cross-field rule (payload depends on the container type): stay within the smallest payload of all types.
        return new AnyWeight().WithKilograms(kilograms => kilograms.LessThanOrEqualTo(ContainerType.HighCube40.MaxPayloadKilograms()));
    }

    private static AnyVolume AnyValidVolume() {
        // Cross-field rule (capacity depends on the container type): stay within the smallest capacity of all types.
        return new AnyVolume().WithCubicMetres(cubicMetres => cubicMetres.LessThanOrEqualTo(ContainerType.Dry20.CapacityCubicMetres()));
    }

    private static AnyItinerary AnyValidLegs() {
        return new AnyItinerary();
    }

    private AnyShipment(IAny<Guid>               id,
                        IAny<ContainerCode>      container,
                        IAny<ContainerType>      containerType,
                        IAny<Weight>             grossWeight,
                        IAny<Volume>             volume,
                        IAny<IReadOnlyList<Leg>> legs) {
        _id            = id;
        _container     = container;
        _containerType = containerType;
        _grossWeight   = grossWeight;
        _volume        = volume;
        _legs          = legs;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyShipment WithId(Guid value) {
        return new AnyShipment(new FixedValue<Guid>(value), _container, _containerType, _grossWeight, _volume, _legs);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShipment WithId(Func<AnyGuid, AnyGuid> configure) {
        return new AnyShipment(configure(AnyValidId()), _container, _containerType, _grossWeight, _volume, _legs);
    }

    /// <summary>Pins <c>container</c> to a fixed value.</summary>
    public AnyShipment WithContainer(ContainerCode value) {
        return new AnyShipment(_id, new FixedValue<ContainerCode>(value), _containerType, _grossWeight, _volume, _legs);
    }

    /// <summary>Draws <c>container</c> from <c>AnyValidContainer()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShipment WithContainer(Func<AnyContainerCode, AnyContainerCode> configure) {
        return new AnyShipment(_id, configure(AnyValidContainer()), _containerType, _grossWeight, _volume, _legs);
    }

    /// <summary>Pins <c>containerType</c> to a fixed value.</summary>
    public AnyShipment WithContainerType(ContainerType value) {
        return new AnyShipment(_id, _container, new FixedValue<ContainerType>(value), _grossWeight, _volume, _legs);
    }

    /// <summary>Draws <c>containerType</c> from <c>AnyValidContainerType()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShipment WithContainerType(Func<AnyEnum<ContainerType>, AnyEnum<ContainerType>> configure) {
        return new AnyShipment(_id, _container, configure(AnyValidContainerType()), _grossWeight, _volume, _legs);
    }

    /// <summary>Pins <c>grossWeight</c> to a fixed value.</summary>
    public AnyShipment WithGrossWeight(Weight value) {
        return new AnyShipment(_id, _container, _containerType, new FixedValue<Weight>(value), _volume, _legs);
    }

    /// <summary>Draws <c>grossWeight</c> from <c>AnyValidGrossWeight()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShipment WithGrossWeight(Func<AnyWeight, AnyWeight> configure) {
        return new AnyShipment(_id, _container, _containerType, configure(AnyValidGrossWeight()), _volume, _legs);
    }

    /// <summary>Pins <c>volume</c> to a fixed value.</summary>
    public AnyShipment WithVolume(Volume value) {
        return new AnyShipment(_id, _container, _containerType, _grossWeight, new FixedValue<Volume>(value), _legs);
    }

    /// <summary>Draws <c>volume</c> from <c>AnyValidVolume()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShipment WithVolume(Func<AnyVolume, AnyVolume> configure) {
        return new AnyShipment(_id, _container, _containerType, _grossWeight, configure(AnyValidVolume()), _legs);
    }

    /// <summary>Pins <c>legs</c> to a fixed value.</summary>
    public AnyShipment WithLegs(IReadOnlyList<Leg> value) {
        return new AnyShipment(_id, _container, _containerType, _grossWeight, _volume, new FixedValue<IReadOnlyList<Leg>>(value));
    }

    /// <summary>Draws <c>legs</c> from <c>AnyValidLegs()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShipment WithLegs(Func<AnyItinerary, AnyItinerary> configure) {
        return new AnyShipment(_id, _container, _containerType, _grossWeight, _volume, configure(AnyValidLegs()));
    }

    /// <summary>Produces one arbitrary <see cref="Shipment" />.</summary>
    public Shipment Generate() {
        return new Shipment(_id.Generate(),
                            _container.Generate(),
                            _containerType.Generate(),
                            _grossWeight.Generate(),
                            _volume.Generate(),
                            _legs.Generate());
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
