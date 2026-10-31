// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VesselStowage.Domain.Container --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace VesselStowage.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Container" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyContainer : IAny<Container> {

    private readonly IAny<ContainerId>   _id;
    private readonly IAny<ContainerSize> _size;
    private readonly IAny<decimal>?      _tareTonnes;
    private readonly IAny<decimal>?      _grossMassTonnes;
    private readonly IAny<PortCode>      _portOfLoading;
    private readonly IAny<PortCode>      _portOfDischarge;
    private readonly IAny<HazardClass>   _hazard;
    private readonly IAny<bool>?         _isReefer;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyContainer()
        : this(id:              AnyValidId(),
               size:            AnyValidSize(),
               tareTonnes:      null,
               grossMassTonnes: null,
               portOfLoading:   AnyValidPortOfLoading(),
               portOfDischarge: AnyValidPortOfDischarge(),
               hazard:          AnyValidHazard(),
               isReefer:        null) { }

    private static AnyContainerId AnyValidId() {
        return new AnyContainerId();
    }

    private static AnyEnum<ContainerSize> AnyValidSize() {
        return Any.Enum<ContainerSize>();
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

    private AnyContainer(IAny<ContainerId>   id,
                         IAny<ContainerSize> size,
                         IAny<decimal>?      tareTonnes,
                         IAny<decimal>?      grossMassTonnes,
                         IAny<PortCode>      portOfLoading,
                         IAny<PortCode>      portOfDischarge,
                         IAny<HazardClass>   hazard,
                         IAny<bool>?         isReefer) {
        _id              = id;
        _size            = size;
        _tareTonnes      = tareTonnes;
        _grossMassTonnes = grossMassTonnes;
        _portOfLoading   = portOfLoading;
        _portOfDischarge = portOfDischarge;
        _hazard          = hazard;
        _isReefer        = isReefer;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyContainer WithId(ContainerId value) {
        return new AnyContainer(new FixedValue<ContainerId>(value), _size, _tareTonnes, _grossMassTonnes, _portOfLoading, _portOfDischarge, _hazard, _isReefer);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyContainer WithId(Func<AnyContainerId, AnyContainerId> configure) {
        return new AnyContainer(configure(AnyValidId()), _size, _tareTonnes, _grossMassTonnes, _portOfLoading, _portOfDischarge, _hazard, _isReefer);
    }

    /// <summary>Pins <c>size</c> to a fixed value.</summary>
    public AnyContainer WithSize(ContainerSize value) {
        return new AnyContainer(_id, new FixedValue<ContainerSize>(value), _tareTonnes, _grossMassTonnes, _portOfLoading, _portOfDischarge, _hazard, _isReefer);
    }

    /// <summary>Draws <c>size</c> from <c>AnyValidSize()</c>, refined by <paramref name="configure" />.</summary>
    public AnyContainer WithSize(Func<AnyEnum<ContainerSize>, AnyEnum<ContainerSize>> configure) {
        return new AnyContainer(_id, configure(AnyValidSize()), _tareTonnes, _grossMassTonnes, _portOfLoading, _portOfDischarge, _hazard, _isReefer);
    }

    /// <summary>Pins <c>tareTonnes</c> to a fixed value.</summary>
    public AnyContainer WithTareTonnes(decimal value) {
        return new AnyContainer(_id, _size, new FixedValue<decimal>(value), _grossMassTonnes, _portOfLoading, _portOfDischarge, _hazard, _isReefer);
    }

    /// <summary>Pins <c>grossMassTonnes</c> to a fixed value.</summary>
    public AnyContainer WithGrossMassTonnes(decimal value) {
        return new AnyContainer(_id, _size, _tareTonnes, new FixedValue<decimal>(value), _portOfLoading, _portOfDischarge, _hazard, _isReefer);
    }

    /// <summary>Pins <c>portOfLoading</c> to a fixed value.</summary>
    public AnyContainer WithPortOfLoading(PortCode value) {
        return new AnyContainer(_id, _size, _tareTonnes, _grossMassTonnes, new FixedValue<PortCode>(value), _portOfDischarge, _hazard, _isReefer);
    }

    /// <summary>Draws <c>portOfLoading</c> from <c>AnyValidPortOfLoading()</c>, refined by <paramref name="configure" />.</summary>
    public AnyContainer WithPortOfLoading(Func<AnyPortCode, AnyPortCode> configure) {
        return new AnyContainer(_id, _size, _tareTonnes, _grossMassTonnes, configure(AnyValidPortOfLoading()), _portOfDischarge, _hazard, _isReefer);
    }

    /// <summary>Pins <c>portOfDischarge</c> to a fixed value.</summary>
    public AnyContainer WithPortOfDischarge(PortCode value) {
        return new AnyContainer(_id, _size, _tareTonnes, _grossMassTonnes, _portOfLoading, new FixedValue<PortCode>(value), _hazard, _isReefer);
    }

    /// <summary>Draws <c>portOfDischarge</c> from <c>AnyValidPortOfDischarge()</c>, refined by <paramref name="configure" />.</summary>
    public AnyContainer WithPortOfDischarge(Func<AnyPortCode, AnyPortCode> configure) {
        return new AnyContainer(_id, _size, _tareTonnes, _grossMassTonnes, _portOfLoading, configure(AnyValidPortOfDischarge()), _hazard, _isReefer);
    }

    /// <summary>Pins <c>hazard</c> to a fixed value.</summary>
    public AnyContainer WithHazard(HazardClass value) {
        return new AnyContainer(_id, _size, _tareTonnes, _grossMassTonnes, _portOfLoading, _portOfDischarge, new FixedValue<HazardClass>(value), _isReefer);
    }

    /// <summary>Draws <c>hazard</c> from <c>AnyValidHazard()</c>, refined by <paramref name="configure" />.</summary>
    public AnyContainer WithHazard(Func<AnyEnum<HazardClass>, AnyEnum<HazardClass>> configure) {
        return new AnyContainer(_id, _size, _tareTonnes, _grossMassTonnes, _portOfLoading, _portOfDischarge, configure(AnyValidHazard()), _isReefer);
    }

    /// <summary>Pins <c>isReefer</c> to a fixed value.</summary>
    public AnyContainer WithIsReefer(bool value) {
        return new AnyContainer(_id, _size, _tareTonnes, _grossMassTonnes, _portOfLoading, _portOfDischarge, _hazard, new FixedValue<bool>(value));
    }

    /// <summary>Produces one arbitrary <see cref="Container" />.</summary>
    /// <remarks>
    ///     Tare, gross mass and reefer flag depend on other parameters (size, tare, hazard), so unless pinned
    ///     they are derived here from what was drawn first. The discharge port is redrawn while it equals
    ///     the loading port.
    /// </remarks>
    public Container Generate() {
        ContainerSize size   = _size.Generate();
        HazardClass   hazard = _hazard.Generate();
        decimal tare = (_tareTonnes ?? (size == ContainerSize.TwentyFoot
                                            ? Any.Decimal().Between(1.8m, 3.2m).WithScale(2)
                                            : Any.Decimal().Between(3.4m, 4.9m).WithScale(2))).Generate();
        decimal gross = (_grossMassTonnes ?? Any.Decimal().Between(tare, Container.MaxGrossMass(size)).WithScale(2)).Generate();
        bool isReefer = (_isReefer ?? (hazard is HazardClass.Explosives or HazardClass.Radioactive
                                           ? Any.Boolean().False()
                                           : Any.Boolean())).Generate();
        PortCode loading   = _portOfLoading.Generate();
        PortCode discharge = _portOfDischarge.Generate();
        for (int attempt = 0; attempt < 10 && discharge.Equals(loading); attempt++) {
            discharge = _portOfDischarge.Generate();
        }

        return new Container(_id.Generate(), size, tare, gross, loading, discharge, hazard, isReefer);
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
