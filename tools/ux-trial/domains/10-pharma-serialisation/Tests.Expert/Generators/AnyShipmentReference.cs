using JustDummies;

namespace Serialisation.Domain;

/// <summary>A generator of arbitrary shipment references: SHP- and eight digits.</summary>
public sealed class AnyShipmentReference : IAny<string> {

    private static readonly IAny<string> Reference = Any.StringMatching("SHP-[0-9]{8}");

    public string Generate() {
        return Reference.Generate();
    }

}

public static class AnyShipmentReferenceEntry {

    extension(Any) {

        public static AnyShipmentReference ShipmentReference() {
            return new AnyShipmentReference();
        }

    }

}
