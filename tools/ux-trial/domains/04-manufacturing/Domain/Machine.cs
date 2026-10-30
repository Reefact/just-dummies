using System.Text.RegularExpressions;

namespace Manufacturing.Domain;

/// <summary>A work centre, identified as "M-" plus three digits, driven through a fixed state machine.</summary>
public sealed class Machine {

    public Machine(string machineId, MachineState initialState) {
        if (!Regex.IsMatch(machineId ?? string.Empty, @"^M-\d{3}$")) {
            throw new ArgumentException($"'{machineId}' is not a machine id (M-NNN).", nameof(machineId));
        }
        State = initialState switch {
            MachineState.Idle or MachineState.Stopped or MachineState.Maintenance => initialState,
            _ => throw new ArgumentOutOfRangeException(nameof(initialState), initialState, "A machine is commissioned idle, stopped or under maintenance.")
        };
        MachineId = machineId!;
    }

    public string       MachineId { get; }
    public MachineState State     { get; private set; }

    public void Fire(MachineEvent machineEvent) {
        State = (State, machineEvent) switch {
            (MachineState.Idle, MachineEvent.BeginSetup)             => MachineState.Setup,
            (MachineState.Setup, MachineEvent.Start)                 => MachineState.Running,
            (MachineState.Running, MachineEvent.Stop)                => MachineState.Stopped,
            (MachineState.Stopped, MachineEvent.Start)               => MachineState.Running,
            (MachineState.Stopped, MachineEvent.BeginSetup)          => MachineState.Setup,
            (_, MachineEvent.Fault) when State != MachineState.Maintenance => MachineState.Faulted,
            (MachineState.Faulted, MachineEvent.SendToMaintenance)   => MachineState.Maintenance,
            (MachineState.Stopped, MachineEvent.SendToMaintenance)   => MachineState.Maintenance,
            (MachineState.Idle, MachineEvent.SendToMaintenance)      => MachineState.Maintenance,
            (MachineState.Maintenance, MachineEvent.ReleaseFromMaintenance) => MachineState.Idle,
            _ => throw new InvalidOperationException($"{machineEvent} is not allowed while {State}.")
        };
    }

    public bool CanProduce => State == MachineState.Running;

}

public enum MachineEvent {
    BeginSetup,
    Start,
    Stop,
    Fault,
    SendToMaintenance,
    ReleaseFromMaintenance
}
