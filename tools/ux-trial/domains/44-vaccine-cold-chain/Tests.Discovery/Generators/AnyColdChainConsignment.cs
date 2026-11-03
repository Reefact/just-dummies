// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ColdChainConsignment --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;
using System.Linq;

using AgriTech.Domain;
using Freight.Domain;
using JustDummies;
using Warehouse.Domain;

using FreightShipment = Freight.Domain.Shipment;

namespace ColdChain.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ColdChainConsignment" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyColdChainConsignment : IAny<ColdChainConsignment> {

    private IAny<ConsignmentId>                  _id;
    private IAny<VaccineProduct>                 _product;
    private IAny<FreightShipment>                       _shipment;
    private IAny<IReadOnlyList<ColdChainPallet>>? _pallets;

    private AnyConsignmentId         _idRecipe;
    private AnyVaccineProduct        _productRecipe;
    private AnyShipment              _shipmentRecipe;
    private AnyInt32                 _palletCountRecipe;
    private Func<AnyColdChainPallet, AnyColdChainPallet> _palletRefinement = pallet => pallet;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyColdChainConsignment()
        : this(id:       AnyValidId(),
               product:  AnyValidProduct(),
               shipment: AnyValidShipment(),
               palletCount: AnyValidPalletCount()) { }

    private static AnyConsignmentId AnyValidId() {
        return new AnyConsignmentId();
    }

    private static AnyVaccineProduct AnyValidProduct() {
        return new AnyVaccineProduct();
    }

    private static AnyShipment AnyValidShipment() {
        // Vaccines travel in a reefer; a shipment built by its constructor is always Booked.
        return new AnyShipment().WithContainerType(ContainerType.Reefer40);
    }

    private static AnyInt32 AnyValidPalletCount() {
        // A handful of pallets stands for a load: the 20-pallet ceiling is a boundary to pin, not a size to draw.
        return Any.Int32().Between(1, 5);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyColdChainConsignment(AnyConsignmentId         id,
                                    AnyVaccineProduct        product,
                                    AnyShipment              shipment,
                                    AnyInt32                 palletCount) {
        _id             = id;
        _idRecipe       = id;
        _product        = product;
        _productRecipe  = product;
        _shipment       = shipment;
        _shipmentRecipe = shipment;
        _palletCountRecipe = palletCount;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyColdChainConsignment(AnyColdChainConsignment from) {
        _id             = from._id;
        _idRecipe       = from._idRecipe;
        _product        = from._product;
        _productRecipe  = from._productRecipe;
        _shipment       = from._shipment;
        _shipmentRecipe = from._shipmentRecipe;
        _pallets           = from._pallets;
        _palletCountRecipe = from._palletCountRecipe;
        _palletRefinement  = from._palletRefinement;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyColdChainConsignment WithId(ConsignmentId value) {
        AnyColdChainConsignment copy = new AnyColdChainConsignment(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyConsignmentId, AnyConsignmentId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyColdChainConsignment WithId(Func<AnyConsignmentId, AnyConsignmentId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyColdChainConsignment copy = new AnyColdChainConsignment(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>product</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyColdChainConsignment WithProduct(VaccineProduct value) {
        AnyColdChainConsignment copy = new AnyColdChainConsignment(this);

        copy._product = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>product</c> is drawn from — <c>AnyValidProduct()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyVaccineProduct, AnyVaccineProduct&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyColdChainConsignment WithProduct(Func<AnyVaccineProduct, AnyVaccineProduct> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyColdChainConsignment copy = new AnyColdChainConsignment(this);

        copy._productRecipe = configure(_productRecipe);
        copy._product       = copy._productRecipe;

        return copy;
    }

    /// <summary>Pins <c>shipment</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyColdChainConsignment WithShipment(FreightShipment value) {
        AnyColdChainConsignment copy = new AnyColdChainConsignment(this);

        copy._shipment = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>shipment</c> is drawn from — <c>AnyValidShipment()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyShipment, AnyShipment&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyColdChainConsignment WithShipment(Func<AnyShipment, AnyShipment> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyColdChainConsignment copy = new AnyColdChainConsignment(this);

        copy._shipmentRecipe = configure(_shipmentRecipe);
        copy._shipment       = copy._shipmentRecipe;

        return copy;
    }

    /// <summary>Pins <c>pallets</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyColdChainConsignment WithPallets(IReadOnlyList<ColdChainPallet> value) {
        AnyColdChainConsignment copy = new AnyColdChainConsignment(this);

        copy._pallets = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines every pallet drawn: each one is loaded with the product's GTIN, whatever <paramref name="configure" /> does.</summary>
    public AnyColdChainConsignment WithPallets(Func<AnyColdChainPallet, AnyColdChainPallet> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyColdChainConsignment copy = new AnyColdChainConsignment(this);

        Func<AnyColdChainPallet, AnyColdChainPallet> previous = _palletRefinement;
        copy._palletRefinement = pallet => configure(previous(pallet));

        return copy;
    }

    /// <summary>How many pallets are drawn when they are not pinned.</summary>
    public AnyColdChainConsignment WithPalletCount(int count) {
        AnyColdChainConsignment copy = new AnyColdChainConsignment(this);

        copy._palletCountRecipe = Any.Int32().Between(count, count);

        return copy;
    }


    /// <summary>Produces one arbitrary <see cref="ColdChainConsignment" />.</summary>
    public ColdChainConsignment Generate() {
        VaccineProduct product = _product.Generate();

        IReadOnlyList<ColdChainPallet> pallets = _pallets is not null ? _pallets.Generate() : PalletsOf(product);

        return new ColdChainConsignment(_id.Generate(),
                                        product,
                                        _shipment.Generate(),
                                        pallets);
    }

    /// <summary>
    ///     Pallets the constructor accepts for <paramref name="product" />: its GTIN on every pack, a distinct SSCC and a
    ///     distinct logger on each, and loggers fast enough to resolve its single-excursion allowance.
    /// </summary>
    private IReadOnlyList<ColdChainPallet> PalletsOf(VaccineProduct product) {
        int count     = _palletCountRecipe.Generate();
        List<Sscc>     ssccs     = Any.ListOf(new AnySscc()).WithCount(count).Distinct().Generate();
        List<SensorId> loggerIds = Any.ListOf(new AnySensorId()).WithCount(count).Distinct().Generate();

        return Enumerable.Range(0, count)
                         .Select(index => _palletRefinement(new AnyColdChainPallet()
                                                                .ForProduct(product)
                                                                .WithSscc(ssccs[index])
                                                                .WithLogger(logger => logger.WithId(loggerIds[index])))
                                              .Generate())
                         .ToList();
    }

}
