// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Machine --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Manufacturing.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Machine" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMachine : IAny<Machine> {

    private readonly IAny<string>       _machineId;
    private readonly IAny<MachineState> _initialState;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMachine()
        : this(machineId:    AnyValidMachineId(),
               initialState: AnyValidInitialState()) { }

    private static AnyPattern AnyValidMachineId() {
        return Any.StringMatching(@"M-\d{3}");
    }

    private static AnyEnum<MachineState> AnyValidInitialState() {
        // A machine is commissioned idle, stopped or under maintenance.
        return Any.Enum<MachineState>().OneOf(MachineState.Idle, MachineState.Stopped, MachineState.Maintenance);
    }

    private AnyMachine(IAny<string>       machineId,
                       IAny<MachineState> initialState) {
        _machineId    = machineId;
        _initialState = initialState;
    }

    /// <summary>Pins <c>machineId</c> to a fixed value.</summary>
    public AnyMachine WithMachineId(string value) {
        return new AnyMachine(new FixedValue<string>(value), _initialState);
    }

    /// <summary>Draws <c>machineId</c> from <c>AnyValidMachineId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMachine WithMachineId(Func<AnyPattern, AnyPattern> configure) {
        return new AnyMachine(configure(AnyValidMachineId()), _initialState);
    }

    /// <summary>Pins <c>initialState</c> to a fixed value.</summary>
    public AnyMachine WithInitialState(MachineState value) {
        return new AnyMachine(_machineId, new FixedValue<MachineState>(value));
    }

    /// <summary>Draws <c>initialState</c> from <c>AnyValidInitialState()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMachine WithInitialState(Func<AnyEnum<MachineState>, AnyEnum<MachineState>> configure) {
        return new AnyMachine(_machineId, configure(AnyValidInitialState()));
    }

    /// <summary>Produces one arbitrary <see cref="Machine" />.</summary>
    public Machine Generate() {
        return new Machine(_machineId.Generate(),
                           _initialState.Generate());
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
