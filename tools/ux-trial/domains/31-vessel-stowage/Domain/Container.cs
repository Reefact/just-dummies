namespace VesselStowage.Domain;

/// <summary>A box booked on the voyage, with its verified gross mass and its route.</summary>
public sealed class Container
{
    public Container(
        ContainerId id,
        ContainerSize size,
        decimal tareTonnes,
        decimal grossMassTonnes,
        PortCode portOfLoading,
        PortCode portOfDischarge,
        HazardClass hazard,
        bool isReefer)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(portOfLoading);
        ArgumentNullException.ThrowIfNull(portOfDischarge);
        if (!Enum.IsDefined(size))
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Unknown container size.");
        }

        if (!Enum.IsDefined(hazard))
        {
            throw new ArgumentOutOfRangeException(nameof(hazard), hazard, "Unknown hazard class.");
        }

        (decimal minTare, decimal maxTare) = size == ContainerSize.TwentyFoot ? (1.8m, 3.2m) : (3.4m, 4.9m);
        if (tareTonnes < minTare || tareTonnes > maxTare)
        {
            throw new ArgumentOutOfRangeException(nameof(tareTonnes), tareTonnes, $"Tare of a {size} container is between {minTare} and {maxTare} t.");
        }

        decimal maxGross = MaxGrossMass(size);
        if (grossMassTonnes < tareTonnes || grossMassTonnes > maxGross)
        {
            throw new ArgumentOutOfRangeException(nameof(grossMassTonnes), grossMassTonnes, $"Gross mass must be between the tare ({tareTonnes} t) and {maxGross} t.");
        }

        if (portOfLoading.Equals(portOfDischarge))
        {
            throw new ArgumentException("Port of discharge must differ from port of loading.", nameof(portOfDischarge));
        }

        if (isReefer && hazard is HazardClass.Explosives or HazardClass.Radioactive)
        {
            throw new ArgumentException("Explosives and radioactive cargo are not accepted in reefers.", nameof(hazard));
        }

        Id = id;
        Size = size;
        TareTonnes = tareTonnes;
        GrossMassTonnes = grossMassTonnes;
        PortOfLoading = portOfLoading;
        PortOfDischarge = portOfDischarge;
        Hazard = hazard;
        IsReefer = isReefer;
    }

    public ContainerId Id { get; }

    public ContainerSize Size { get; }

    public decimal TareTonnes { get; }

    public decimal GrossMassTonnes { get; }

    public decimal CargoMassTonnes => GrossMassTonnes - TareTonnes;

    public PortCode PortOfLoading { get; }

    public PortCode PortOfDischarge { get; }

    public HazardClass Hazard { get; }

    public bool IsReefer { get; }

    public bool IsDangerous => Hazard != HazardClass.None;

    public static decimal MaxGrossMass(ContainerSize size) => size == ContainerSize.TwentyFoot ? 30.48m : 32.5m;

    /// <summary>Whether two neighbouring boxes may stand side by side (simplified IMDG segregation).</summary>
    public bool CanNeighbour(Container other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (!IsDangerous || !other.IsDangerous)
        {
            return true;
        }

        if (Hazard is HazardClass.Explosives or HazardClass.Radioactive || other.Hazard is HazardClass.Explosives or HazardClass.Radioactive)
        {
            return false;
        }

        bool flammable = Hazard is HazardClass.FlammableLiquids or HazardClass.FlammableSolids;
        bool otherFlammable = other.Hazard is HazardClass.FlammableLiquids or HazardClass.FlammableSolids;
        return !((flammable && other.Hazard == HazardClass.Oxidizers) || (otherFlammable && Hazard == HazardClass.Oxidizers));
    }
}
