// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Bus --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GridLoadFlow.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Bus" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBus : IAny<Bus> {

    private readonly IAny<BusId>                                       _id;
    private readonly IAny<string>                                      _name;
    private readonly IAny<BusType>                                     _type;
    private readonly IAny<double>                                      _baseKv;
    private readonly Func<BusType, IAny<PowerInjection>>               _generation;
    private readonly IAny<PowerInjection>                              _load;
    private readonly Func<BusType, VoltageLimits, IAny<double?>>       _voltageSetpointPu;
    private readonly IAny<VoltageLimits>                               _limits;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    /// <remarks>
    ///     Generation and setpoint depend on the bus type (and the setpoint on the limits): a PQ bus
    ///     takes no setpoint and no active generation, a PV or slack bus regulates within its band.
    ///     They are drawn after the type and the limits, from a base that knows both.
    /// </remarks>
    public AnyBus()
        : this(id:                AnyValidId(),
               name:              AnyValidName(),
               type:              AnyValidType(),
               baseKv:            AnyValidBaseKv(),
               generation:        AnyValidGeneration,
               load:              AnyValidLoad(),
               voltageSetpointPu: (type, limits) => AnyValidVoltageSetpointPu(type, limits, d => d),
               limits:            AnyValidLimits()) { }

    private static AnyBusId AnyValidId() {
        return new AnyBusId();
    }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank().WithMaxLength(64);
    }

    private static AnyEnum<BusType> AnyValidType() {
        return Any.Enum<BusType>();
    }

    private static AnyDouble AnyValidBaseKv() {
        // (0, Bus.MaximumBaseKv] kV.
        return Any.Double().GreaterThan(0.0).LessThanOrEqualTo(Bus.MaximumBaseKv);
    }

    private static AnyPowerInjection AnyValidGeneration(BusType type) {
        // A PQ bus carries no active generation; any other generates a non-negative P.
        return type == BusType.Pq
                   ? new AnyPowerInjection().WithActiveMw(0.0)
                   : new AnyPowerInjection().WithActiveMw(p => p.GreaterThanOrEqualTo(0.0));
    }

    private static AnyPowerInjection AnyValidLoad() {
        return new AnyPowerInjection();
    }

    private static IAny<double?> AnyValidVoltageSetpointPu(BusType type, VoltageLimits limits, Func<AnyDouble, AnyDouble> configure) {
        // A PQ bus takes no setpoint; a regulating bus holds one inside its own band.
        return type == BusType.Pq
                   ? new FixedValue<double?>(null)
                   : configure(Any.Double().Between(limits.MinimumPu, limits.MaximumPu)).AsNullable();
    }

    private static AnyVoltageLimits AnyValidLimits() {
        return new AnyVoltageLimits();
    }

    private AnyBus(IAny<BusId>                                 id,
                   IAny<string>                                name,
                   IAny<BusType>                               type,
                   IAny<double>                                baseKv,
                   Func<BusType, IAny<PowerInjection>>         generation,
                   IAny<PowerInjection>                        load,
                   Func<BusType, VoltageLimits, IAny<double?>> voltageSetpointPu,
                   IAny<VoltageLimits>                         limits) {
        _id                = id;
        _name              = name;
        _type              = type;
        _baseKv            = baseKv;
        _generation        = generation;
        _load              = load;
        _voltageSetpointPu = voltageSetpointPu;
        _limits            = limits;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyBus WithId(BusId value) {
        return new AnyBus(new FixedValue<BusId>(value), _name, _type, _baseKv, _generation, _load, _voltageSetpointPu, _limits);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBus WithId(Func<AnyBusId, AnyBusId> configure) {
        return new AnyBus(configure(AnyValidId()), _name, _type, _baseKv, _generation, _load, _voltageSetpointPu, _limits);
    }

    /// <summary>Pins <c>name</c> to a fixed value.</summary>
    public AnyBus WithName(string value) {
        return new AnyBus(_id, new FixedValue<string>(value), _type, _baseKv, _generation, _load, _voltageSetpointPu, _limits);
    }

    /// <summary>Draws <c>name</c> from <c>AnyValidName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBus WithName(Func<AnyString, AnyString> configure) {
        return new AnyBus(_id, configure(AnyValidName()), _type, _baseKv, _generation, _load, _voltageSetpointPu, _limits);
    }

    /// <summary>Pins <c>type</c> to a fixed value.</summary>
    public AnyBus WithType(BusType value) {
        return new AnyBus(_id, _name, new FixedValue<BusType>(value), _baseKv, _generation, _load, _voltageSetpointPu, _limits);
    }

    /// <summary>Draws <c>type</c> from <c>AnyValidType()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBus WithType(Func<AnyEnum<BusType>, AnyEnum<BusType>> configure) {
        return new AnyBus(_id, _name, configure(AnyValidType()), _baseKv, _generation, _load, _voltageSetpointPu, _limits);
    }

    /// <summary>Pins <c>baseKv</c> to a fixed value.</summary>
    public AnyBus WithBaseKv(double value) {
        return new AnyBus(_id, _name, _type, new FixedValue<double>(value), _generation, _load, _voltageSetpointPu, _limits);
    }

    /// <summary>Draws <c>baseKv</c> from <c>AnyValidBaseKv()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBus WithBaseKv(Func<AnyDouble, AnyDouble> configure) {
        return new AnyBus(_id, _name, _type, configure(AnyValidBaseKv()), _generation, _load, _voltageSetpointPu, _limits);
    }

    /// <summary>Pins <c>generation</c> to a fixed value.</summary>
    public AnyBus WithGeneration(PowerInjection value) {
        return new AnyBus(_id, _name, _type, _baseKv, _ => new FixedValue<PowerInjection>(value), _load, _voltageSetpointPu, _limits);
    }

    /// <summary>Draws <c>generation</c> from <c>AnyValidGeneration()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBus WithGeneration(Func<AnyPowerInjection, AnyPowerInjection> configure) {
        return new AnyBus(_id, _name, _type, _baseKv, t => configure(AnyValidGeneration(t)), _load, _voltageSetpointPu, _limits);
    }

    /// <summary>Pins <c>load</c> to a fixed value.</summary>
    public AnyBus WithLoad(PowerInjection value) {
        return new AnyBus(_id, _name, _type, _baseKv, _generation, new FixedValue<PowerInjection>(value), _voltageSetpointPu, _limits);
    }

    /// <summary>Draws <c>load</c> from <c>AnyValidLoad()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBus WithLoad(Func<AnyPowerInjection, AnyPowerInjection> configure) {
        return new AnyBus(_id, _name, _type, _baseKv, _generation, configure(AnyValidLoad()), _voltageSetpointPu, _limits);
    }

    /// <summary>Pins <c>voltageSetpointPu</c> to a fixed value.</summary>
    public AnyBus WithVoltageSetpointPu(double? value) {
        return new AnyBus(_id, _name, _type, _baseKv, _generation, _load, (_, _) => new FixedValue<double?>(value), _limits);
    }

    /// <summary>Draws <c>voltageSetpointPu</c> inside the bus limits, refined by <paramref name="configure" />; a PQ bus still takes none.</summary>
    public AnyBus WithVoltageSetpointPu(Func<AnyDouble, AnyDouble> configure) {
        return new AnyBus(_id, _name, _type, _baseKv, _generation, _load, (t, l) => AnyValidVoltageSetpointPu(t, l, configure), _limits);
    }

    /// <summary>Pins <c>limits</c> to a fixed value.</summary>
    public AnyBus WithLimits(VoltageLimits value) {
        return new AnyBus(_id, _name, _type, _baseKv, _generation, _load, _voltageSetpointPu, new FixedValue<VoltageLimits>(value));
    }

    /// <summary>Draws <c>limits</c> from <c>AnyValidLimits()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBus WithLimits(Func<AnyVoltageLimits, AnyVoltageLimits> configure) {
        return new AnyBus(_id, _name, _type, _baseKv, _generation, _load, _voltageSetpointPu, configure(AnyValidLimits()));
    }

    /// <summary>Produces one arbitrary <see cref="Bus" />.</summary>
    public Bus Generate() {
        BusType       type   = _type.Generate();
        VoltageLimits limits = _limits.Generate();

        return new Bus(_id.Generate(),
                       _name.Generate(),
                       type,
                       _baseKv.Generate(),
                       _generation(type).Generate(),
                       _load.Generate(),
                       _voltageSetpointPu(type, limits).Generate(),
                       limits);
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
