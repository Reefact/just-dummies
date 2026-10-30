namespace Serialisation.Domain;

/// <summary>One physical pack carrying a unique identifier: product, batch, expiry and serial.</summary>
public sealed class SerialisedPack {

    public SerialisedPack(Gtin gtin, BatchNumber batch, DateOnly expiresOn, SerialNumber serial) {
        Gtin      = gtin ?? throw new ArgumentNullException(nameof(gtin));
        Batch     = batch ?? throw new ArgumentNullException(nameof(batch));
        Serial    = serial ?? throw new ArgumentNullException(nameof(serial));
        ExpiresOn = expiresOn;
        Status    = PackStatus.Active;
    }

    public Gtin         Gtin      { get; }
    public BatchNumber  Batch     { get; }
    public DateOnly     ExpiresOn { get; }
    public SerialNumber Serial    { get; }
    public PackStatus   Status    { get; private set; }

    /// <summary>The GS1 element string printed in human-readable form beneath the DataMatrix.</summary>
    public string ToElementString() {
        return $"(01){Gtin.Value}(17){ExpiresOn:yyMMdd}(10){Batch.Value}(21){Serial.Value}";
    }

    internal void Decommission(PackStatus to) {
        if (Status != PackStatus.Active) {
            throw new InvalidOperationException($"Pack {Serial} is already {Status}.");
        }
        if (to == PackStatus.Active) {
            throw new ArgumentException("Decommissioning cannot lead back to Active.", nameof(to));
        }

        Status = to;
    }

}
