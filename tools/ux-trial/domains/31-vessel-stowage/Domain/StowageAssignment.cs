namespace VesselStowage.Domain;

/// <summary>A container placed in a slot whose bay accepts its size.</summary>
public sealed class StowageAssignment
{
    public StowageAssignment(Container container, SlotPosition slot)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(slot);
        if (slot.AcceptedSize != container.Size)
        {
            throw new ArgumentException($"Bay {slot.Bay} takes {slot.AcceptedSize} containers, not {container.Size}.", nameof(slot));
        }

        Container = container;
        Slot = slot;
    }

    public Container Container { get; }

    public SlotPosition Slot { get; }
}
