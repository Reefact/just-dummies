// Scaffolded by `any` (JustDummies), then reworked by hand: three constructor guards tie
// parameters together (size -> tare -> gross mass, loading port != discharge port, reefer -> hazard),
// which no per-parameter recipe can honour. Each tie is drawn in dependency order in Generate().
// Do NOT re-scaffold with --force: this file would lose those ties.

using System;

using JustDummies;

namespace VesselStowage.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Container" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes.
/// </summary>
public sealed partial class AnyContainer : IAny<Container> {

    private readonly IAny<ContainerId>                             _id;
    private readonly IAny<ContainerSize>                           _size;
    private readonly decimal?                                      _pinnedTare;
    private readonly decimal?                                      _pinnedGross;
    private readonly Func<AnyDecimal, AnyDecimal>                  _refineGross;
    private readonly IAny<PortCode>                                _portOfLoading;
    private readonly Func<PortCode, IAny<PortCode>>                _portOfDischarge;
    private readonly Func<AnyEnum<HazardClass>, IAny<HazardClass>> _hazard;
    private readonly bool?                                         _pinnedReefer;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyContainer()
        : this(id:              new AnyContainerId(),
               size:            Any.Enum<ContainerSize>(),
               pinnedTare:      null,
               pinnedGross:     null,
               refineGross:     g => g,
               portOfLoading:   new AnyPortCode(),
               portOfDischarge: AnyValidPortOfDischarge,
               hazard:          h => h,
               pinnedReefer:    null) { }

    private AnyContainer(IAny<ContainerId>                             id,
                         IAny<ContainerSize>                           size,
                         decimal?                                      pinnedTare,
                         decimal?                                      pinnedGross,
                         Func<AnyDecimal, AnyDecimal>                  refineGross,
                         IAny<PortCode>                                portOfLoading,
                         Func<PortCode, IAny<PortCode>>                portOfDischarge,
                         Func<AnyEnum<HazardClass>, IAny<HazardClass>> hazard,
                         bool?                                         pinnedReefer) {
        _id              = id;
        _size            = size;
        _pinnedTare      = pinnedTare;
        _pinnedGross     = pinnedGross;
        _refineGross     = refineGross;
        _portOfLoading   = portOfLoading;
        _portOfDischarge = portOfDischarge;
        _hazard          = hazard;
        _pinnedReefer    = pinnedReefer;
    }

    // Tare range depends on the size (20 ft: 1.8-3.2 t, 40 ft: 3.4-4.9 t).
    private static AnyDecimal AnyValidTareTonnes(ContainerSize size) {
        return size == ContainerSize.TwentyFoot
            ? Any.Decimal().Between(1.8m, 3.2m).WithScale(2)
            : Any.Decimal().Between(3.4m, 4.9m).WithScale(2);
    }

    // Gross mass lies between the tare and the size's maximum gross mass.
    private static AnyDecimal AnyValidGrossMassTonnes(ContainerSize size, decimal tare) {
        return Any.Decimal().Between(tare, Container.MaxGrossMass(size)).WithScale(2);
    }

    private static IAny<PortCode> AnyValidPortOfDischarge(PortCode portOfLoading) {
        return new AnyPortCode().WithValue(v => v.DifferentFrom(portOfLoading.Value));
    }

    // A reefer never carries explosives or radioactive cargo.
    private static AnyEnum<HazardClass> AnyValidHazard(bool isReefer) {
        return isReefer
            ? Any.Enum<HazardClass>().Except(HazardClass.Explosives, HazardClass.Radioactive)
            : Any.Enum<HazardClass>();
    }

    private AnyContainer With(IAny<ContainerId>?                             id              = null,
                              IAny<ContainerSize>?                           size            = null,
                              decimal?                                       pinnedTare      = null,
                              decimal?                                       pinnedGross     = null,
                              Func<AnyDecimal, AnyDecimal>?                  refineGross     = null,
                              IAny<PortCode>?                                portOfLoading   = null,
                              Func<PortCode, IAny<PortCode>>?                portOfDischarge = null,
                              Func<AnyEnum<HazardClass>, IAny<HazardClass>>? hazard          = null,
                              bool?                                          pinnedReefer    = null) {
        return new AnyContainer(id ?? _id,
                                size ?? _size,
                                pinnedTare ?? _pinnedTare,
                                pinnedGross ?? _pinnedGross,
                                refineGross ?? _refineGross,
                                portOfLoading ?? _portOfLoading,
                                portOfDischarge ?? _portOfDischarge,
                                hazard ?? _hazard,
                                pinnedReefer ?? _pinnedReefer);
    }

    public AnyContainer WithId(ContainerId value) => With(id: new FixedValue<ContainerId>(value));

    public AnyContainer WithSize(ContainerSize value) => With(size: new FixedValue<ContainerSize>(value));

    public AnyContainer WithTareTonnes(decimal value) => With(pinnedTare: value);

    /// <summary>Pins the gross mass; the tare is then drawn no heavier than it.</summary>
    public AnyContainer WithGrossMassTonnes(decimal value) => With(pinnedGross: value);

    /// <summary>Refines the size- and tare-dependent base of the gross mass (drops a pinned one).</summary>
    public AnyContainer WithGrossMassTonnes(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyContainer(_id, _size, _pinnedTare, null, configure, _portOfLoading, _portOfDischarge, _hazard, _pinnedReefer);
    }

    public AnyContainer WithPortOfLoading(PortCode value) => With(portOfLoading: new FixedValue<PortCode>(value));

    public AnyContainer WithPortOfDischarge(PortCode value) => With(portOfDischarge: _ => new FixedValue<PortCode>(value));

    public AnyContainer WithHazard(HazardClass value) => With(hazard: _ => new FixedValue<HazardClass>(value));

    public AnyContainer WithHazard(Func<AnyEnum<HazardClass>, AnyEnum<HazardClass>> configure) => With(hazard: configure);

    public AnyContainer WithIsReefer(bool value) => With(pinnedReefer: value);

    /// <summary>Produces one arbitrary <see cref="Container" />.</summary>
    public Container Generate() {
        ContainerSize size = _size.Generate();

        AnyDecimal anyTare = AnyValidTareTonnes(size);
        if (_pinnedGross is decimal grossCeiling) {
            anyTare = anyTare.LessThanOrEqualTo(grossCeiling);
        }

        decimal tare  = _pinnedTare ?? anyTare.Generate();
        decimal gross = _pinnedGross ?? _refineGross(AnyValidGrossMassTonnes(size, tare)).Generate();

        PortCode portOfLoading   = _portOfLoading.Generate();
        PortCode portOfDischarge = _portOfDischarge(portOfLoading).Generate();

        HazardClass hazard;
        bool        isReefer;
        if (_pinnedReefer is bool pinned) {
            isReefer = pinned;
            hazard   = _hazard(AnyValidHazard(isReefer)).Generate();
        } else {
            hazard   = _hazard(AnyValidHazard(isReefer: false)).Generate();
            isReefer = hazard is not (HazardClass.Explosives or HazardClass.Radioactive) && Any.Boolean().Generate();
        }

        return new Container(_id.Generate(), size, tare, gross, portOfLoading, portOfDischarge, hazard, isReefer);
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
