namespace Freight.Domain;

public enum ContainerType {

    Dry20,
    Dry40,
    HighCube40,
    Reefer40

}

public static class ContainerTypeSpecifications {

    public static double MaxPayloadKilograms(this ContainerType type) {
        return type switch {
            ContainerType.Dry20      => 28_200,
            ContainerType.Dry40      => 26_700,
            ContainerType.HighCube40 => 26_500,
            ContainerType.Reefer40   => 27_700,
            _                        => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static double CapacityCubicMetres(this ContainerType type) {
        return type switch {
            ContainerType.Dry20      => 33.2,
            ContainerType.Dry40      => 67.7,
            ContainerType.HighCube40 => 76.4,
            ContainerType.Reefer40   => 59.3,
            _                        => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

}
