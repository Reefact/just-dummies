// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ReceivingWarehouse --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using ColdChain.Domain;
using Freight.Domain;
using JustDummies;
using Warehouse.Domain;

namespace ColdChain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="ReceivingWarehouse" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyReceivingWarehouse : IAny<ReceivingWarehouse> {

    private IAny<UnLocode>         _location;
    private IAny<CustodianId>      _operatorId;
    private IAny<TemperatureRange> _coldRoom;
    private IAny<BinLocation>      _quarantineBay;

    private AnyUnLocode         _locationRecipe;
    private AnyCustodianId      _operatorIdRecipe;
    private AnyTemperatureRange _coldRoomRecipe;
    private AnyBinLocation      _quarantineBayRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyReceivingWarehouse()
        : this(location:      AnyValidLocation(),
               operatorId:    AnyValidOperatorId(),
               coldRoom:      AnyValidColdRoom(),
               quarantineBay: AnyValidQuarantineBay()) { }

    private static AnyUnLocode AnyValidLocation() {
        return new AnyUnLocode();
    }

    private static AnyCustodianId AnyValidOperatorId() {
        return new AnyCustodianId();
    }

    private static AnyTemperatureRange AnyValidColdRoom() {
        return new AnyTemperatureRange().WithMinCelsius(min => min.GreaterThanOrEqualTo(VaccineProduct.FreezeLimitCelsius).WithScale(1)).WithMaxCelsius(max => max.LessThanOrEqualTo(VaccineProduct.ColdChainMaxCelsius).WithScale(1));
    }

    private static AnyBinLocation AnyValidQuarantineBay() {
        return new AnyBinLocation().WithLevel(level => level.LessThanOrEqualTo(4));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyReceivingWarehouse(AnyUnLocode         location,
                                  AnyCustodianId      operatorId,
                                  AnyTemperatureRange coldRoom,
                                  AnyBinLocation      quarantineBay) {
        _location            = location;
        _locationRecipe      = location;
        _operatorId          = operatorId;
        _operatorIdRecipe    = operatorId;
        _coldRoom            = coldRoom;
        _coldRoomRecipe      = coldRoom;
        _quarantineBay       = quarantineBay;
        _quarantineBayRecipe = quarantineBay;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyReceivingWarehouse(AnyReceivingWarehouse from) {
        _location            = from._location;
        _locationRecipe      = from._locationRecipe;
        _operatorId          = from._operatorId;
        _operatorIdRecipe    = from._operatorIdRecipe;
        _coldRoom            = from._coldRoom;
        _coldRoomRecipe      = from._coldRoomRecipe;
        _quarantineBay       = from._quarantineBay;
        _quarantineBayRecipe = from._quarantineBayRecipe;
    }

    /// <summary>Pins <c>location</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReceivingWarehouse WithLocation(UnLocode value) {
        AnyReceivingWarehouse copy = new AnyReceivingWarehouse(this);

        copy._location = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>location</c> is drawn from — <c>AnyValidLocation()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUnLocode, AnyUnLocode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReceivingWarehouse WithLocation(Func<AnyUnLocode, AnyUnLocode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReceivingWarehouse copy = new AnyReceivingWarehouse(this);

        copy._locationRecipe = configure(_locationRecipe);
        copy._location       = copy._locationRecipe;

        return copy;
    }

    /// <summary>Pins <c>operatorId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReceivingWarehouse WithOperatorId(CustodianId value) {
        AnyReceivingWarehouse copy = new AnyReceivingWarehouse(this);

        copy._operatorId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>operatorId</c> is drawn from — <c>AnyValidOperatorId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCustodianId, AnyCustodianId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReceivingWarehouse WithOperatorId(Func<AnyCustodianId, AnyCustodianId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReceivingWarehouse copy = new AnyReceivingWarehouse(this);

        copy._operatorIdRecipe = configure(_operatorIdRecipe);
        copy._operatorId       = copy._operatorIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>coldRoom</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReceivingWarehouse WithColdRoom(TemperatureRange value) {
        AnyReceivingWarehouse copy = new AnyReceivingWarehouse(this);

        copy._coldRoom = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>coldRoom</c> is drawn from — <c>AnyValidColdRoom()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTemperatureRange, AnyTemperatureRange&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReceivingWarehouse WithColdRoom(Func<AnyTemperatureRange, AnyTemperatureRange> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReceivingWarehouse copy = new AnyReceivingWarehouse(this);

        copy._coldRoomRecipe = configure(_coldRoomRecipe);
        copy._coldRoom       = copy._coldRoomRecipe;

        return copy;
    }

    /// <summary>Pins <c>quarantineBay</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReceivingWarehouse WithQuarantineBay(BinLocation value) {
        AnyReceivingWarehouse copy = new AnyReceivingWarehouse(this);

        copy._quarantineBay = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>quarantineBay</c> is drawn from — <c>AnyValidQuarantineBay()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBinLocation, AnyBinLocation&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReceivingWarehouse WithQuarantineBay(Func<AnyBinLocation, AnyBinLocation> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReceivingWarehouse copy = new AnyReceivingWarehouse(this);

        copy._quarantineBayRecipe = configure(_quarantineBayRecipe);
        copy._quarantineBay       = copy._quarantineBayRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ReceivingWarehouse" />.</summary>
    public ReceivingWarehouse Generate() {
        return new ReceivingWarehouse(_location.Generate(),
                                      _operatorId.Generate(),
                                      _coldRoom.Generate(),
                                      _quarantineBay.Generate());
    }

}
