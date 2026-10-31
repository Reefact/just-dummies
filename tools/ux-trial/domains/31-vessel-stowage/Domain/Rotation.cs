namespace VesselStowage.Domain;

/// <summary>The ordered ports of call of a voyage. Each port is called once.</summary>
public sealed class Rotation
{
    private readonly List<PortCode> _ports;

    public Rotation(IEnumerable<PortCode> ports)
    {
        ArgumentNullException.ThrowIfNull(ports);
        List<PortCode> list = ports.ToList();
        if (list.Count < 2)
        {
            throw new ArgumentException("A rotation calls at least two ports.", nameof(ports));
        }

        if (list.Any(p => p is null))
        {
            throw new ArgumentException("A rotation cannot contain a null port.", nameof(ports));
        }

        if (list.Distinct().Count() != list.Count)
        {
            throw new ArgumentException("A port appears only once in a rotation.", nameof(ports));
        }

        _ports = list;
    }

    public IReadOnlyList<PortCode> Ports => _ports;

    public bool Calls(PortCode port) => _ports.Contains(port);

    /// <summary>Zero-based position of the port in the rotation.</summary>
    public int IndexOf(PortCode port)
    {
        ArgumentNullException.ThrowIfNull(port);
        int index = _ports.IndexOf(port);
        if (index < 0)
        {
            throw new ArgumentException($"Port {port} is not in the rotation.", nameof(port));
        }

        return index;
    }
}
