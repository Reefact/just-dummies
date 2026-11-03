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

using Freight.Domain;
using JustDummies;
using Serialisation.Domain;
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
    private IAny<IReadOnlyList<ColdChainPallet>> _pallets;

    private AnyConsignmentId         _idRecipe;
    private AnyVaccineProduct        _productRecipe;
    private AnyShipment              _shipmentRecipe;
    private AnyList<ColdChainPallet> _palletsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyColdChainConsignment()
        : this(id:       AnyValidId(),
               product:  AnyValidProduct(),
               shipment: AnyValidShipment(),
               pallets:  AnyValidPallets()) { }

    private static AnyConsignmentId AnyValidId() {
        return new AnyConsignmentId();
    }

    private static AnyVaccineProduct AnyValidProduct() {
        return new AnyVaccineProduct();
    }

    private static AnyShipment AnyValidShipment() {
        // A consignment is planned on a booked shipment (a new one is), in a reefer.
        return new AnyShipment().WithContainerType(ContainerType.Reefer40);
    }

    private static AnyList<ColdChainPallet> AnyValidPallets() {
        // 1 to 20 pallets in the domain; a dummy consignment stays small. What ties them to the product is settled in Generate().
        return Any.ListOf(new AnyColdChainPallet()).WithCountBetween(1, 6).DistinctBy(pallet => pallet.Sscc.Value);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyColdChainConsignment(AnyConsignmentId         id,
                                    AnyVaccineProduct        product,
                                    AnyShipment              shipment,
                                    AnyList<ColdChainPallet> pallets) {
        _id             = id;
        _idRecipe       = id;
        _product        = product;
        _productRecipe  = product;
        _shipment       = shipment;
        _shipmentRecipe = shipment;
        _pallets        = pallets;
        _palletsRecipe  = pallets;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyColdChainConsignment(AnyColdChainConsignment from) {
        _id             = from._id;
        _idRecipe       = from._idRecipe;
        _product        = from._product;
        _productRecipe  = from._productRecipe;
        _shipment       = from._shipment;
        _shipmentRecipe = from._shipmentRecipe;
        _pallets        = from._pallets;
        _palletsRecipe  = from._palletsRecipe;
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

    /// <summary>Refines the recipe <c>pallets</c> is drawn from — <c>AnyValidPallets()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;ColdChainPallet&gt;, AnyList&lt;ColdChainPallet&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyColdChainConsignment WithPallets(Func<AnyList<ColdChainPallet>, AnyList<ColdChainPallet>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyColdChainConsignment copy = new AnyColdChainConsignment(this);

        copy._palletsRecipe = configure(_palletsRecipe);
        copy._pallets       = copy._palletsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ColdChainConsignment" />.</summary>
    public ColdChainConsignment Generate() {
        VaccineProduct product = _product.Generate();

        IReadOnlyList<ColdChainPallet> pallets = _pallets.Generate();
        if (ReferenceEquals(_pallets, _palletsRecipe)) {
            pallets = OfTheProduct(product, pallets);
        }

        return new ColdChainConsignment(_id.Generate(),
                                        product,
                                        _shipment.Generate(),
                                        pallets);
    }

    /// <summary>
    ///     Every pallet carries the product's GTIN, with its own logger and its own serials, and a logger records often
    ///     enough to resolve the product's single-excursion allowance.
    /// </summary>
    private static IReadOnlyList<ColdChainPallet> OfTheProduct(VaccineProduct product, IReadOnlyList<ColdChainPallet> drawn) {
        HashSet<string>     loggers = new HashSet<string>();
        HashSet<string>     serials = new HashSet<string>();
        List<ColdChainPallet> pallets = new List<ColdChainPallet>();

        foreach (ColdChainPallet candidate in drawn) {
            ColdChainPallet pallet = candidate;
            for (int attempt = 0; attempt < 10 && !IsNew(pallet, loggers, serials); attempt++) {
                pallet = new AnyColdChainPallet().Generate();
            }

            if (!IsNew(pallet, loggers, serials)) { throw new InvalidOperationException("No pallet distinct from the others could be drawn."); }

            loggers.Add(pallet.Logger.Id.Value);
            serials.UnionWith(pallet.Packs.Select(pack => pack.Serial.Value));

            int          allowance = product.Budget.MaxSingleExcursionMinutes;
            DataLogger   logger    = pallet.Logger;
            if (allowance > 0 && logger.RecordingIntervalMinutes > allowance) {
                logger = new DataLogger(logger.Id, allowance, logger.CalibratedOn, logger.CalibrationDueOn);
            }

            IReadOnlyList<SerialisedPack> packs = pallet.Packs.Select(pack => new SerialisedPack(product.Gtin, pack.Batch, pack.ExpiresOn, pack.Serial)).ToList();
            pallets.Add(new ColdChainPallet(pallet.Sscc, logger, packs));
        }

        return pallets;
    }

    private static bool IsNew(ColdChainPallet pallet, HashSet<string> loggers, HashSet<string> serials) {
        return !loggers.Contains(pallet.Logger.Id.Value) && !pallet.Packs.Any(pack => serials.Contains(pack.Serial.Value));
    }

}
