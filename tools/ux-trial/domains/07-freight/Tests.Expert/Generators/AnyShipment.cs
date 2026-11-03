// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Shipment --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;
using System.Linq;

using Freight.Domain;
using JustDummies;

namespace Freight.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Shipment" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyShipment : IAny<Shipment> {

    private IAny<Guid>               _id;
    private IAny<ContainerCode>      _container;
    private IAny<ContainerType>      _containerType;
    private IAny<Weight>             _grossWeight;
    private IAny<Volume>             _volume;
    private IAny<IReadOnlyList<Leg>> _legs;

    private AnyGuid                _idRecipe;
    private AnyContainerCode       _containerRecipe;
    private AnyEnum<ContainerType> _containerTypeRecipe;
    private AnyWeight              _grossWeightRecipe;
    private AnyVolume              _volumeRecipe;
    private AnyItinerary            _legsRecipe;

    private ContainerType? _containerTypePin;
    private Weight?        _grossWeightPin;
    private Volume?        _volumePin;

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
        return new AnyWeight();
    }

    private static AnyVolume AnyValidVolume() {
        return new AnyVolume();
    }

    private static AnyItinerary AnyValidLegs() {
        return new AnyItinerary();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyShipment(AnyGuid                id,
                        AnyContainerCode       container,
                        AnyEnum<ContainerType> containerType,
                        AnyWeight              grossWeight,
                        AnyVolume              volume,
                        AnyItinerary            legs) {
        _id                  = id;
        _idRecipe            = id;
        _container           = container;
        _containerRecipe     = container;
        _containerType       = containerType;
        _containerTypeRecipe = containerType;
        _grossWeight         = grossWeight;
        _grossWeightRecipe   = grossWeight;
        _volume              = volume;
        _volumeRecipe        = volume;
        _legs                = legs;
        _legsRecipe          = legs;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyShipment(AnyShipment from) {
        _id                  = from._id;
        _idRecipe            = from._idRecipe;
        _container           = from._container;
        _containerRecipe     = from._containerRecipe;
        _containerType       = from._containerType;
        _containerTypeRecipe = from._containerTypeRecipe;
        _grossWeight         = from._grossWeight;
        _grossWeightRecipe   = from._grossWeightRecipe;
        _volume              = from._volume;
        _volumeRecipe        = from._volumeRecipe;
        _legs                = from._legs;
        _legsRecipe          = from._legsRecipe;
        _containerTypePin    = from._containerTypePin;
        _grossWeightPin      = from._grossWeightPin;
        _volumePin           = from._volumePin;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShipment WithId(Guid value) {
        AnyShipment copy = new AnyShipment(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGuid, AnyGuid&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShipment WithId(Func<AnyGuid, AnyGuid> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShipment copy = new AnyShipment(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>container</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShipment WithContainer(ContainerCode value) {
        AnyShipment copy = new AnyShipment(this);

        copy._container = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>container</c> is drawn from — <c>AnyValidContainer()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyContainerCode, AnyContainerCode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShipment WithContainer(Func<AnyContainerCode, AnyContainerCode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShipment copy = new AnyShipment(this);

        copy._containerRecipe = configure(_containerRecipe);
        copy._container       = copy._containerRecipe;

        return copy;
    }

    /// <summary>Pins <c>containerType</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShipment WithContainerType(ContainerType value) {
        AnyShipment copy = new AnyShipment(this);

        copy._containerType    = Any.Fixed(value);
        copy._containerTypePin = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>containerType</c> is drawn from — <c>AnyValidContainerType()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;ContainerType&gt;, AnyEnum&lt;ContainerType&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShipment WithContainerType(Func<AnyEnum<ContainerType>, AnyEnum<ContainerType>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShipment copy = new AnyShipment(this);

        copy._containerTypeRecipe = configure(_containerTypeRecipe);
        copy._containerType       = copy._containerTypeRecipe;
        copy._containerTypePin    = null;

        return copy;
    }

    /// <summary>Pins <c>grossWeight</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShipment WithGrossWeight(Weight value) {
        AnyShipment copy = new AnyShipment(this);

        copy._grossWeight    = Any.Fixed(value);
        copy._grossWeightPin = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>grossWeight</c> is drawn from — <c>AnyValidGrossWeight()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyWeight, AnyWeight&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShipment WithGrossWeight(Func<AnyWeight, AnyWeight> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShipment copy = new AnyShipment(this);

        copy._grossWeightRecipe = configure(_grossWeightRecipe);
        copy._grossWeight       = copy._grossWeightRecipe;
        copy._grossWeightPin    = null;

        return copy;
    }

    /// <summary>Pins <c>volume</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShipment WithVolume(Volume value) {
        AnyShipment copy = new AnyShipment(this);

        copy._volume    = Any.Fixed(value);
        copy._volumePin = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>volume</c> is drawn from — <c>AnyValidVolume()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyVolume, AnyVolume&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShipment WithVolume(Func<AnyVolume, AnyVolume> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShipment copy = new AnyShipment(this);

        copy._volumeRecipe = configure(_volumeRecipe);
        copy._volume       = copy._volumeRecipe;
        copy._volumePin    = null;

        return copy;
    }

    /// <summary>Pins <c>legs</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShipment WithLegs(IReadOnlyList<Leg> value) {
        AnyShipment copy = new AnyShipment(this);

        copy._legs = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>legs</c> is drawn from — <c>AnyValidLegs()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyItinerary, AnyItinerary&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShipment WithLegs(Func<AnyItinerary, AnyItinerary> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShipment copy = new AnyShipment(this);

        copy._legsRecipe = configure(_legsRecipe);
        copy._legs       = copy._legsRecipe;

        return copy;
    }

    /// <summary>
    ///     Produces one arbitrary <see cref="Shipment" />. The load is tied to the container type, so the type is
    ///     drawn first (among those a pinned weight or volume fits in), then the weight and the volume within
    ///     what the type carries.
    /// </summary>
    public Shipment Generate() {
        ContainerType containerType = DrawContainerType();

        Weight grossWeight = _grossWeightPin ?? _grossWeightRecipe.WithKilograms(k => k.LessThanOrEqualTo(containerType.MaxPayloadKilograms())).Generate();
        Volume volume      = _volumePin ?? _volumeRecipe.WithCubicMetres(c => c.LessThanOrEqualTo(containerType.CapacityCubicMetres())).Generate();

        return new Shipment(_id.Generate(),
                            _container.Generate(),
                            containerType,
                            grossWeight,
                            volume,
                            _legs.Generate());
    }

    private ContainerType DrawContainerType() {
        if (_containerTypePin is { } pinned) { return pinned; }

        ContainerType[] tooSmall = Enum.GetValues<ContainerType>()
                                       .Where(type => (_grossWeightPin is { } weight && weight.Kilograms > type.MaxPayloadKilograms())
                                                   || (_volumePin is { } volume && volume.CubicMetres > type.CapacityCubicMetres()))
                                       .ToArray();

        return _containerTypeRecipe.Except(tooSmall).Generate();
    }

}
