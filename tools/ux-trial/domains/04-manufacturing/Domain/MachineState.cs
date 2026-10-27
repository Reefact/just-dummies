namespace Manufacturing.Domain;

public enum MachineState {
    Idle,
    Setup,
    Running,
    Stopped,
    Faulted,
    Maintenance
}
