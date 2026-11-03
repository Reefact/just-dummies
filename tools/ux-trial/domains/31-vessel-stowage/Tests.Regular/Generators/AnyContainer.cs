// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Container --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace VesselStowage.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Container" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyContainer : IAny<Container> {

    private IAny<ContainerId>       _id;
    private IAny<ContainerSize>     _size;
    private IAny<PortCode>          _portOfLoading;
    private IAny<PortCode>          _portOfDischarge;
    private IAny<HazardClass>       _hazard;
    private IAny<bool>              _isReefer;

    private AnyContainerId         _idRecipe;
    private AnyEnum<ContainerSize> _sizeRecipe;
    private AnyDecimal             _tareTonnesRecipe;
    private AnyDecimal             _grossMassTonnesRecipe;
    private AnyPortCode            _portOfLoadingRecipe;
    private AnyPortCode            _portOfDischargeRecipe;
    private AnyEnum<HazardClass>   _hazardRecipe;
    private AnyBoolean             _isReeferRecipe;

    private decimal? _tareTonnesPin;
    private decimal? _grossMassTonnesPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyContainer()
        : this(id:              AnyValidId(),
               size:            AnyValidSize(),
               tareTonnes:      AnyValidTareTonnes(),
               grossMassTonnes: AnyValidGrossMassTonnes(),
               portOfLoading:   AnyValidPortOfLoading(),
               portOfDischarge: AnyValidPortOfDischarge(),
               hazard:          AnyValidHazard(),
               isReefer:        AnyValidIsReefer()) { }

    private static AnyContainerId AnyValidId() {
        return new AnyContainerId();
    }

    private static AnyEnum<ContainerSize> AnyValidSize() {
        return Any.Enum<ContainerSize>();
    }

    private static AnyDecimal AnyValidTareTonnes() {
        // The union of both sizes' ranges: Generate() narrows it to the size it draws.
        return Any.Decimal().Between(1.8m, 4.9m).WithScale(1);
    }

    private static AnyDecimal AnyValidGrossMassTonnes() {
        return Any.Decimal().Between(1.8m, 32.5m).WithScale(2);
    }

    private static AnyPortCode AnyValidPortOfLoading() {
        return new AnyPortCode();
    }

    private static AnyPortCode AnyValidPortOfDischarge() {
        return new AnyPortCode();
    }

    private static AnyEnum<HazardClass> AnyValidHazard() {
        return Any.Enum<HazardClass>();
    }

    private static AnyBoolean AnyValidIsReefer() {
        return Any.Boolean();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyContainer(AnyContainerId         id,
                         AnyEnum<ContainerSize> size,
                         AnyDecimal             tareTonnes,
                         AnyDecimal             grossMassTonnes,
                         AnyPortCode            portOfLoading,
                         AnyPortCode            portOfDischarge,
                         AnyEnum<HazardClass>   hazard,
                         AnyBoolean             isReefer) {
        _id                           = id;
        _idRecipe                     = id;
        _size                         = size;
        _sizeRecipe                   = size;
        _tareTonnesRecipe             = tareTonnes;
        _grossMassTonnesRecipe        = grossMassTonnes;
        _portOfLoading                = portOfLoading;
        _portOfLoadingRecipe          = portOfLoading;
        _portOfDischarge              = portOfDischarge;
        _portOfDischargeRecipe        = portOfDischarge;
        _hazard                       = hazard;
        _hazardRecipe                 = hazard;
        _isReefer                     = isReefer;
        _isReeferRecipe               = isReefer;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyContainer(AnyContainer from) {
        _id                           = from._id;
        _idRecipe                     = from._idRecipe;
        _size                         = from._size;
        _sizeRecipe                   = from._sizeRecipe;
        _tareTonnesRecipe             = from._tareTonnesRecipe;
        _tareTonnesPin                = from._tareTonnesPin;
        _grossMassTonnesRecipe        = from._grossMassTonnesRecipe;
        _grossMassTonnesPin           = from._grossMassTonnesPin;
        _portOfLoading                = from._portOfLoading;
        _portOfLoadingRecipe          = from._portOfLoadingRecipe;
        _portOfDischarge              = from._portOfDischarge;
        _portOfDischargeRecipe        = from._portOfDischargeRecipe;
        _hazard                       = from._hazard;
        _hazardRecipe                 = from._hazardRecipe;
        _isReefer                     = from._isReefer;
        _isReeferRecipe               = from._isReeferRecipe;
    }

    /// <summary>Draws <c>tareTonnes</c> and <c>grossMassTonnes</c> together, <c>tareTonnes</c> never after <c>grossMassTonnes</c> as the constructor requires, each from its current recipe and pin.</summary>
    private AnyOrderedPair<decimal> TareTonnesAndGrossMassTonnes(ContainerSize size) {
        (decimal minTare, decimal maxTare) = size == ContainerSize.TwentyFoot ? (1.8m, 3.2m) : (3.4m, 4.9m);

        AnyOrderedPair<decimal> pair = Any.OrderedPair(_tareTonnesRecipe.Between(minTare, maxTare),
                                                       _grossMassTonnesRecipe.Between(minTare, Container.MaxGrossMass(size)));

        if (_tareTonnesPin is { } tareTonnes) { pair = pair.WithFirst(tareTonnes); }
        if (_grossMassTonnesPin is { } grossMassTonnes) { pair = pair.WithSecond(grossMassTonnes); }

        return pair;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyContainer WithId(ContainerId value) {
        AnyContainer copy = new AnyContainer(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyContainerId, AnyContainerId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyContainer WithId(Func<AnyContainerId, AnyContainerId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyContainer copy = new AnyContainer(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>size</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyContainer WithSize(ContainerSize value) {
        AnyContainer copy = new AnyContainer(this);

        copy._size = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>size</c> is drawn from — <c>AnyValidSize()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;ContainerSize&gt;, AnyEnum&lt;ContainerSize&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyContainer WithSize(Func<AnyEnum<ContainerSize>, AnyEnum<ContainerSize>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyContainer copy = new AnyContainer(this);

        copy._sizeRecipe = configure(_sizeRecipe);
        copy._size       = copy._sizeRecipe;

        return copy;
    }

    /// <summary>Pins <c>tareTonnes</c> to a fixed value, which bounds <c>grossMassTonnes</c>; its recipe is kept for a later refinement.</summary>
    public AnyContainer WithTareTonnes(decimal value) {
        AnyContainer copy = new AnyContainer(this);

        copy._tareTonnesPin                = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>tareTonnes</c> is drawn from — <c>AnyValidTareTonnes()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>grossMassTonnes</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyContainer WithTareTonnes(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyContainer copy = new AnyContainer(this);

        copy._tareTonnesRecipe             = configure(_tareTonnesRecipe);
        copy._tareTonnesPin                = null;

        return copy;
    }

    /// <summary>Pins <c>grossMassTonnes</c> to a fixed value, which bounds <c>tareTonnes</c>; its recipe is kept for a later refinement.</summary>
    public AnyContainer WithGrossMassTonnes(decimal value) {
        AnyContainer copy = new AnyContainer(this);

        copy._grossMassTonnesPin           = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>grossMassTonnes</c> is drawn from — <c>AnyValidGrossMassTonnes()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>tareTonnes</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyContainer WithGrossMassTonnes(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyContainer copy = new AnyContainer(this);

        copy._grossMassTonnesRecipe        = configure(_grossMassTonnesRecipe);
        copy._grossMassTonnesPin           = null;

        return copy;
    }

    /// <summary>Pins <c>portOfLoading</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyContainer WithPortOfLoading(PortCode value) {
        AnyContainer copy = new AnyContainer(this);

        copy._portOfLoading = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>portOfLoading</c> is drawn from — <c>AnyValidPortOfLoading()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPortCode, AnyPortCode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyContainer WithPortOfLoading(Func<AnyPortCode, AnyPortCode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyContainer copy = new AnyContainer(this);

        copy._portOfLoadingRecipe = configure(_portOfLoadingRecipe);
        copy._portOfLoading       = copy._portOfLoadingRecipe;

        return copy;
    }

    /// <summary>Pins <c>portOfDischarge</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyContainer WithPortOfDischarge(PortCode value) {
        AnyContainer copy = new AnyContainer(this);

        copy._portOfDischarge = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>portOfDischarge</c> is drawn from — <c>AnyValidPortOfDischarge()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPortCode, AnyPortCode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyContainer WithPortOfDischarge(Func<AnyPortCode, AnyPortCode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyContainer copy = new AnyContainer(this);

        copy._portOfDischargeRecipe = configure(_portOfDischargeRecipe);
        copy._portOfDischarge       = copy._portOfDischargeRecipe;

        return copy;
    }

    /// <summary>Pins <c>hazard</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyContainer WithHazard(HazardClass value) {
        AnyContainer copy = new AnyContainer(this);

        copy._hazard = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>hazard</c> is drawn from — <c>AnyValidHazard()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;HazardClass&gt;, AnyEnum&lt;HazardClass&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyContainer WithHazard(Func<AnyEnum<HazardClass>, AnyEnum<HazardClass>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyContainer copy = new AnyContainer(this);

        copy._hazardRecipe = configure(_hazardRecipe);
        copy._hazard       = copy._hazardRecipe;

        return copy;
    }

    /// <summary>Pins <c>isReefer</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyContainer WithIsReefer(bool value) {
        AnyContainer copy = new AnyContainer(this);

        copy._isReefer = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>isReefer</c> is drawn from — <c>AnyValidIsReefer()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBoolean, AnyBoolean&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyContainer WithIsReefer(Func<AnyBoolean, AnyBoolean> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyContainer copy = new AnyContainer(this);

        copy._isReeferRecipe = configure(_isReeferRecipe);
        copy._isReefer       = copy._isReeferRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Container" />.</summary>
    public Container Generate() {
        // The masses, the two ports and the reefer flag are tied to other parameters (size, each other, hazard):
        // they are drawn here, after the parameter they depend on.
        ContainerSize size = _size.Generate();
        (decimal tareTonnes, decimal grossMassTonnes) = TareTonnesAndGrossMassTonnes(size).Generate();

        PortCode portOfLoading   = _portOfLoading.Generate();
        PortCode portOfDischarge = ReferenceEquals(_portOfDischarge, _portOfDischargeRecipe)
            ? _portOfDischarge.DifferentFrom(portOfLoading).Generate()
            : _portOfDischarge.Generate();

        // A reefer never carries explosives or radioactive cargo. A pinned parameter is honoured as given.
        bool hazardPinned = !ReferenceEquals(_hazard, _hazardRecipe);
        bool reeferPinned = !ReferenceEquals(_isReefer, _isReeferRecipe);
        HazardClass hazard;
        bool        isReefer;
        if (reeferPinned || !hazardPinned) {
            isReefer = _isReefer.Generate();
            hazard   = isReefer && !hazardPinned
                ? _hazardRecipe.Except(HazardClass.Explosives, HazardClass.Radioactive).Generate()
                : _hazard.Generate();
        } else {
            hazard   = _hazard.Generate();
            isReefer = hazard is not (HazardClass.Explosives or HazardClass.Radioactive) && _isReefer.Generate();
        }

        return new Container(_id.Generate(),
                             size,
                             tareTonnes,
                             grossMassTonnes,
                             portOfLoading,
                             portOfDischarge,
                             hazard,
                             isReefer);
    }

}
