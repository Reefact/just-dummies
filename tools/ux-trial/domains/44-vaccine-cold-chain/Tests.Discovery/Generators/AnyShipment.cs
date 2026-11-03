// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Freight.Domain.Shipment --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

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
    private IAny<IReadOnlyList<Leg>> _legsRecipe;

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
        // 26 500 kg is the smallest payload of any container type, so the weight fits whichever type is drawn.
        return new AnyWeight().WithKilograms(k => k.LessThanOrEqualTo(26_500.0));
    }

    private static AnyVolume AnyValidVolume() {
        // 33.2 m3 is the smallest capacity of any container type.
        return new AnyVolume().WithCubicMetres(v => v.LessThanOrEqualTo(33.2));
    }

    private static IAny<IReadOnlyList<Leg>> AnyValidLegs() {
        // An itinerary of one to four legs, each one starting where the previous one ended and no earlier than its arrival.
        return Any.Combine(
            Any.ListOf(new AnyUnLocode()).WithCountBetween(2, 5).Distinct(),
            Any.DateTimeOffset().Between(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2035, 12, 31, 0, 0, 0, TimeSpan.Zero)),
            Any.ListOf(Any.TimeSpan().Between(TimeSpan.FromHours(1), TimeSpan.FromDays(30)).WithGranularity(TimeSpan.FromMinutes(1))).WithCount(4),
            Any.ListOf(Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromDays(3)).WithGranularity(TimeSpan.FromMinutes(1))).WithCount(4),
            (ports, firstDeparture, transits, dwells) => {
                List<Leg>      legs      = new List<Leg>();
                DateTimeOffset departure = firstDeparture;
                for (int index = 0; index < ports.Count - 1; index++) {
                    Leg leg = new Leg(ports[index], ports[index + 1], departure, departure + transits[index]);
                    legs.Add(leg);
                    departure = leg.Arrival + dwells[index];
                }

                return (IReadOnlyList<Leg>)legs;
            });
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyShipment(AnyGuid                id,
                        AnyContainerCode       container,
                        AnyEnum<ContainerType> containerType,
                        AnyWeight              grossWeight,
                        AnyVolume              volume,
                        IAny<IReadOnlyList<Leg>> legs) {
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

        copy._containerType = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>containerType</c> is drawn from — <c>AnyValidContainerType()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;ContainerType&gt;, AnyEnum&lt;ContainerType&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShipment WithContainerType(Func<AnyEnum<ContainerType>, AnyEnum<ContainerType>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShipment copy = new AnyShipment(this);

        copy._containerTypeRecipe = configure(_containerTypeRecipe);
        copy._containerType       = copy._containerTypeRecipe;

        return copy;
    }

    /// <summary>Pins <c>grossWeight</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShipment WithGrossWeight(Weight value) {
        AnyShipment copy = new AnyShipment(this);

        copy._grossWeight = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>grossWeight</c> is drawn from — <c>AnyValidGrossWeight()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyWeight, AnyWeight&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShipment WithGrossWeight(Func<AnyWeight, AnyWeight> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShipment copy = new AnyShipment(this);

        copy._grossWeightRecipe = configure(_grossWeightRecipe);
        copy._grossWeight       = copy._grossWeightRecipe;

        return copy;
    }

    /// <summary>Pins <c>volume</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShipment WithVolume(Volume value) {
        AnyShipment copy = new AnyShipment(this);

        copy._volume = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>volume</c> is drawn from — <c>AnyValidVolume()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyVolume, AnyVolume&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShipment WithVolume(Func<AnyVolume, AnyVolume> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShipment copy = new AnyShipment(this);

        copy._volumeRecipe = configure(_volumeRecipe);
        copy._volume       = copy._volumeRecipe;

        return copy;
    }

    /// <summary>Pins <c>legs</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShipment WithLegs(IReadOnlyList<Leg> value) {
        AnyShipment copy = new AnyShipment(this);

        copy._legs = Any.Fixed(value);

        return copy;
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

}
