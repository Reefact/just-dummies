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
    private readonly IAny<PortCode>?     _portOfDischarge;
    private readonly IAny<HazardClass>   _hazard;
    private readonly IAny<bool>?         _isReefer;

    // Four parameters are tied to another one by the constructor, so a null field means "derive it
    // in Generate()" rather than "draw it on its own":
    //   tare            -> within the range of the drawn size
    //   gross mass      -> between the tare and the maximum of the drawn size
    //   port of discharge -> different from the port of loading
    //   reefer          -> never for explosives or radioactive cargo
    // Pinning one of them replaces the derivation, and a pin that breaks the rule is refused by the
    // constructor on the first run.

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyContainer()
        : this(id:              AnyValidId(),
               size:            AnyValidSize(),
               tareTonnes:      null,
               grossMassTonnes: null,
               portOfLoading:   AnyValidPortOfLoading(),
               portOfDischarge: null,
               hazard:          AnyValidHazard(),
               isReefer:        null) { }

    private static AnyContainerId AnyValidId() {
        return new AnyContainerId();
    }

    private static AnyEnum<ContainerSize> AnyValidSize() {
        return Any.Enum<ContainerSize>();
    }

    private static AnyDecimal AnyValidTareTonnes(ContainerSize size) {
        return size == ContainerSize.TwentyFoot
            ? Any.Decimal().Between(1.8m, 3.2m).WithScale(2)
            : Any.Decimal().Between(3.4m, 4.9m).WithScale(2);
    }

    private static AnyDecimal AnyValidGrossMassTonnes(ContainerSize size, decimal tareTonnes) {
        return Any.Decimal().Between(tareTonnes, Container.MaxGrossMass(size)).WithScale(2);
    }

    private static AnyPortCode AnyValidPortOfLoading() {
        return new AnyPortCode();
    }

    private static AnyPortCode AnyValidPortOfDischarge(PortCode portOfLoading) {
        return new AnyPortCode().WithValue(v => v.DifferentFrom(portOfLoading.Value));
    }

    private static AnyEnum<HazardClass> AnyValidHazard() {
        return Any.Enum<HazardClass>();
    }

    private static AnyBoolean AnyValidIsReefer(HazardClass hazard) {
        return hazard is HazardClass.Explosives or HazardClass.Radioactive ? Any.Boolean().False() : Any.Boolean();
    }

    private AnyContainer(IAny<ContainerId>   id,
                         IAny<ContainerSize> size,
                         IAny<decimal>?      tareTonnes,
                         IAny<decimal>?      grossMassTonnes,
                         IAny<PortCode>      portOfLoading,
                         IAny<PortCode>?     portOfDischarge,
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
    public Container Generate() {
        ContainerSize size            = _size.Generate();
        decimal       tareTonnes      = (_tareTonnes ?? AnyValidTareTonnes(size)).Generate();
        decimal       grossMassTonnes = (_grossMassTonnes ?? AnyValidGrossMassTonnes(size, tareTonnes)).Generate();
        PortCode      portOfLoading   = _portOfLoading.Generate();
        PortCode      portOfDischarge = (_portOfDischarge ?? AnyValidPortOfDischarge(portOfLoading)).Generate();
        HazardClass   hazard          = _hazard.Generate();
        bool          isReefer        = (_isReefer ?? AnyValidIsReefer(hazard)).Generate();

        return new Container(_id.Generate(), size, tareTonnes, grossMassTonnes, portOfLoading, portOfDischarge, hazard, isReefer);
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
